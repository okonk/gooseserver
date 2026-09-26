using System.Data.SQLite;

namespace Goose.Logs
{
    public sealed class LogQueryRow
    {
        public long RowId { get; }
        public long UtcTicks { get; }
        public long Type { get; }
        public bool TypeIsInteger { get; }
        public long PlayerId { get; }
        public bool PlayerIdIsInteger { get; }
        public long OtherId { get; }
        public bool OtherIdIsInteger { get; }
        public long MapId { get; }
        public bool MapIdIsInteger { get; }
        public long MapX { get; }
        public bool MapXIsInteger { get; }
        public long MapY { get; }
        public bool MapYIsInteger { get; }
        public string TypeLabel { get; }
        public string GroupLabel { get; }
        public LogRelatedEntity? Primary { get; }
        public LogRelatedEntity? Related { get; }
        public LogRelatedEntity? Map { get; }
        public string Summary { get; }
        public string Text { get; }

        internal LogQueryRow(long rowId, long utcTicks, long type, bool typeIsInteger,
            long playerId, bool playerIdIsInteger, long otherId, bool otherIdIsInteger,
            long mapId, bool mapIdIsInteger, long mapX, bool mapXIsInteger, long mapY, bool mapYIsInteger,
            string typeLabel, string groupLabel, LogRelatedEntity? primary, LogRelatedEntity? related,
            LogRelatedEntity? map, string summary, string text)
        {
            RowId = rowId;
            UtcTicks = utcTicks;
            Type = type;
            TypeIsInteger = typeIsInteger;
            PlayerId = playerId;
            PlayerIdIsInteger = playerIdIsInteger;
            OtherId = otherId;
            OtherIdIsInteger = otherIdIsInteger;
            MapId = mapId;
            MapIdIsInteger = mapIdIsInteger;
            MapX = mapX;
            MapXIsInteger = mapXIsInteger;
            MapY = mapY;
            MapYIsInteger = mapYIsInteger;
            TypeLabel = typeLabel;
            GroupLabel = groupLabel;
            Primary = primary;
            Related = related;
            Map = map;
            Summary = summary;
            Text = text;
        }
    }

    internal readonly record struct LogRawRow(
        long RowId, long DateTicks,
        (long Value, bool IsInteger) Type,
        (long Value, bool IsInteger) PlayerId,
        (long Value, bool IsInteger) OtherId,
        (long Value, bool IsInteger) MapId,
        (long Value, bool IsInteger) MapX,
        (long Value, bool IsInteger) MapY,
        string Text);

    public static class LogQueryReader
    {
        public static IReadOnlyList<LogQueryRow> Read(SQLiteConnection connection, LogQueryCommand command)
        {
            var raw = new List<LogRawRow>();
            using (var dbCommand = connection.CreateCommand())
            {
                dbCommand.CommandText = command.CommandText;
                foreach (var parameter in command.Parameters)
                    dbCommand.Parameters.Add(parameter);
                using var reader = dbCommand.ExecuteReader();
                while (reader.Read())
                {
                    raw.Add(new LogRawRow(
                        reader.GetInt64(0),
                        reader.GetInt64(1),
                        ReadField(reader, 2, 3),
                        ReadField(reader, 4, 5),
                        ReadField(reader, 6, 7),
                        ReadField(reader, 8, 9),
                        ReadField(reader, 10, 11),
                        ReadField(reader, 12, 13),
                        reader.IsDBNull(14) ? string.Empty : reader.GetString(14)));
                }
            }

            LogEntityNames names = LogEntityResolver.Load(connection,
                PlayerCandidates(raw), GuildCandidates(raw), NpcCandidates(raw), MapCandidates(raw));

            var rows = new List<LogQueryRow>(raw.Count);
            foreach (var r in raw)
                rows.Add(Project(r, names));
            return rows;
        }

        private static (long Value, bool IsInteger) ReadField(SQLiteDataReader reader, int typeColumn, int valueColumn)
        {
            // Storage class comes from typeof(); CAST results for non-integer storage are coercions, not IDs.
            if (reader.GetString(typeColumn) != "integer")
                return (0, false);
            return (reader.GetInt64(valueColumn), true);
        }

