using System.Data.SQLite;
using System.Text.Json;
using Goose.Events;

namespace Goose
{
    public class WorldState
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        // baseline and pendingDeletes are also touched by ApplyCommit on the DB thread
        private readonly object gate = new();
        private readonly Dictionary<string, string> baseline = new();
        private readonly HashSet<string> pendingDeletes = new();
        private readonly Dictionary<string, object> values = new();

        // inFlight/trailing are read and written only under gate: settlement could clear
        // inFlight and consume trailing between an unlocked read and the lock, stranding the trailing request.
        private bool inFlight;
        private bool trailing;

        private record RawJson(string Json);

        public void Load(Database db)
        {
            db.Execute(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT key, value FROM world_state";
                using var reader = cmd.ExecuteReader();
                var rows = new List<KeyValuePair<string, string>>();
                while (reader.Read())
                    rows.Add(new KeyValuePair<string, string>(reader.GetString(0), reader.GetString(1)));
                LoadRows(rows);
            });
        }

        internal void LoadRows(IEnumerable<KeyValuePair<string, string>> rows)
        {
            lock (gate)
            {
                values.Clear();
                baseline.Clear();
                pendingDeletes.Clear();
                foreach (var (key, value) in rows)
                {
                    values[key] = new RawJson(value);
                    baseline[key] = value;
                }
            }
        }

        public T? Get<T>(string key)
        {
            if (!values.TryGetValue(key, out var stored)) return default;
            if (stored is RawJson raw)
            {
                T? typed;
                try
                {
                    typed = JsonHelper.Deserialize<T>(raw.Json);
                }
                catch (JsonException e)
                {
                    log.Error(e, "Failed to parse world_state value '{key}'", key);
                    return default;
                }
                values[key] = typed!;
                return typed;
            }
            if (stored is T t) return t;
            try
            {
                return JsonHelper.Deserialize<T>(JsonHelper.Serialize(stored));
            }
            catch (JsonException e)
            {
                log.Error(e, "Failed to convert world_state value '{key}' to {type}", key, typeof(T).Name);
                return default;
            }
        }

        public void Set(string key, object value)
        {
            values[key] = value;
        }

        public void Remove(string key)
        {
            lock (gate)
            {
                values.Remove(key);
                pendingDeletes.Add(key);
            }
        }

        public IEnumerable<string> KeysWithPrefix(string prefix)
        {
            return values.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        }

        public void Save(GameWorld world)
        {
            try
            {
                SaveCore(world);
            }
            finally
            {
                AddSaveEvent(world);
            }
        }

        public void SaveSync(GameWorld world)
        {
            while (true)
            {
                bool inFlight;
                lock (gate) inFlight = this.inFlight;
                if (!inFlight) break;
                Thread.Sleep(10);
            }

            var plan = PlanSave();
            if (plan.Upserts.Count == 0 && plan.Deletes.Count == 0) return;

            world.Database.ExecuteTransaction(conn => ExecutePlan(conn, plan));
            ApplyCommit(plan);
        }

        public void AddSaveEvent(GameWorld world)
        {
            var ev = new WorldSaveEvent();
            // H6: clamp to >= 1, a 0/negative period re-enqueues at now and spins EventHandler.Update
            ev.Ticks += (long)(Math.Max(1, world.Settings.WorldSavePeriod) * world.TimerFrequency);

            world.EventHandler.AddEvent(ev);
        }

        private void SaveCore(GameWorld world)
        {
            SavePlan plan;
            lock (gate)
            {
                if (inFlight)
                {
                    trailing = true;
                    return;
                }

                inFlight = true;
                try
                {
                    plan = PlanSaveLocked();
                }
                catch
                {
                    inFlight = false;
                    throw;
                }
                if (plan.Upserts.Count == 0 && plan.Deletes.Count == 0)
                {
                    inFlight = false;
                    return;
                }
            }

            try
            {
                world.Database.EnqueueTransaction(
                    conn =>
                    {
                        // Runs on the DB thread; executes only the captured plan, never live state.
                        ExecutePlan(conn, plan);
                    },
                    onCommit: () =>
                    {
                        lock (gate)
                        {
                            ApplyCommitLocked(plan);
                        }
                    },
                    onSettled: _ => SettleSave(world));
            }
            catch
            {
                lock (gate) inFlight = false;
                throw;
            }
        }

        private void SettleSave(GameWorld world)
        {
            bool runTrailing;
            lock (gate)
            {
                inFlight = false;
                runTrailing = trailing;
                trailing = false;
            }
            if (runTrailing) world.EnqueueCompletion(() => SaveCore(world));
        }

        private static void ExecutePlan(SQLiteConnection conn, SavePlan plan)
        {
            foreach (var key in plan.Deletes)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM world_state WHERE key=@key";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.ExecuteNonQuery();
            }
            foreach (var (key, json) in plan.Upserts)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO world_state (key, value) VALUES (@key, @value) " +
                    "ON CONFLICT(key) DO UPDATE SET value=@value";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.Parameters.AddWithValue("@value", json);
                cmd.ExecuteNonQuery();
            }
        }

        internal record SavePlan(List<(string Key, string Json)> Upserts, List<string> Deletes);

        internal SavePlan PlanSave()
        {
            lock (gate)
            {
                return PlanSaveLocked();
            }
        }

        private SavePlan PlanSaveLocked()
        {
            List<(string Key, string Json)> upserts;
            List<string> deletes;

            upserts = new List<(string Key, string Json)>();
            foreach (var (key, value) in values)
            {
                var json = value is RawJson raw ? raw.Json : JsonHelper.Serialize(value);
                if (!baseline.TryGetValue(key, out var baseJson) || baseJson != json)
                    upserts.Add((key, json));
            }
            deletes = pendingDeletes.Where(k => !values.ContainsKey(k)).ToList();

            return new SavePlan(upserts, deletes);
        }

        internal void ApplyCommit(SavePlan plan)
        {
            lock (gate)
            {
                ApplyCommitLocked(plan);
            }
        }

        private void ApplyCommitLocked(SavePlan plan)
        {
            foreach (var (key, json) in plan.Upserts) baseline[key] = json;
            foreach (var key in plan.Deletes) baseline.Remove(key);
            pendingDeletes.ExceptWith(plan.Deletes);
        }
    }
}
