using System.Data.SQLite;
using System.Text.Json;

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
            return values.Keys.Where(k => k.StartsWith(prefix)).ToList();
        }

        internal record SavePlan(List<(string Key, string Json)> Upserts, List<string> Deletes);

        internal SavePlan PlanSave()
        {
            List<(string Key, string Json)> upserts;
            List<string> deletes;
            lock (gate)
            {
                upserts = new List<(string Key, string Json)>();
                foreach (var (key, value) in values)
                {
                    var json = value is RawJson raw ? raw.Json : JsonHelper.Serialize(value);
                    if (!baseline.TryGetValue(key, out var baseJson) || baseJson != json)
                        upserts.Add((key, json));
                }
                deletes = pendingDeletes.Where(k => !values.ContainsKey(k)).ToList();
            }
            return new SavePlan(upserts, deletes);
        }

        internal void ApplyCommit(SavePlan plan)
        {
            lock (gate)
            {
                foreach (var (key, json) in plan.Upserts) baseline[key] = json;
                foreach (var key in plan.Deletes) baseline.Remove(key);
                pendingDeletes.ExceptWith(plan.Deletes);
            }
        }
    }
}
