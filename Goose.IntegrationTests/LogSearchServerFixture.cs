using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Goose;
using Goose.Logs;
using Goose.Testing;

namespace Goose.IntegrationTests
{
    public sealed class LogSearchServerFixture : IDisposable
    {
        public const long StartMs = 1_700_000_000_000;
        public const long EndMs = StartMs + 3_600_000;

        public TestWorldFixture World { get; }
        public GameWorld GameWorld => this.World.World;
        public string DbPath { get; }
        internal LogSearchService Service { get; }

        private long monotonic;
        public long Monotonic { get => this.monotonic; set => this.monotonic = value; }
        public long MillisecondTicks => Stopwatch.Frequency / 1000;

        private DateTimeOffset utc = new(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        public DateTimeOffset Utc { get => this.utc; set => this.utc = value; }

        private Map? baseMap;

        public LogSearchServerFixture(bool createPlayersTable = true)
        {
            this.World = new TestWorldFixture();
            this.DbPath = Path.Combine(Path.GetTempPath(), "logsearch-" + Guid.NewGuid().ToString("N") + ".db");
            var db = this.GameWorld.Database;
            db.Start(this.DbPath);
            db.Execute(conn => RunSql(conn,
                File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "logs.sql"))));
            if (createPlayersTable)
            {
                db.Execute(conn => RunSql(conn,
                    "CREATE TABLE players (player_id INT PRIMARY KEY, player_name TEXT NOT NULL);"));
            }
            db.Execute(conn => RunSql(conn,
                "CREATE TABLE guilds (guild_id INTEGER PRIMARY KEY, guild_name TEXT NOT NULL);"));
            db.Execute(conn => RunSql(conn,
                "CREATE TABLE maps (map_id INTEGER PRIMARY KEY, map_name TEXT NOT NULL);"));
            db.Execute(conn => RunSql(conn,
                "CREATE TABLE npc_templates (npc_id INTEGER PRIMARY KEY, npc_name TEXT NOT NULL);"));

            this.Service = new LogSearchService(this.GameWorld,
                () => this.Monotonic, LogPageTokenCodec.Create, () => this.Utc);
            this.GameWorld.LogSearches = this.Service;
        }

        public TestWorldFixture.CapturingPlayer AddGm(string name, int playerId)
        {
            this.baseMap ??= this.World.AddBaseMap(7, "Town");
            var player = this.World.CommandPlayerOn(this.baseMap, 3, 4, name);
            player.PlayerID = playerId;
            player.Access = Player.AccessStatus.GameMaster;
            this.World.AddOnlinePlayer(player);
            return player;
        }

        public LogViewerWindow OpenViewer(TestWorldFixture.CapturingPlayer player)
        {
            Assert.True(LogViewerWindow.Open(player, this.GameWorld));
            return (LogViewerWindow)player.Windows[^1];
        }

        public void Send(TestWorldFixture.CapturingPlayer player, string packet)
        {
            this.GameWorld.EventHandler.AddEvent(player, packet);
            this.GameWorld.Update();
        }

        public void Pump()
        {
            this.GameWorld.Database.Execute(conn => { });
            for (int guard = 0; guard < 10_000; guard++)
            {
                this.GameWorld.Update();
                if (this.GameWorld.PendingDeliveryCount == 0) break;
            }
        }

        public int ViewLogsAuditCount =>
            this.GameWorld.LogHandler.Pending.Count(l => l.Type == Log.Types.ViewLogs);

        public string? LastLrxMessage(TestWorldFixture.CapturingPlayer player)
        {
            string? packet = player.Sent.Skip(this.LastResponseStart(player)).LastOrDefault(s => s.StartsWith("LRX"));
            if (packet is null) return null;
            string[] parts = packet.TrimEnd(LogProtocolPackets.PacketDelimiter).Split(',');
            return ProtocolTextCodec.TryDecodeText(parts[2], 4_096, out string message) ? message : null;
        }

        public string? LrxMessage(TestWorldFixture.CapturingPlayer player, int windowId, int requestId)
        {
            string? packet = player.Sent.LastOrDefault(s => s.StartsWith($"LRX{windowId},{requestId},"));
            if (packet is null) return null;
            string[] parts = packet.TrimEnd(LogProtocolPackets.PacketDelimiter).Split(',');
            return ProtocolTextCodec.TryDecodeText(parts[2], 4_096, out string message) ? message : null;
        }

        public sealed record Lrf(bool HasMore, string Current, string Next);

        public Lrf? LastLrf(TestWorldFixture.CapturingPlayer player)
        {
            string? packet = player.Sent.Skip(this.LastResponseStart(player)).LastOrDefault(s => s.StartsWith("LRF"));
            if (packet is null) return null;
            string[] parts = packet.TrimEnd(LogProtocolPackets.PacketDelimiter).Split(',');
            return new Lrf(parts[2] == "1", parts[3], parts[4]);
        }

