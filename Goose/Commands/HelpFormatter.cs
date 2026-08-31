using System.Reflection;

namespace Goose.Commands
{
    internal static class HelpFormatter
    {
        public const int MaxLineLength = 53;
        public const int MaxLinesPerPage = 20;

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
                var indexBlocks = new List<List<string>>();
                foreach (var section in sections)
                {
                    var visible = section.Commands.Count(def => CommandRegistry.IsUsableBy(player, def));
                    if (visible == 0) continue;
                    indexBlocks.Add(Wrap($"{section.Name} ({visible})"));
                }
                if (indexBlocks.Count > 0)
                    pages.AddRange(SplitPages(indexBlocks));

                var sectionBlocks = SectionBlocks(player, sections);
                if (sectionBlocks.Count > 0)
                    pages.AddRange(SplitSectionPages(sectionBlocks));

                return pages.Count > 0 ? pages : null;
            }

            var input = NormalizeKey(name);

            var namedSection = sections
                .FirstOrDefault(s => string.Equals(s.Name, input, StringComparison.OrdinalIgnoreCase));
            if (namedSection is not null)
            {
                var sectionBlocks = SectionBlocks(player, [namedSection]);
                if (sectionBlocks.Count > 0)
                    return SplitSectionPages(sectionBlocks);
            }

            var command = FindCommand(player, snapshot, input);
            if (command is not null)
                return SplitPages(CommandBlocks(player, command));

            return null;
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

        // One entry per visible section: [header, command, command, ...] sub-blocks.
        private static List<List<List<string>>> SectionBlocks(Player player, IEnumerable<CommandSection> sections)
        {
            var result = new List<List<List<string>>>();
            foreach (var section in sections)
            {
                var header = new List<string>();
                header.AddRange(Wrap(section.Name));
                header.Add("");
                var blocks = new List<List<string>> { header };
                foreach (var def in section.Commands)
                {
                    if (!CommandRegistry.IsUsableBy(player, def)) continue;
                    var block = new List<string>(IndentedWrap(UsageLine(UsageText(def)) + " - " + def.Help));
                    foreach (var sub in def.Subcommands)
                    {
                        if (sub.Privilege is not null && !player.HasPrivilege(sub.Privilege.Value)) continue;
                        block.AddRange(IndentedWrap(SubcommandLine(def, sub)));
                    }
                    blocks.Add(block);
                }
                if (blocks.Count > 1)
                    result.Add(blocks);
            }
            return result;
        }

        private static List<List<string>> CommandBlocks(Player player, CommandDefinition def)
        {
            var blocks = new List<List<string>>();
            blocks.Add(IndentedWrap(def.Help));
            blocks.Add(IndentedWrap(UsageLine(UsageText(def))));
            foreach (var sub in def.Subcommands)
            {
                if (sub.Privilege is not null && !player.HasPrivilege(sub.Privilege.Value)) continue;
                blocks.Add(IndentedWrap(SubcommandLine(def, sub)));
            }
            return blocks;
        }

        private static string SubcommandLine(CommandDefinition def, SubcommandInfo sub)
            => UsageLine(CommandBinder.Usage(SubcommandKey(def, sub.PrimaryName), sub.Parameters, sub.UsageOverride))
                + " - " + sub.Help;

        private static string UsageLine(string usage)
            => usage.StartsWith(CommandBinder.UsagePrefix, StringComparison.Ordinal)
                ? usage[CommandBinder.UsagePrefix.Length..]
                : usage;

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

        private static List<string> IndentedWrap(string text)
        {
            var wrapped = Wrap(text, MaxLineLength, MaxLineLength - 2);
            for (var i = 1; i < wrapped.Count; i++)
                wrapped[i] = "  " + wrapped[i];
            return wrapped;
        }

        // Blocks are kept together on one page; only a block taller than a page is hard-split.
        private static List<List<string>> SplitPages(IEnumerable<List<string>> blocks)
        {
            var pages = new List<List<string>>();
            var page = new List<string>();
            foreach (var block in blocks)
            {
                if (page.Count > 0 && page.Count + block.Count > MaxLinesPerPage)
                {
                    pages.Add(page);
                    page = new List<string>();
                }
                AddBlock(ref page, pages, block);
            }
            if (page.Count > 0)
                pages.Add(page);
            return pages;
        }

        // Sections flow continuously and stay on one page when they fit the remaining room;
        // a section taller than a page falls back to keeping its commands together.
        private static List<List<string>> SplitSectionPages(List<List<List<string>>> sections)
        {
            var pages = new List<List<string>>();
            var page = new List<string>();
            foreach (var section in sections)
            {
                var gap = page.Count > 0 ? 1 : 0;
                var total = section.Sum(block => block.Count);
                if (page.Count + gap + total <= MaxLinesPerPage)
                {
                    if (gap > 0)
                        page.Add("");
                    foreach (var block in section)
                        page.AddRange(block);
                    continue;
                }
                if (page.Count > 0)
                {
                    pages.Add(page);
                    page = new List<string>();
                }
                foreach (var block in section)
                {
                    if (page.Count > 0 && page.Count + block.Count > MaxLinesPerPage)
                    {
                        pages.Add(page);
                        page = new List<string>();
                    }
                    AddBlock(ref page, pages, block);
                }
            }
            if (page.Count > 0)
                pages.Add(page);
            return pages;
        }

        private static void AddBlock(ref List<string> page, List<List<string>> pages, List<string> block)
        {
            foreach (var line in block)
            {
                if (page.Count >= MaxLinesPerPage)
                {
                    pages.Add(page);
                    page = new List<string>();
                }
                page.Add(line);
            }
        }
    }
}
