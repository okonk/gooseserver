using System.Reflection;

namespace Goose.Commands
{
    internal static class HelpFormatter
    {
        public const int MaxLineLength = 42;
        public const int MaxLinesPerPage = 19;

        public static List<string> Wrap(string line)
            => Wrap(line, MaxLineLength, MaxLineLength);

        private static List<string> Wrap(string line, int firstBudget, int continuationBudget)
        {
            var lines = new List<string>();
            var current = "";
            var budget = firstBudget;

            foreach (var word in line.Split(' '))
            {
                while (true)
                {
                    if (word.Length > budget)
                    {
                        if (current.Length > 0)
                        {
                            lines.Add(current);
                            current = "";
                            budget = continuationBudget;
                        }
                        var remaining = word;
                        var chunkBudget = budget;
                        while (remaining.Length > 0)
                        {
                            var len = Math.Min(chunkBudget, remaining.Length);
                            lines.Add(remaining[..len]);
                            remaining = remaining[len..];
                            chunkBudget = continuationBudget;
                        }
                        budget = continuationBudget;
                        break;
                    }

                    var candidate = current.Length == 0 ? word : current + " " + word;
                    if (candidate.Length <= budget)
                    {
                        current = candidate;
                        break;
                    }

                    lines.Add(current);
                    current = "";
                    budget = continuationBudget;
                }
            }

            if (current.Length > 0 || lines.Count == 0)
                lines.Add(current);
            return lines;
        }

        public static List<List<string>>? BuildPages(Player player, CommandRegistry registry, string? name)
        {
            var snapshot = registry.Snapshot;
            var sections = registry.SectionsOf(snapshot);
            var pages = new List<List<string>>();

            if (name is null)
            {
                var listLines = new List<string>();
                foreach (var section in sections)
                {
                    var visible = section.Commands.Count(def => CommandRegistry.IsUsableBy(player, def));
                    if (visible == 0) continue;
                    listLines.AddRange(Wrap($"{section.Name} ({visible})"));
                }
                if (listLines.Count > 0)
                    pages.AddRange(SplitPages(listLines));
                foreach (var section in sections)
                {
                    var lines = SectionLines(player, section);
                    if (lines.Count > 0)
                        pages.AddRange(SplitPages(lines));
                }
                return pages.Count > 0 ? pages : null;
            }

            var input = NormalizeKey(name);

            var command = FindCommand(player, snapshot, input);
            if (command is not null)
                pages.AddRange(SplitPages(CommandLines(player, command)));

            var namedSection = sections
                .FirstOrDefault(s => string.Equals(s.Name, input, StringComparison.OrdinalIgnoreCase));
            if (namedSection is not null)
            {
                var lines = SectionLines(player, namedSection);
                if (lines.Count > 0)
                    pages.AddRange(SplitPages(lines));
            }

            return pages.Count > 0 ? pages : null;
        }

        private static string NormalizeKey(string key)
        {
            var trimmed = key.TrimEnd(' ');
            return trimmed.Length > 0 && trimmed[0] == '/' ? trimmed[1..] : trimmed;
        }

        private static CommandDefinition? FindCommand(Player player, CommandSnapshot snapshot, string name)
        {
            foreach (var def in snapshot.Ordered)
            {
                if (def.Section is null) continue;
                if (!CommandRegistry.IsUsableBy(player, def)) continue;
                if (def.Keys.Any(key => string.Equals(NormalizeKey(key), name, StringComparison.OrdinalIgnoreCase)))
                    return def;
            }
            return null;
        }

        private static List<string> SectionLines(Player player, CommandSection section)
        {
            var lines = new List<string>();
            foreach (var def in section.Commands)
            {
                if (!CommandRegistry.IsUsableBy(player, def)) continue;
                AddWrapped(lines, UsageText(def) + " - " + def.Help);
            }
            return lines;
        }

        private static List<string> CommandLines(Player player, CommandDefinition def)
        {
            var lines = new List<string>();
            AddWrapped(lines, def.Help);
            AddWrapped(lines, UsageText(def));
            foreach (var sub in def.Subcommands)
            {
                if (sub.Privilege is not null && !player.HasPrivilege(sub.Privilege.Value)) continue;
                AddWrapped(lines, CommandBinder.Usage(SubcommandKey(def, sub.PrimaryName), sub.Parameters, sub.UsageOverride)
                    + " - " + sub.Help);
            }
            return lines;
        }

        private static string SubcommandKey(CommandDefinition def, string primaryName)
            => $"{def.PrimaryKey.TrimEnd()} {primaryName}";

        private static string UsageText(CommandDefinition def)
        {
            var key = def.PrimaryKey.TrimEnd();
            ParameterInfo[] parameters;
            string? usageOverride;
            if (def.ExecuteMethod is not null)
            {
                parameters = def.ExecuteMethod.GetParameters();
                usageOverride = def.UsageOverride;
            }
            else if (def.Handler is not null)
            {
                parameters = CommandBinder.UsageParameters(def.Handler);
                usageOverride = null;
            }
            else
            {
                parameters = [];
                usageOverride = null;
            }
            return CommandBinder.Usage(key, parameters, usageOverride);
        }

        private static void AddWrapped(List<string> lines, string text)
        {
            var wrapped = Wrap(text, MaxLineLength, MaxLineLength - 2);
            for (var i = 0; i < wrapped.Count; i++)
                lines.Add(i == 0 ? wrapped[i] : "  " + wrapped[i]);
        }

        private static List<List<string>> SplitPages(List<string> lines)
        {
            var pages = new List<List<string>>();
            for (var i = 0; i < lines.Count; i += MaxLinesPerPage)
                pages.Add(lines.GetRange(i, Math.Min(MaxLinesPerPage, lines.Count - i)));
            return pages;
        }
    }
}
