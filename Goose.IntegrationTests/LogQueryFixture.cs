using System.Collections.Immutable;
using System.Data;
using System.Data.SQLite;
using Goose.Logs;

namespace Goose.IntegrationTests
{
    public sealed class LogQueryFixture : IDisposable
    {
        public SQLiteConnection Connection { get; }

        private readonly string _dbPath;

        public LogQueryFixture()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "logquery-" + Guid.NewGuid().ToString("N") + ".db");
            Connection = new SQLiteConnection("Data Source=" + _dbPath);
            Connection.Open();
            Run(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "logs.sql")));
            Run("CREATE TABLE players (player_id INT PRIMARY KEY, player_name TEXT NOT NULL);");
            Run("CREATE TABLE guilds (guild_id INTEGER PRIMARY KEY, guild_name TEXT NOT NULL);");
            Run("CREATE TABLE maps (map_id INTEGER PRIMARY KEY, map_name TEXT NOT NULL);");
            Run("CREATE TABLE npc_templates (npc_id INTEGER PRIMARY KEY, npc_name TEXT NOT NULL);");
        }

        public long InsertLog(long dateTicks, long type, long player, long other, long map, long x, long y, string text)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = "INSERT INTO logs (text, log_type, playerid, otherid, mapid, mapx, mapy, log_date) "
                + "VALUES (@text, @type, @player, @other, @map, @x, @y, @date);";
            command.Parameters.Add(new SQLiteParameter("@text", DbType.String) { Value = text });
            command.Parameters.Add(new SQLiteParameter("@type", DbType.Int64) { Value = type });
            command.Parameters.Add(new SQLiteParameter("@player", DbType.Int64) { Value = player });
            command.Parameters.Add(new SQLiteParameter("@other", DbType.Int64) { Value = other });
            command.Parameters.Add(new SQLiteParameter("@map", DbType.Int64) { Value = map });
            command.Parameters.Add(new SQLiteParameter("@x", DbType.Int64) { Value = x });
            command.Parameters.Add(new SQLiteParameter("@y", DbType.Int64) { Value = y });
            command.Parameters.Add(new SQLiteParameter("@date", DbType.Int64) { Value = dateTicks });
            command.ExecuteNonQuery();
            return LastInsertRowId();
        }

        public long InsertRawLog(object? date, object? type, object? player, object? other, object? map, object? x, object? y, object? text)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = "INSERT INTO logs (text, log_type, playerid, otherid, mapid, mapx, mapy, log_date) "
                + "VALUES (@text, @type, @player, @other, @map, @x, @y, @date);";
            command.Parameters.Add(new SQLiteParameter("@text") { Value = text ?? DBNull.Value });
            command.Parameters.Add(new SQLiteParameter("@type") { Value = type ?? DBNull.Value });
            command.Parameters.Add(new SQLiteParameter("@player") { Value = player ?? DBNull.Value });
            command.Parameters.Add(new SQLiteParameter("@other") { Value = other ?? DBNull.Value });
            command.Parameters.Add(new SQLiteParameter("@map") { Value = map ?? DBNull.Value });
            command.Parameters.Add(new SQLiteParameter("@x") { Value = x ?? DBNull.Value });
            command.Parameters.Add(new SQLiteParameter("@y") { Value = y ?? DBNull.Value });
            command.Parameters.Add(new SQLiteParameter("@date") { Value = date ?? DBNull.Value });
            command.ExecuteNonQuery();
            return LastInsertRowId();
        }

        public void InsertPlayer(int id, string name)
            => Run("INSERT INTO players (player_id, player_name) VALUES (" + id + ", @name);",
                new SQLiteParameter("@name", DbType.String) { Value = name });

        public void RenamePlayer(int id, string name)
            => Run("UPDATE players SET player_name = @name WHERE player_id = " + id + ";",
                new SQLiteParameter("@name", DbType.String) { Value = name });

        public void DeletePlayer(int id)
            => Run("DELETE FROM players WHERE player_id = " + id + ";");

        public void InsertGuild(int id, string name)
            => Run("INSERT INTO guilds (guild_id, guild_name) VALUES (" + id + ", @name);",
                new SQLiteParameter("@name", DbType.String) { Value = name });

        public void InsertMap(int id, string name)
            => Run("INSERT INTO maps (map_id, map_name) VALUES (" + id + ", @name);",
                new SQLiteParameter("@name", DbType.String) { Value = name });

        public void InsertNpcTemplate(int id, string name)
            => Run("INSERT INTO npc_templates (npc_id, npc_name) VALUES (" + id + ", @name);",
                new SQLiteParameter("@name", DbType.String) { Value = name });

        public IReadOnlyList<LogQueryRow> Query(LogSearchQuery query, long snapshotCeiling = 1_000_000_000L, int limit = 51)
            => LogQueryReader.Read(Connection, LogQuerySqlBuilder.Build(query, snapshotCeiling, limit));

        public LogQueryCommand BuildCommand(LogSearchQuery query, long snapshotCeiling = 1_000_000_000L, int limit = 51)
            => LogQuerySqlBuilder.Build(query, snapshotCeiling, limit);

        public IReadOnlyList<string> ExplainQueryPlan(LogQueryCommand command)
        {
            using var dbCommand = Connection.CreateCommand();
            dbCommand.CommandText = "EXPLAIN QUERY PLAN " + command.CommandText;
            foreach (var parameter in command.Parameters)
                dbCommand.Parameters.Add(parameter);
            using var reader = dbCommand.ExecuteReader();
            var lines = new List<string>();
            while (reader.Read())
                lines.Add(reader.GetString(3));
            return lines;
        }

        public int CountLogs()
        {
            using var command = Connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM logs;";
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IReadOnlyList<string> LogDateStorageTypes()
        {
            using var command = Connection.CreateCommand();
            command.CommandText = "SELECT typeof(log_date) FROM logs ORDER BY rowid;";
            using var reader = command.ExecuteReader();
            var types = new List<string>();
            while (reader.Read())
                types.Add(reader.GetString(0));
            return types;
        }

        public void SeedPlanData()
        {
            using var transaction = Connection.BeginTransaction();
            using var command = Connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO logs (text, log_type, playerid, otherid, mapid, mapx, mapy, log_date) "
                + "VALUES (@text, @type, @player, @other, @map, @x, @y, @date);";
            var pText = command.Parameters.Add("@text", DbType.String);
            var pType = command.Parameters.Add("@type", DbType.Int64);
            var pPlayer = command.Parameters.Add("@player", DbType.Int64);
            var pOther = command.Parameters.Add("@other", DbType.Int64);
            var pMap = command.Parameters.Add("@map", DbType.Int64);
            var pX = command.Parameters.Add("@x", DbType.Int64);
            var pY = command.Parameters.Add("@y", DbType.Int64);
            var pDate = command.Parameters.Add("@date", DbType.Int64);
            long baseTicks = DateTime.UnixEpoch.Ticks + 10_000_000_000L;
            for (int i = 0; i < 10_000; i++)
            {
                pText.Value = "event " + i;
                pType.Value = i % 50;
                pPlayer.Value = i % 100;
                pOther.Value = i % 7 == 0 ? 0 : i % 40;
                pMap.Value = i % 3 == 0 ? 0 : i % 20;
                pX.Value = 10;
                pY.Value = 20;
                pDate.Value = baseTicks + (long)i * TimeSpan.TicksPerSecond;
                command.ExecuteNonQuery();
            }
            transaction.Commit();
            Run("ANALYZE;");
        }

        internal static LogSearchQuery SearchQuery(long startTicks, long endTicks, int? participant = null,
            int? map = null, IEnumerable<int>? types = null, string text = "", LogPageCursor? cursor = null)
        {
            return new(startTicks, endTicks, participant, map,
                (types ?? Array.Empty<int>()).ToImmutableArray(), text, cursor);
        }

        private long LastInsertRowId()
        {
            using var command = Connection.CreateCommand();
            command.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt64(command.ExecuteScalar());
        }

        private void Run(string sql, SQLiteParameter? parameter = null)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = sql;
            if (parameter is not null)
                command.Parameters.Add(parameter);
            command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            Connection.Dispose();
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var path = _dbPath + suffix;
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
