using System.Data;
using System.Data.SQLite;
using System.Globalization;

namespace Goose.Logs
{
    public sealed class LogMigrationResult
    {
        public int TimestampsRepaired { get; }
        public int MalformedTimestamps { get; }
        public int ClassChangesRepaired { get; }
        public int MalformedClassChanges { get; }
        public int RespawnMapsRepaired { get; }
        public int MalformedRespawnMaps { get; }

        public LogMigrationResult(int timestampsRepaired, int malformedTimestamps,
            int classChangesRepaired, int malformedClassChanges,
            int respawnMapsRepaired, int malformedRespawnMaps)
        {
            this.TimestampsRepaired = timestampsRepaired;
            this.MalformedTimestamps = malformedTimestamps;
            this.ClassChangesRepaired = classChangesRepaired;
            this.MalformedClassChanges = malformedClassChanges;
            this.RespawnMapsRepaired = respawnMapsRepaired;
            this.MalformedRespawnMaps = malformedRespawnMaps;
        }
    }

    public static class LogSchemaMigrator
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private const long MaxTicks = 3155378975999999999;
        private const int ClassChangeType = 10002;
        private const int RespawnMapType = 10005;
        private static readonly DateTimeStyles UtcStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        private static readonly string[] CompactIsoFormats =
        {
            "yyyyMMddHHmmss", "yyyyMMddTHHmmss", "yyyyMMddHHmmss.fffffff", "yyyyMMddTHHmmss.fffffff",
        };
        private static readonly string[] IndexStatements =
        {
            "CREATE INDEX IF NOT EXISTS logs_log_date_idx ON logs (log_date)",
            "CREATE INDEX IF NOT EXISTS logs_playerid_log_date_idx ON logs (playerid, log_date)",
            "CREATE INDEX IF NOT EXISTS logs_otherid_log_date_idx ON logs (otherid, log_date)",
            "CREATE INDEX IF NOT EXISTS logs_log_type_log_date_idx ON logs (log_type, log_date)",
            "CREATE INDEX IF NOT EXISTS logs_mapid_log_date_idx ON logs (mapid, log_date)",
        };

        private readonly record struct TimestampCandidate(long RowId, string StorageType, object? Value);
        private readonly record struct ClassChangeCandidate(long RowId, string? Text);
        private readonly record struct RespawnMapCandidate(long RowId,
            string OtherIdStorage, string MapIdStorage, string MapXStorage, string MapYStorage,
            long OtherId, long MapId, long MapX, long MapY);

        public static LogMigrationResult Migrate(SQLiteConnection connection)
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                var timestamps = SelectTimestampCandidates(connection, transaction);
                var classChanges = SelectClassChangeCandidates(connection, transaction);
                var respawnMaps = SelectRespawnMapCandidates(connection, transaction);

                int timestampsRepaired = 0, malformedTimestamps = 0;
                foreach (var candidate in timestamps)
                {
                    if (TryParseTimestamp(candidate.StorageType, candidate.Value, out long? ticks) && ticks is not null)
                    {
                        UpdateRow(connection, transaction,
                            "UPDATE logs SET log_date = @value WHERE rowid = @rowid",
                            candidate.RowId, new SQLiteParameter("@value", DbType.Int64) { Value = ticks.Value });
                        timestampsRepaired++;
                    }
                    else
                    {
                        malformedTimestamps++;
                    }
                }

                int classChangesRepaired = 0, malformedClassChanges = 0;
                foreach (var candidate in classChanges)
                {
                    string? firstToken = candidate.Text is null
                        ? null
                        : candidate.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (firstToken is not null &&
                        int.TryParse(firstToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out int targetId) &&
                        targetId >= 1)
                    {
                        UpdateRow(connection, transaction,
                            "UPDATE logs SET otherid = @value WHERE rowid = @rowid",
                            candidate.RowId, new SQLiteParameter("@value", DbType.Int32) { Value = targetId });
                        classChangesRepaired++;
                    }
                    else
                    {
                        malformedClassChanges++;
                    }
                }

                int respawnMapsRepaired = 0, malformedRespawnMaps = 0;
                foreach (var candidate in respawnMaps)
                {
                    if (candidate.OtherIdStorage == "integer" && candidate.MapIdStorage == "integer" &&
                        candidate.MapXStorage == "integer" && candidate.MapYStorage == "integer" &&
                        candidate.OtherId >= 1 && candidate.OtherId <= int.MaxValue &&
                        candidate.MapId >= 0 && candidate.MapId <= int.MaxValue &&
                        candidate.MapX >= 0 && candidate.MapX <= int.MaxValue &&
                        candidate.MapY == 0)
                    {
                        using var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = "UPDATE logs SET otherid = 0, mapid = @mapid, mapx = @mapx, mapy = @mapy WHERE rowid = @rowid";
                        command.Parameters.Add(new SQLiteParameter("@mapid", DbType.Int64) { Value = candidate.OtherId });
                        command.Parameters.Add(new SQLiteParameter("@mapx", DbType.Int64) { Value = candidate.MapId });
                        command.Parameters.Add(new SQLiteParameter("@mapy", DbType.Int64) { Value = candidate.MapX });
                        command.Parameters.Add(new SQLiteParameter("@rowid", DbType.Int64) { Value = candidate.RowId });
                        command.ExecuteNonQuery();
                        respawnMapsRepaired++;
                    }
                    else
                    {
                        malformedRespawnMaps++;
                    }
                }