        public List<string> LastRowJsons(TestWorldFixture.CapturingPlayer player)
        {
            var groups = new SortedDictionary<int, List<(int Index, string Segment)>>();
            foreach (string raw in player.Sent.Skip(this.LastResponseStart(player)).Where(s => s.StartsWith("LRD")))
            {
                string[] parts = raw.TrimEnd(LogProtocolPackets.PacketDelimiter).Split(',');
                int ordinal = int.Parse(parts[2], CultureInfo.InvariantCulture);
                if (!groups.TryGetValue(ordinal, out var list))
                    groups[ordinal] = list = new List<(int, string)>();
                list.Add((int.Parse(parts[3], CultureInfo.InvariantCulture), parts[5]));
            }

            var jsons = new List<string>(groups.Count);
            foreach (var list in groups.Values)
            {
                string base64 = string.Concat(list.OrderBy(c => c.Index).Select(c => c.Segment));
                jsons.Add(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64)));
            }
            return jsons;
        }

        public List<long> LastRowIds(TestWorldFixture.CapturingPlayer player)
            => this.LastRowJsons(player).Select(json =>
                JsonDocument.Parse(json).RootElement.GetProperty("rowId").GetInt64()).ToList();

        public long InsertLog(long dateTicks, long type, long player, long other, long map, string text)
        {
            return this.GameWorld.Database.Execute<long>(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "INSERT INTO logs (text, log_type, playerid, otherid, mapid, mapx, mapy, log_date) "
                    + "VALUES (@text, @type, @player, @other, @map, 1, 2, @date);";
                command.Parameters.Add(new SQLiteParameter("@text", DbType.String) { Value = text });
                command.Parameters.Add(new SQLiteParameter("@type", DbType.Int64) { Value = type });
                command.Parameters.Add(new SQLiteParameter("@player", DbType.Int64) { Value = player });
                command.Parameters.Add(new SQLiteParameter("@other", DbType.Int64) { Value = other });
                command.Parameters.Add(new SQLiteParameter("@map", DbType.Int64) { Value = map });
                command.Parameters.Add(new SQLiteParameter("@date", DbType.Int64) { Value = dateTicks });
                command.ExecuteNonQuery();
                using var rowId = conn.CreateCommand();
                rowId.CommandText = "SELECT last_insert_rowid();";
                return Convert.ToInt64(rowId.ExecuteScalar());
            });
        }

        public void InsertPlayer(int id, string name)
        {
            this.GameWorld.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "INSERT INTO players (player_id, player_name) VALUES (@id, @name);";
                command.Parameters.Add(new SQLiteParameter("@id", DbType.Int32) { Value = id });
                command.Parameters.Add(new SQLiteParameter("@name", DbType.String) { Value = name });
                command.ExecuteNonQuery();
            });
        }

        public void RenamePlayer(int id, string name)
        {
            this.GameWorld.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "UPDATE players SET player_name = @name WHERE player_id = @id;";
                command.Parameters.Add(new SQLiteParameter("@name", DbType.String) { Value = name });
                command.Parameters.Add(new SQLiteParameter("@id", DbType.Int32) { Value = id });
                command.ExecuteNonQuery();
            });
        }

        public void InsertRowsAscending(int count, long player = 0, long type = 0)
        {
            long baseTicks = ToTicks(StartMs + 1_000);
            for (int i = 1; i <= count; i++)
                this.InsertLog(baseTicks + i * TimeSpan.TicksPerSecond, type, player, 0, 0, "row " + i);
        }

        public static long ToTicks(long unixMs)
            => DateTime.UnixEpoch.Ticks + unixMs * TimeSpan.TicksPerMillisecond;

        public static string Fresh(int windowId, int requestId, long startMs = StartMs, long endMs = EndMs,
            string? participant = null, int mapId = 0, int[]? typeIds = null, string? text = null)
        {
            return "LQS" + windowId + "," + requestId + ",F," + startMs + "," + endMs + ","
                + ProtocolTextCodec.EncodeText(participant ?? "") + "," + mapId + ","
                + string.Join("|", typeIds ?? Array.Empty<int>()) + ","
                + ProtocolTextCodec.EncodeText(text ?? "");
        }

        public static string Page(int windowId, int requestId, string token)
            => "LQS" + windowId + "," + requestId + ",P," + token;

        private int LastResponseStart(TestWorldFixture.CapturingPlayer player)
        {
            int lrb = -1;
            int lrx = -1;
            for (int i = 0; i < player.Sent.Count; i++)
            {
                if (player.Sent[i].StartsWith("LRB")) lrb = i;
                else if (player.Sent[i].StartsWith("LRX")) lrx = i;
            }
            return Math.Max(lrb, lrx);
        }

        private static void RunSql(SQLiteConnection conn, string sql)
        {
            using var command = conn.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            try
            {
                this.GameWorld.Database.Stop();
            }
            catch (Exception) { }
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var path = this.DbPath + suffix;
                if (File.Exists(path)) File.Delete(path);
            }
            this.World.Dispose();
        }
    }
}
