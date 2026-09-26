using System.Collections.Immutable;
using System.Data;
using System.Data.SQLite;

namespace Goose.Logs
{
    public sealed class LogEntityNames
    {
        public IReadOnlyDictionary<int, string> PlayerNames { get; }
        public IReadOnlyDictionary<int, string> GuildNames { get; }
        public IReadOnlyDictionary<int, string> NpcTemplateNames { get; }
        public IReadOnlyDictionary<int, string> MapNames { get; }

        internal LogEntityNames(IReadOnlyDictionary<int, string> playerNames,
            IReadOnlyDictionary<int, string> guildNames,
            IReadOnlyDictionary<int, string> npcTemplateNames,
            IReadOnlyDictionary<int, string> mapNames)
        {
            PlayerNames = playerNames;
            GuildNames = guildNames;
            NpcTemplateNames = npcTemplateNames;
            MapNames = mapNames;
        }
    }

    public static class LogEntityResolver
    {
        private const int MaxCandidates = 51;

        public static LogEntityNames Load(SQLiteConnection connection, IEnumerable<long> playerIds,
            IEnumerable<long> guildIds, IEnumerable<long> npcTemplateIds, IEnumerable<long> mapIds)
        {
            return new LogEntityNames(
                LoadNames(connection, "players", "player_id", "player_name", playerIds),
                LoadNames(connection, "guilds", "guild_id", "guild_name", guildIds),
                LoadNames(connection, "npc_templates", "npc_id", "npc_name", npcTemplateIds),
                LoadNames(connection, "maps", "map_id", "map_name", mapIds));
        }

        private static ImmutableDictionary<int, string> LoadNames(SQLiteConnection connection, string table,
            string idColumn, string nameColumn, IEnumerable<long> ids)
        {
            List<int> candidates = ids
                .Where(id => id is > 0 and <= int.MaxValue)
                .Distinct()
                .Select(id => (int)id)
                .ToList();
            if (candidates.Count == 0)
                return ImmutableDictionary<int, string>.Empty;

            var found = new Dictionary<int, string>();
            for (int start = 0; start < candidates.Count; start += MaxCandidates)
            {
                int count = Math.Min(MaxCandidates, candidates.Count - start);
                var command = connection.CreateCommand();
                try
                {
                    var placeholders = new string[count];
                    for (int i = 0; i < count; i++)
                    {
                        string name = "@id" + i;
                        placeholders[i] = name;
                        command.Parameters.Add(new SQLiteParameter(name, DbType.Int32) { Value = candidates[start + i] });
                    }
                    command.CommandText = "SELECT " + idColumn + ", " + nameColumn + " FROM " + table
                        + " WHERE " + idColumn + " IN (" + string.Join(", ", placeholders) + ");";
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                        found[reader.GetInt32(0)] = reader.GetString(1);
                }
                finally
                {
                    command.Dispose();
                }
            }
            return found.ToImmutableDictionary();
        }
    }
}
