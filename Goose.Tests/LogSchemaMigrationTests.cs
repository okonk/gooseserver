using System.Data;
using System.Data.SQLite;
using System.Globalization;
using Goose.Logs;
using Xunit;

namespace Goose.Tests
{
    [Collection("ProcessEnvironment")]
    public class LogSchemaMigrationTests
    {
        private static readonly string[] AllIndexes =
        {
            "logs_log_date_idx", "logs_log_type_log_date_idx", "logs_mapid_log_date_idx",
            "logs_otherid_log_date_idx", "logs_playerid_log_date_idx",
        };

        [Fact]
        public void Fresh_schema_declares_integer_log_date_and_all_five_indexes()
        {
            using var connection = OpenDb(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "logs.sql")));

            var columnTypes = new Dictionary<string, string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA table_info(logs)";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    columnTypes[reader.GetString(1)] = reader.GetString(2).ToUpperInvariant();
            }
            Assert.Equal("INTEGER", columnTypes["log_date"]);

            Assert.Equal(AllIndexes, IndexNames(connection));
        }

        [Fact]
        public void Provider_datetime_text_under_non_utc_timezone_and_culture_becomes_utc_ticks()
        {
            using var scope = new ProcessEnvironmentScope("Pacific/Honolulu", new CultureInfo("fr-FR"));
            using var connection = OpenDb(OldDdl(dateNotNull: true));

            var now = DateTime.Now;
            var wall = now.AddTicks(-now.Ticks % TimeSpan.TicksPerSecond);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO logs (text, log_type, playerid, log_date) VALUES ('p', 0, 1, @d)";
                command.Parameters.Add(new SQLiteParameter("@d", DbType.DateTime2) { Value = wall });
                command.ExecuteNonQuery();
            }

            Assert.Equal("text", (string)Scalar(connection, "SELECT typeof(log_date) FROM logs"));
            string providerText = (string)Scalar(connection, "SELECT log_date FROM logs");
            Assert.Equal(wall.Ticks, DateTime.Parse(providerText, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal).Ticks);

            var result = LogSchemaMigrator.Migrate(connection);

            Assert.Equal("integer", (string)Scalar(connection, "SELECT typeof(log_date) FROM logs"));
            Assert.Equal(wall.Ticks, Long(connection, "SELECT log_date FROM logs"));
            Assert.Equal(1, result.TimestampsRepaired);
            Assert.Equal(0, result.MalformedTimestamps);
        }

        [Fact]
        public void Iso_text_variants_become_exact_utc_ticks()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            InsertLog(connection, 0, 1, "zoneless", 0, 0, 0, 0, "2024-01-15T10:30:45.1234567");
            InsertLog(connection, 0, 2, "z", 0, 0, 0, 0, "2024-01-15T10:30:45.1234567Z");
            InsertLog(connection, 0, 3, "offset", 0, 0, 0, 0, "2024-01-15T12:30:45.1234567+02:00");

            var result = LogSchemaMigrator.Migrate(connection);

            var expected = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc).AddTicks(1234567).Ticks;
            for (int playerid = 1; playerid <= 3; playerid++)
                Assert.Equal(expected, Long(connection, "SELECT log_date FROM logs WHERE playerid = " + playerid));
            Assert.Equal(3, result.TimestampsRepaired);
            Assert.Equal(0, result.MalformedTimestamps);
        }

        [Fact]
        public void Mixed_integer_and_text_timestamps_are_canonicalized_without_losing_ticks()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            var integerTicks = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(42).Ticks;
            InsertLog(connection, 0, 1, "integer", 0, 0, 0, 0, integerTicks);
            InsertLog(connection, 0, 2, "provider", 0, 0, 0, 0, "5/1/2024 12:30:45");
            InsertLog(connection, 0, 3, "iso", 0, 0, 0, 0, "2024-05-01T13:00:00.0000001");
            InsertLog(connection, 0, 4, "fraction", 0, 0, 0, 0, "2024-05-01T14:00:00.1234567");

            var result = LogSchemaMigrator.Migrate(connection);

            var expected = new long[]
            {
                integerTicks,
                new DateTime(2024, 5, 1, 12, 30, 45, DateTimeKind.Utc).Ticks,
                new DateTime(2024, 5, 1, 13, 0, 0, DateTimeKind.Utc).AddTicks(1).Ticks,
                new DateTime(2024, 5, 1, 14, 0, 0, DateTimeKind.Utc).AddTicks(1234567).Ticks,
            };
            for (int playerid = 1; playerid <= 4; playerid++)
                Assert.Equal(expected[playerid - 1], Long(connection, "SELECT log_date FROM logs WHERE playerid = " + playerid));
            Assert.Equal(3, result.TimestampsRepaired);
            Assert.Equal(0, result.MalformedTimestamps);
        }

        [Fact]
        public void Malformed_timestamp_storage_is_preserved_counted_and_not_integer()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: false));
            InsertLog(connection, 0, 1, "text", 0, 0, 0, 0, "not a date at all");
            InsertLog(connection, 0, 2, "null", 0, 0, 0, 0, null);
            InsertLog(connection, 0, 3, "blob", 0, 0, 0, 0, new byte[] { 0x00, 0xFF });
            InsertLog(connection, 0, 4, "real", 0, 0, 0, 0, 3.14);
            InsertLog(connection, 0, 5, "overflow", 0, 0, 0, 0, "99999999999999999999999999");
            InsertLog(connection, 0, 6, "negative", 0, 0, 0, 0, -1L);
            InsertLog(connection, 0, 7, "above-max", 0, 0, 0, 0, 4000000000000000000L);

            var result = LogSchemaMigrator.Migrate(connection);

            Assert.Equal(0, result.TimestampsRepaired);
            Assert.Equal(7, result.MalformedTimestamps);

            Assert.Equal("text", (string)Scalar(connection, "SELECT typeof(log_date) FROM logs WHERE playerid = 1"));
            Assert.Equal("not a date at all", (string)Scalar(connection, "SELECT log_date FROM logs WHERE playerid = 1"));
            Assert.Equal("null", (string)Scalar(connection, "SELECT typeof(log_date) FROM logs WHERE playerid = 2"));
            Assert.Equal(DBNull.Value, Scalar(connection, "SELECT log_date FROM logs WHERE playerid = 2"));
            Assert.Equal("blob", (string)Scalar(connection, "SELECT typeof(log_date) FROM logs WHERE playerid = 3"));
            Assert.Equal(new byte[] { 0x00, 0xFF }, (byte[])Scalar(connection, "SELECT log_date FROM logs WHERE playerid = 3")!);
            Assert.Equal("real", (string)Scalar(connection, "SELECT typeof(log_date) FROM logs WHERE playerid = 4"));
            Assert.Equal(3.14, (double)Scalar(connection, "SELECT log_date FROM logs WHERE playerid = 4")!);
            Assert.Equal("real", (string)Scalar(connection, "SELECT typeof(log_date) FROM logs WHERE playerid = 5"));
            Assert.Equal(double.Parse("99999999999999999999999999"), (double)Scalar(connection, "SELECT log_date FROM logs WHERE playerid = 5")!);
            Assert.Equal(-1L, Long(connection, "SELECT log_date FROM logs WHERE playerid = 6"));
            Assert.Equal(4000000000000000000L, Long(connection, "SELECT log_date FROM logs WHERE playerid = 7"));
        }

        [Fact]
        public void Timestamp_migration_orders_across_month_and_year_boundaries()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            InsertLog(connection, 0, 1, "a", 0, 0, 0, 0, "2023-12-31T23:59:59.9999999");
            InsertLog(connection, 0, 2, "b", 0, 0, 0, 0, "2024-01-01T00:00:00");
            InsertLog(connection, 0, 3, "c", 0, 0, 0, 0,
                new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999).Ticks);
            InsertLog(connection, 0, 4, "d", 0, 0, 0, 0, "2024-02-01T00:00:00.0000001");

            var result = LogSchemaMigrator.Migrate(connection);

            var stored = new List<long>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT log_date FROM logs ORDER BY log_date DESC";
                using var reader = command.ExecuteReader();
                while (reader.Read()) stored.Add(reader.GetInt64(0));
            }

            var expected = new long[]
            {
                new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1).Ticks,
                new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999).Ticks,
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks,
                new DateTime(2023, 12, 31, 23, 59, 59, DateTimeKind.Utc).AddTicks(9999999).Ticks,
            };
            Assert.Equal(expected, stored.ToArray());
            Assert.Equal(3, result.TimestampsRepaired);
            Assert.Equal(0, result.MalformedTimestamps);
        }

        [Fact]
        public void Existing_logs_table_gains_all_indexes_and_second_run_is_idempotent()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            Assert.Empty(IndexNames(connection));

            var first = LogSchemaMigrator.Migrate(connection);
            Assert.Equal(AllIndexes, IndexNames(connection));

            var second = LogSchemaMigrator.Migrate(connection);
            Assert.Equal(AllIndexes, IndexNames(connection));
            Assert.Equal(0, first.TimestampsRepaired);
            Assert.Equal(0, first.MalformedTimestamps);
            Assert.Equal(0, first.ClassChangesRepaired);
            Assert.Equal(0, first.MalformedClassChanges);
            Assert.Equal(0, first.RespawnMapsRepaired);
            Assert.Equal(0, first.MalformedRespawnMaps);
            Assert.Equal(0, second.TimestampsRepaired);
            Assert.Equal(0, second.MalformedTimestamps);
            Assert.Equal(0, second.ClassChangesRepaired);
            Assert.Equal(0, second.MalformedClassChanges);
            Assert.Equal(0, second.RespawnMapsRepaired);
            Assert.Equal(0, second.MalformedRespawnMaps);
        }

        [Fact]
        public void ClassChange_repair_moves_only_a_valid_positive_first_token()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            var date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            InsertLog(connection, 10002, 1, "205 Warrior 1.5", 0, 0, 0, 0, date);
            InsertLog(connection, 10002, 2, "205 Warrior 1.5", 205, 0, 0, 0, date);
            InsertLog(connection, 10002, 3, "", 0, 0, 0, 0, date);
            InsertLog(connection, 10002, 4, "abc Warrior", 0, 0, 0, 0, date);
            InsertLog(connection, 10002, 5, "0 Warrior", 0, 0, 0, 0, date);
            InsertLog(connection, 10002, 6, "-5 Warrior", 0, 0, 0, 0, date);
            InsertLog(connection, 10002, 7, "4294967296 Warrior", 0, 0, 0, 0, date);
            InsertLog(connection, 10002, 8, "206 Warrior", null, 0, 0, 0, date);

            var result = LogSchemaMigrator.Migrate(connection);

            Assert.Equal(205L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 1"));
            Assert.Equal(205L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 2"));
            Assert.Equal(0L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 3"));
            Assert.Equal(0L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 4"));
            Assert.Equal(0L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 5"));
            Assert.Equal(0L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 6"));
            Assert.Equal(0L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 7"));
            Assert.Equal(206L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 8"));
            Assert.Equal(2, result.ClassChangesRepaired);
            Assert.Equal(5, result.MalformedClassChanges);
        }

        [Fact]
        public void RespawnMap_repair_unshifts_only_a_fully_valid_legacy_signature()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            var date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            InsertLog(connection, 10005, 1, "", 7, 12, 34, 0, date);
            InsertLog(connection, 10005, 2, "", "7abc", 12, 34, 0, date);
            InsertLog(connection, 10005, 3, "", null, 12, 34, 0, date);
            InsertLog(connection, 10005, 4, "", 7, -1, 34, 0, date);
            InsertLog(connection, 10005, 5, "", 7, 4294967296L, 34, 0, date);
            InsertLog(connection, 10005, 6, "", 7, 12, -3, 0, date);
            InsertLog(connection, 10005, 7, "", 7, 12, 4294967296L, 0, date);
            InsertLog(connection, 10005, 8, "", 7, 12, 34, 4, date);
            InsertLog(connection, 10005, 9, "", 7, 12, 34, -1, date);
            InsertLog(connection, 10005, 10, "", 7, null, 34, 0, date);
            InsertLog(connection, 10005, 11, "", -7, 12, 34, 0, date);
            InsertLog(connection, 10005, 12, "", 4294967296L, 12, 34, 0, date);

            var result = LogSchemaMigrator.Migrate(connection);

            Assert.Equal((0L, 7L, 12L, 34L), Row(connection, 1));
            Assert.Equal("text", (string)Scalar(connection, "SELECT typeof(otherid) FROM logs WHERE playerid = 2"));
            Assert.Equal("7abc", (string)Scalar(connection, "SELECT CAST(otherid AS TEXT) FROM logs WHERE playerid = 2"));
            Assert.Equal((DBNull.Value, 12L, 34L, 0L), Row(connection, 3));
            Assert.Equal((7L, -1L, 34L, 0L), Row(connection, 4));
            Assert.Equal((7L, 4294967296L, 34L, 0L), Row(connection, 5));
            Assert.Equal((7L, 12L, -3L, 0L), Row(connection, 6));
            Assert.Equal((7L, 12L, 4294967296L, 0L), Row(connection, 7));
            Assert.Equal((7L, 12L, 34L, 4L), Row(connection, 8));
            Assert.Equal((7L, 12L, 34L, -1L), Row(connection, 9));
            Assert.Equal((7L, DBNull.Value, 34L, 0L), Row(connection, 10));
            Assert.Equal((-7L, 12L, 34L, 0L), Row(connection, 11));
            Assert.Equal((4294967296L, 12L, 34L, 0L), Row(connection, 12));
            Assert.Equal(1, result.RespawnMapsRepaired);
            Assert.Equal(10, result.MalformedRespawnMaps);
        }

        [Fact]
        public void Migration_materializes_candidates_before_updates()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            var date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            for (int i = 1; i <= 5; i++)
                InsertLog(connection, 0, i, "ts" + i, 0, 0, 0, 0, "2024-03-0" + i + "T00:00:00");
            for (int i = 6; i <= 8; i++)
                InsertLog(connection, 10002, i, i * 100 + " Warrior", 0, 0, 0, 0, date);
            for (int i = 9; i <= 11; i++)
                InsertLog(connection, 10005, i, "", i, 1, 2, 0, date);

            var result = LogSchemaMigrator.Migrate(connection);

            Assert.Equal(5, result.TimestampsRepaired);
            Assert.Equal(3, result.ClassChangesRepaired);
            Assert.Equal(3, result.RespawnMapsRepaired);
            for (int i = 1; i <= 5; i++)
                Assert.Equal(new DateTime(2024, 3, i, 0, 0, 0, DateTimeKind.Utc).Ticks,
                    Long(connection, "SELECT log_date FROM logs WHERE playerid = " + i));
            for (int i = 6; i <= 8; i++)
                Assert.Equal((long)(i * 100), Long(connection, "SELECT otherid FROM logs WHERE playerid = " + i));
            for (int i = 9; i <= 11; i++)
                Assert.Equal((0L, (long)i, 1L, 2L), Row(connection, i));
        }

        [Fact]
        public void Second_run_reports_zero_repairs_but_recounts_preserved_malformed_rows()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            var date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            InsertLog(connection, 0, 1, "ok", 0, 0, 0, 0, "2024-03-01T00:00:00");
            InsertLog(connection, 0, 2, "bad", 0, 0, 0, 0, "not a date at all");
            InsertLog(connection, 10002, 3, "205 Warrior", 0, 0, 0, 0, date);
            InsertLog(connection, 10002, 4, "abc Warrior", 0, 0, 0, 0, date);
            InsertLog(connection, 10005, 5, "", 7, 12, 34, 0, date);
            InsertLog(connection, 10005, 6, "", 7, 12, 34, 4, date);

            using (var log = new CapturingLog())
            {
                var first = LogSchemaMigrator.Migrate(connection);
                Assert.Equal(1, first.TimestampsRepaired);
                Assert.Equal(1, first.MalformedTimestamps);
                Assert.Equal(1, first.ClassChangesRepaired);
                Assert.Equal(1, first.MalformedClassChanges);
                Assert.Equal(1, first.RespawnMapsRepaired);
                Assert.Equal(1, first.MalformedRespawnMaps);
                Assert.True(log.Messages.Any(m => m.Contains("1 malformed")));
            }

            using (var log = new CapturingLog())
            {
                var second = LogSchemaMigrator.Migrate(connection);
                Assert.Equal(0, second.TimestampsRepaired);
                Assert.Equal(1, second.MalformedTimestamps);
                Assert.Equal(0, second.ClassChangesRepaired);
                Assert.Equal(1, second.MalformedClassChanges);
                Assert.Equal(0, second.RespawnMapsRepaired);
                Assert.Equal(1, second.MalformedRespawnMaps);
                Assert.True(log.Messages.Any(m => m.Contains("1 malformed")));
            }
        }

        [Fact]
        public void Timestamp_repairs_and_indexes_are_atomic()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            var date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            InsertLog(connection, 0, 1, "ts", 0, 0, 0, 0, "2024-03-01T00:00:00");
            InsertLog(connection, 10002, 2, "205 Warrior", 0, 0, 0, 0, date);
            InsertLog(connection, 10005, 3, "", 7, 12, 34, 0, date);
            Execute(connection, "CREATE VIEW logs_log_date_idx AS SELECT 1");

            Assert.ThrowsAny<Exception>(() => LogSchemaMigrator.Migrate(connection));

            Assert.Equal("text", (string)Scalar(connection, "SELECT typeof(log_date) FROM logs WHERE playerid = 1"));
            Assert.Equal("2024-03-01T00:00:00", (string)Scalar(connection, "SELECT log_date FROM logs WHERE playerid = 1"));
            Assert.Equal(0L, Long(connection, "SELECT otherid FROM logs WHERE playerid = 2"));
            Assert.Equal((7L, 12L, 34L, 0L), Row(connection, 3));
            Assert.Empty(IndexNames(connection));
        }

        [Fact]
        public void Counts_are_logged_only_after_commit()
        {
            using var connection = OpenDb(OldDdl(dateNotNull: true));
            var date = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            InsertLog(connection, 0, 1, "ts", 0, 0, 0, 0, "2024-03-01T00:00:00");
            Execute(connection, "CREATE VIEW logs_log_date_idx AS SELECT 1");

            using var log = new CapturingLog();
            Assert.ThrowsAny<Exception>(() => LogSchemaMigrator.Migrate(connection));

            Assert.DoesNotContain(log.Messages, m => m.Contains("Logs migration:"));
        }

        private static string OldDdl(bool dateNotNull)
        {
            return "CREATE TABLE logs (\n" +
                "  text TEXT,\n" +
                "  log_type INT NOT NULL,\n" +
                "  playerid INT NOT NULL,\n" +
                "  otherid INT,\n" +
                "  mapid SMALLINT,\n" +
                "  mapx SMALLINT,\n" +
                "  mapy SMALLINT,\n" +
                "  log_date DATETIME2" + (dateNotNull ? " NOT NULL" : "") + "\n)";
        }

        private static SQLiteConnection OpenDb(string ddl)
        {
            var connection = new SQLiteConnection("Data Source=:memory:");
            connection.Open();
            Execute(connection, ddl);
            return connection;
        }

        private static void Execute(SQLiteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static object? Scalar(SQLiteConnection connection, string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return command.ExecuteScalar();
        }

        private static long Long(SQLiteConnection connection, string sql)
        {
            return Convert.ToInt64(Scalar(connection, sql));
        }

        private static List<string> IndexNames(SQLiteConnection connection)
        {
            var names = new List<string>();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA index_list(logs)";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                names.Add(reader.GetString(1));
            return names.OrderBy(n => n).ToList();
        }

        private static void InsertLog(SQLiteConnection connection, int logType, int playerid, string? text,
            object? otherid, object? mapid, object? mapx, object? mapy, object? logDate)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                "INSERT INTO logs (text, log_type, playerid, otherid, mapid, mapx, mapy, log_date) " +
                "VALUES (@text, @logType, @playerid, @otherid, @mapid, @mapx, @mapy, @logDate)";
            command.Parameters.AddWithValue("@text", (object?)text ?? DBNull.Value);
            command.Parameters.AddWithValue("@logType", logType);
            command.Parameters.AddWithValue("@playerid", playerid);
            command.Parameters.AddWithValue("@otherid", otherid ?? DBNull.Value);
            command.Parameters.AddWithValue("@mapid", mapid ?? DBNull.Value);
            command.Parameters.AddWithValue("@mapx", mapx ?? DBNull.Value);
            command.Parameters.AddWithValue("@mapy", mapy ?? DBNull.Value);
            command.Parameters.AddWithValue("@logDate", logDate ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        private static (object? otherid, object? mapid, object? mapx, object? mapy) Row(SQLiteConnection connection, int playerid)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT CAST(otherid AS INTEGER), CAST(mapid AS INTEGER), CAST(mapx AS INTEGER), CAST(mapy AS INTEGER) FROM logs WHERE playerid = " + playerid;
            using var reader = command.ExecuteReader();
            reader.Read();
            return (AsLong(reader.GetValue(0)), AsLong(reader.GetValue(1)), AsLong(reader.GetValue(2)), AsLong(reader.GetValue(3)));
        }

        private static object? AsLong(object? value)
        {
            return value is long longValue ? (object)longValue
                : value is int intValue ? (object)(long)intValue
                : value is short shortValue ? (object)(long)shortValue
                : value;
        }
    }
}