                foreach (var statement in IndexStatements)
                    Run(connection, transaction, statement);

                transaction.Commit();

                log.Info("Logs migration: {0} timestamp(s) repaired, {1} malformed timestamp(s) preserved",
                    timestampsRepaired, malformedTimestamps);
                log.Info("Logs migration: {0} ClassChange row(s) repaired, {1} malformed row(s) preserved",
                    classChangesRepaired, malformedClassChanges);
                log.Info("Logs migration: {0} RespawnMap row(s) repaired, {1} malformed row(s) preserved",
                    respawnMapsRepaired, malformedRespawnMaps);

                return new LogMigrationResult(timestampsRepaired, malformedTimestamps,
                    classChangesRepaired, malformedClassChanges, respawnMapsRepaired, malformedRespawnMaps);
            }
            catch (Exception e)
            {
                try { transaction.Rollback(); } catch { }
                log.Error(e, "Log schema migration failed, changes rolled back");
                throw;
            }
        }

        private static List<TimestampCandidate> SelectTimestampCandidates(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            var candidates = new List<TimestampCandidate>();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "SELECT rowid, typeof(log_date), log_date FROM logs " +
                "WHERE typeof(log_date) != 'integer' OR log_date < 0 OR log_date > " + MaxTicks;
            using var reader = command.ExecuteReader();
            while (reader.Read())
                candidates.Add(new TimestampCandidate(reader.GetInt64(0), reader.GetString(1), reader.GetValue(2)));
            return candidates;
        }

        private static List<ClassChangeCandidate> SelectClassChangeCandidates(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            var candidates = new List<ClassChangeCandidate>();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "SELECT rowid, text FROM logs WHERE log_type = " + ClassChangeType + " AND (otherid IS NULL OR otherid = 0)";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                candidates.Add(new ClassChangeCandidate(reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            return candidates;
        }

        private static List<RespawnMapCandidate> SelectRespawnMapCandidates(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            var candidates = new List<RespawnMapCandidate>();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "SELECT rowid, typeof(otherid), typeof(mapid), typeof(mapx), typeof(mapy), " +
                "CAST(otherid AS INTEGER), CAST(mapid AS INTEGER), CAST(mapx AS INTEGER), CAST(mapy AS INTEGER) " +
                "FROM logs WHERE log_type = " + RespawnMapType + " AND otherid IS NOT NULL AND otherid != 0";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                candidates.Add(new RespawnMapCandidate(
                    reader.GetInt64(0),
                    reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    AsLong(reader.GetValue(5)), AsLong(reader.GetValue(6)), AsLong(reader.GetValue(7)), AsLong(reader.GetValue(8))));
            }
            return candidates;
        }

        private static bool TryParseTimestamp(string storageType, object? value, out long? ticks)
        {
            ticks = null;
            switch (storageType)
            {
                case "integer":
                    long integerTicks = Convert.ToInt64(value);
                    if (integerTicks >= 0 && integerTicks <= MaxTicks) ticks = integerTicks;
                    return ticks is not null;
                case "text":
                    return TryParseText((string)value!, out ticks);
                default:
                    return false;
            }
        }

        private static bool TryParseText(string text, out long? ticks)
        {
            ticks = null;
            if (TryParseIso(text, out var iso))
            {
                ticks = iso.Ticks;
                return true;
            }
            if (DateTime.TryParse(text, CultureInfo.CurrentCulture, UtcStyles, out var localized))
            {
                ticks = localized.Ticks;
                return true;
            }
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long decimalTicks) &&
                decimalTicks >= 0 && decimalTicks <= MaxTicks)
            {
                ticks = decimalTicks;
                return true;
            }
            return false;
        }

        private static bool TryParseIso(string text, out DateTime parsed)
        {
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, UtcStyles, out parsed))
                return true;
            // .NET's TryParse rejects compact ISO forms, and they must match before the
            // decimal-tick fallback or "yyyyMMddHHmmss" text would be read as a tick count.
            foreach (string format in CompactIsoFormats)
            {
                if (DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, UtcStyles, out parsed))
                    return true;
            }
            return false;
        }

        private static void UpdateRow(SQLiteConnection connection, SQLiteTransaction transaction,
            string sql, long rowId, SQLiteParameter value)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.Add(value);
            command.Parameters.Add(new SQLiteParameter("@rowid", DbType.Int64) { Value = rowId });
            command.ExecuteNonQuery();
        }

        private static void Run(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        private static long AsLong(object? value)
        {
            return value is long longValue ? longValue
                : value is int intValue ? intValue
                : value is short shortValue ? shortValue
                : 0;
        }
    }
}
