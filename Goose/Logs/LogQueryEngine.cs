using System.Data.SQLite;

namespace Goose.Logs
{
    internal sealed class LogSearchPage
    {
        public IReadOnlyList<LogQueryRow> Rows { get; }
        public bool HasMore { get; }
        public LogPageCursor? NextCursor { get; }

        internal LogSearchPage(IReadOnlyList<LogQueryRow> rows, bool hasMore, LogPageCursor? nextCursor)
        {
            Rows = rows;
            HasMore = hasMore;
            NextCursor = nextCursor;
        }
    }

    internal static class LogQueryEngine
    {
        public const int PageSize = 50;
        internal const string SnapshotSql = "SELECT COALESCE(MAX(rowid), 0) FROM logs;";

        public static LogSearchPage Execute(SQLiteConnection connection, LogSearchQuery query)
        {
            long snapshotCeiling;
            if (query.Cursor is LogPageCursor cursor)
            {
                ValidateCursor(cursor, query);
                snapshotCeiling = cursor.SnapshotCeiling;
            }
            else
            {
                snapshotCeiling = CaptureSnapshotCeiling(connection);
            }

            var command = LogQuerySqlBuilder.Build(query, snapshotCeiling, PageSize + 1);
            var candidates = LogQueryReader.Read(connection, command);
            bool hasMore = candidates.Count == PageSize + 1;
            if (!hasMore)
                return new LogSearchPage(candidates, false, null);

            var retained = candidates.Take(PageSize).ToList();
            var last = retained[^1];
            return new LogSearchPage(retained, true,
                new LogPageCursor(snapshotCeiling, last.UtcTicks, last.RowId));
        }

        private static long CaptureSnapshotCeiling(SQLiteConnection connection)
        {
            using var dbCommand = connection.CreateCommand();
            dbCommand.CommandText = SnapshotSql;
            return Convert.ToInt64(dbCommand.ExecuteScalar());
        }

        private static void ValidateCursor(LogPageCursor cursor, LogSearchQuery query)
        {
            if (cursor.SnapshotCeiling < 0)
                throw new ArgumentException("Cursor snapshot ceiling must be non-negative.", nameof(query));
            if (cursor.BeforeUtcTicks is null != cursor.BeforeRowId is null)
                throw new ArgumentException("Cursor boundary ticks and row id must both be present or both absent.", nameof(query));
            if (cursor.BeforeUtcTicks is long beforeTicks)
            {
                if (beforeTicks < 0 || beforeTicks > DateTime.MaxValue.Ticks)
                    throw new ArgumentException("Cursor boundary ticks are outside the DateTime domain.", nameof(query));
                if (beforeTicks < query.StartUtcTicks || beforeTicks >= query.EndUtcTicks)
                    throw new ArgumentException("Cursor boundary ticks are outside the query interval.", nameof(query));
            }
            if (cursor.BeforeRowId is long beforeRowId)
            {
                if (beforeRowId <= 0)
                    throw new ArgumentException("Cursor boundary row id must be positive.", nameof(query));
                if (beforeRowId > cursor.SnapshotCeiling)
                    throw new ArgumentException("Cursor boundary row id exceeds the snapshot ceiling.", nameof(query));
            }
        }
    }
}