        private static IEnumerable<long> PlayerCandidates(IReadOnlyList<LogRawRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.PlayerId.IsInteger)
                    yield return row.PlayerId.Value;
                if (row.OtherId.IsInteger && OtherKind(row) == LogOtherIdKind.Player)
                    yield return row.OtherId.Value;
            }
        }

        private static IEnumerable<long> GuildCandidates(IReadOnlyList<LogRawRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.OtherId.IsInteger && OtherKind(row) == LogOtherIdKind.Guild)
                    yield return row.OtherId.Value;
            }
        }

        private static IEnumerable<long> NpcCandidates(IReadOnlyList<LogRawRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.OtherId.IsInteger && OtherKind(row) == LogOtherIdKind.NpcTemplate)
                    yield return row.OtherId.Value;
            }
        }

        private static IEnumerable<long> MapCandidates(IReadOnlyList<LogRawRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.MapId.IsInteger)
                    yield return row.MapId.Value;
            }
        }

        private static LogOtherIdKind? OtherKind(LogRawRow row)
        {
            if (!row.Type.IsInteger)
                return null;
            return LogEventRegistry.TryGetKnown(row.Type.Value, out LogEventDescriptor descriptor)
                ? descriptor.OtherIdKind
                : null;
        }

        private static LogQueryRow Project(LogRawRow row, LogEntityNames names)
        {
            long typeValue = row.Type.IsInteger ? row.Type.Value : -1;
            LogEventDescriptor descriptor = row.Type.IsInteger
                ? LogEventRegistry.Get(row.Type.Value)
                : LogEventRegistry.Unknown;

            var context = new LogFormatContext(
                row.RowId, row.DateTicks, typeValue,
                row.PlayerId.IsInteger ? row.PlayerId.Value : null,
                row.OtherId.IsInteger ? row.OtherId.Value : null,
                row.MapId.IsInteger ? row.MapId.Value : null,
                row.MapX.IsInteger ? row.MapX.Value : null,
                row.MapY.IsInteger ? row.MapY.Value : null,
                row.Text,
                names.PlayerNames, names.GuildNames, names.NpcTemplateNames, names.MapNames);
            LogFormattedEvent formatted = LogFormatter.Project(context);

            LogRelatedEntity? primary = null;
            if (row.PlayerId.IsInteger && row.PlayerId.Value > 0)
            {
                primary = new LogRelatedEntity("Player", LogEntityKind.Player, row.PlayerId.Value,
                    Lookup(names.PlayerNames, row.PlayerId.Value), InQuickFilterDomain(row.PlayerId.Value));
            }

            LogRelatedEntity? map = null;
            if (row.MapId.IsInteger && row.MapId.Value > 0)
            {
                map = new LogRelatedEntity("Map", LogEntityKind.Map, row.MapId.Value,
                    Lookup(names.MapNames, row.MapId.Value), InQuickFilterDomain(row.MapId.Value));
            }

            return new LogQueryRow(
                row.RowId, row.DateTicks,
                row.Type.Value, row.Type.IsInteger,
                row.PlayerId.Value, row.PlayerId.IsInteger,
                row.OtherId.Value, row.OtherId.IsInteger,
                row.MapId.Value, row.MapId.IsInteger,
                row.MapX.Value, row.MapX.IsInteger,
                row.MapY.Value, row.MapY.IsInteger,
                descriptor.Label, descriptor.Group.ToString(),
                primary, formatted.Related, map, formatted.Summary, row.Text);
        }

        private static bool InQuickFilterDomain(long id) => id is > 0 and <= int.MaxValue;

        private static string? Lookup(IReadOnlyDictionary<int, string> names, long id)
        {
            if (id is > 0 and <= int.MaxValue && names.TryGetValue((int)id, out string? name) && name.Length > 0)
                return name;
            return null;
        }
    }
}
