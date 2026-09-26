using System.Data;
using System.Data.SQLite;
using System.Text;

namespace Goose.Logs
{
    public sealed record LogQueryCommand(string CommandText, IReadOnlyList<SQLiteParameter> Parameters);

    public static class LogQuerySqlBuilder
    {
        // CAST keeps the provider from coercing out-of-range values to the declared column type.
        private const string SelectColumns =
            "rowid, CAST(log_date AS INTEGER) AS log_date, " +
            "typeof(log_type) AS log_type_type, CAST(log_type AS INTEGER) AS log_type, " +
            "typeof(playerid) AS playerid_type, CAST(playerid AS INTEGER) AS playerid, " +
            "typeof(otherid) AS otherid_type, CAST(otherid AS INTEGER) AS otherid, " +
            "typeof(mapid) AS mapid_type, CAST(mapid AS INTEGER) AS mapid, " +
            "typeof(mapx) AS mapx_type, CAST(mapx AS INTEGER) AS mapx, " +
            "typeof(mapy) AS mapy_type, CAST(mapy AS INTEGER) AS mapy, text";

        public static LogQueryCommand Build(LogSearchQuery query, long snapshotCeiling, int limit)
        {
            var parameters = new List<SQLiteParameter>
            {
                Int64("@startTicks", query.StartUtcTicks),
                Int64("@endTicks", query.EndUtcTicks),
                Int64("@snapshotCeiling", snapshotCeiling),
                Int64("@limit", limit),
            };

            var predicates = new List<string>
            {
                "typeof(log_date) = 'integer'",
                "log_date >= @startTicks",
                "log_date < @endTicks",
                "rowid <= @snapshotCeiling",
            };

            if (query.EventTypeIds.Length > 0)
            {
                var names = new List<string>(query.EventTypeIds.Length);
                for (int i = 0; i < query.EventTypeIds.Length; i++)
                {
                    string name = "@type" + i;
                    names.Add(name);
                    parameters.Add(new SQLiteParameter(name, DbType.Int32) { Value = query.EventTypeIds[i] });
                }
                predicates.Add("log_type IN (" + string.Join(", ", names) + ")");
            }

            if (query.MapId is int mapId)
            {
                parameters.Add(Int64("@mapId", mapId));
                predicates.Add("mapid = @mapId");
            }

            if (query.Text.Length > 0)
            {
                parameters.Add(new SQLiteParameter("@text", DbType.String) { Value = EscapeLike(query.Text) });
                predicates.Add("text LIKE @text ESCAPE '\\' COLLATE NOCASE");
            }

            if (query.Cursor is LogPageCursor cursor
                && cursor.BeforeUtcTicks is long beforeTicks
                && cursor.BeforeRowId is long beforeRowId)
            {
                parameters.Add(Int64("@beforeTicks", beforeTicks));
                parameters.Add(Int64("@beforeRowId", beforeRowId));
                predicates.Add("(log_date < @beforeTicks OR (log_date = @beforeTicks AND rowid < @beforeRowId))");
            }

            string where = string.Join("\n  AND ", predicates);
            string branch = "SELECT " + SelectColumns + "\nFROM logs\nWHERE " + where;
            string body = branch;

            if (query.ParticipantId is int participantId)
            {
                parameters.Add(Int64("@participantId", participantId));
                string primary = branch + "\n  AND playerid = @participantId";

                var typeNames = new List<string>();
                int index = 0;
                foreach (int typeId in LogEventRegistry.PlayerValuedOtherTypeIds.OrderBy(id => id))
                {
                    string name = "@participantType" + index++;
                    typeNames.Add(name);
                    parameters.Add(new SQLiteParameter(name, DbType.Int32) { Value = typeId });
                }
                string secondary = branch
                    + "\n  AND otherid = @participantId\n  AND log_type IN (" + string.Join(", ", typeNames) + ")";
                body = primary + "\nUNION\n" + secondary;
            }

            string commandText = "SELECT * FROM (" + body + ")"
                + "\nORDER BY log_date DESC, rowid DESC\nLIMIT @limit;";
            return new LogQueryCommand(commandText, parameters);
        }

        internal static string EscapeLike(string text)
        {
            var builder = new StringBuilder(text.Length + 2);
            builder.Append('%');
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '%': builder.Append("\\%"); break;
                    case '_': builder.Append("\\_"); break;
                    default: builder.Append(c); break;
                }
            }
            builder.Append('%');
            return builder.ToString();
        }

        private static SQLiteParameter Int64(string name, long value)
            => new(name, DbType.Int64) { Value = value };
    }
}
