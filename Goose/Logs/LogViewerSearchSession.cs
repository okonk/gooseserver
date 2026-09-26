namespace Goose.Logs
{
    internal sealed class LogViewerSearchSession
    {
        public LogSearchQuery Base { get; }
        public int Generation { get; }
        public string FirstPageToken { get; }

        private readonly Dictionary<string, LogPageCursor?> tokens = new(StringComparer.Ordinal);
        private readonly Dictionary<LogPageCursor, string> tokenByCursor = new();
        private readonly Func<string> tokenSource;
        private LogPageCursor? firstPageReplayCursor;
        private LogQueryRow[]? firstPageRows;
        private bool cleared;

        public LogViewerSearchSession(LogSearchQuery baseQuery, int generation, Func<string>? tokenSource = null)
        {
            if (baseQuery is null || baseQuery.Cursor is not null)
                throw new ArgumentException("Base query must be cursorless.", nameof(baseQuery));
            Base = baseQuery;
            Generation = generation;
            this.tokenSource = tokenSource ?? LogPageTokenCodec.Create;
            FirstPageToken = Issue(null);
        }

        public string IssueToken(LogPageCursor cursor) => Issue(cursor);

        public void StoreFirstPage(IReadOnlyList<LogQueryRow> rows, LogPageCursor? nextCursor)
        {
            firstPageRows = rows.ToArray();
            if (nextCursor is not null)
                firstPageReplayCursor = LogPageCursor.ForFirstPage(nextCursor.SnapshotCeiling);
        }

        public bool TryResolve(string? token, out LogSearchQuery? query, out IReadOnlyList<LogQueryRow>? storedRows)
        {
            query = null;
            storedRows = null;
            if (cleared || token is null || !tokens.TryGetValue(token, out LogPageCursor? cursor))
                return false;
            if (cursor is null)
            {
                if (firstPageReplayCursor is not null)
                    query = Base.WithCursor(firstPageReplayCursor);
                else if (firstPageRows is not null)
                    storedRows = firstPageRows;
                else
                    query = Base;
                return true;
            }
            query = Base.WithCursor(cursor);
            return true;
        }

        public void Clear()
        {
            cleared = true;
            tokens.Clear();
            tokenByCursor.Clear();
        }

        private string Issue(LogPageCursor? cursor)
        {
            if (cursor is not null && tokenByCursor.TryGetValue(cursor, out string? existing))
                return existing;
            while (true)
            {
                string token = tokenSource();
                if (tokens.ContainsKey(token))
                    continue;
                tokens[token] = cursor;
                if (cursor is not null)
                    tokenByCursor[cursor] = token;
                return token;
            }
        }
    }
}
