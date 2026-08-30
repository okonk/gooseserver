using System.Reflection;

namespace Goose.Commands
{
    public static class HelpFormatter
    {
        public const int MaxLineLength = 42;
        public const int MaxLinesPerPage = 19;

        public static List<string> Wrap(string line)
        {
            var lines = new List<string>();
            var current = "";

            foreach (var word in line.Split(' '))
            {
                if (word.Length > MaxLineLength)
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current);
                        current = "";
                    }
                    for (var i = 0; i < word.Length; i += MaxLineLength)
                        lines.Add(word.Substring(i, Math.Min(MaxLineLength, word.Length - i)));
                    continue;
                }

                var candidate = current.Length == 0 ? word : current + " " + word;
                if (candidate.Length <= MaxLineLength)
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }

            if (current.Length > 0 || lines.Count == 0)
                lines.Add(current);
            return lines;
        }

        public static List<List<string>>? BuildPages(Player player, CommandRegistry registry, string? name)
        {
            var pages = new List<List<string>>();

            if (name is null)
            {
                var listLines = new List<string>();
                foreach (var section in registry.Sections)
                {
                    var visible = section.Commands.Count(def => CommandRegistry.IsUsableBy(player, def));
                    if (visible == 0) continue;
                    listLines.Add($"{section.Name} ({visible})");
                }
                if (listLines.Count > 0)
                    pages.AddRange(SplitPages(listLines));
                foreach (var section in registry.Sections)
                {
                    var lines = SectionLines(player, section);
                    if (lines.Count > 0)
                        pages.AddRange(SplitPages(lines));
                }
                return pages.Count > 0 ? pages : null;
            }

            var input = name.TrimEnd(' ');

            var command = FindCommand(registry, input);
            if (command is not null && CommandRegistry.IsUsableBy(player, command))
                pages.AddRange(SplitPages(CommandLines(player, command)));

            var namedSection = registry.Sections
                .FirstOrDefault(s => string.Equals(s.Name, input, StringComparison.OrdinalIgnoreCase));
            if (namedSection is not null)
            {
                var lines = SectionLines(player, namedSection);
                if (lines.Count > 0)
                    pages.AddRange(SplitPages(lines));
            }

            return pages.Count > 0 ? pages : null;
        }

        private static CommandDefinition? FindCommand(CommandRegistry registry, string name)
        {
            foreach (var def in registry.Snapshot.Ordered)
            {
                if (def.Section is null) continue;
                if (string.Equals(def.PrimaryKey.Trim().TrimStart('/'), name, StringComparison.OrdinalIgnoreCase))
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
                parameters = def.Handler.Method.GetParameters();
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
            var wrapped = Wrap(text);
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
