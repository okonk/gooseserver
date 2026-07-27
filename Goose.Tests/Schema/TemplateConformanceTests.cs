using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CsvToSql;
using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema
{
    /// <summary>
    /// Safety net for the descriptor migration: every migrated converter's
    /// <see cref="Column"/> list is checked mechanically against the CREATE TABLE block in
    /// sqlTemplate.sql, because SqlType/Default are transcribed by hand and no other test
    /// observes them (DDL is still template-driven, and the baseline test only sees names,
    /// order and kind).
    ///
    /// THIS WHOLE FILE IS TEMPORARY. It is deleted in Task 8, when DDL becomes
    /// descriptor-generated and sqlTemplate.sql goes away. The converter-type -> table-name
    /// map below duplicates the private mapping in CsvToSqlConverter.BuildConverterMapping()
    /// on purpose: that mapping becomes a typed SchemaRegistry in Task 7, and until then
    /// duplicating it here is cheaper than opening it up.
    /// </summary>
    public class TemplateConformanceTests
    {
        private static readonly Dictionary<string, string> TableNames = new()
        {
            [nameof(ItemsCsvToSql)] = "item_templates",
            [nameof(NpcDropsCsvToSql)] = "npc_drops",
            [nameof(NpcSpawnsCsvToSql)] = "npc_spawns",
            [nameof(NpcVendorsCsvToSql)] = "npc_vendor_items",
            [nameof(NpcCsvToSql)] = "npc_templates",
            [nameof(SpellEffectsCsvToSql)] = "spell_effects",
            [nameof(SpellsCsvToSql)] = "spells",
            [nameof(WarpTilesCsvToSql)] = "warptiles",
            [nameof(QuestsCsvToSql)] = "quests",
            [nameof(QuestRequirementsCsvToSql)] = "quest_requirements",
            [nameof(QuestRewardsCsvToSql)] = "quest_rewards",
            [nameof(MapsCsvToSql)] = "maps",
            [nameof(MapRequiredItemsCsvToSql)] = "map_required_items",
            [nameof(CombinationsCsvToSql)] = "combinations",
            [nameof(CombinationItemRequiredCsvToSql)] = "combination_item_required",
            [nameof(CombinationItemResultsCsvToSql)] = "combination_item_results",
            [nameof(TitleCsvToSql)] = "item_titles",
            [nameof(SurnameCsvToSql)] = "item_surnames",
            [nameof(ClassesCsvToSql)] = "classes",
            [nameof(ClassInfoCsvToSql)] = "class_info",
            [nameof(ClassLevelupSpellsCsvToSql)] = "classes_levelup_spells",
        };

        public static TheoryData<string> MigratedConverters()
        {
            var data = new TheoryData<string>();
            foreach (var type in MigratedConverterTypes())
                data.Add(type.Name);
            return data;
        }

        [Theory]
        [MemberData(nameof(MigratedConverters))]
        public void Descriptors_match_sql_template(string converterTypeName)
        {
            var type = MigratedConverterTypes().Single(t => t.Name == converterTypeName);
            var converter = (CsvToSqlBase)Activator.CreateInstance(type)!;
            var descriptors = converter.GetColumnDescriptors();

            Assert.True(
                TableNames.TryGetValue(converterTypeName, out var table),
                $"{converterTypeName} has migrated to GetColumnDescriptors() but has no entry in " +
                $"{nameof(TemplateConformanceTests)}.{nameof(TableNames)}. Add its target table name " +
                "there so its descriptors are checked against sqlTemplate.sql.");

            var template = ParseCreateTable(table!);

            Assert.True(
                descriptors.Length == template.Count,
                $"[{table}] descriptor count {descriptors.Length} != template column count {template.Count}. " +
                $"Descriptors: {string.Join(", ", descriptors.Select(d => d.Name))}. " +
                $"Template: {string.Join(", ", template.Select(c => c.Name))}.");

            for (var i = 0; i < descriptors.Length; i++)
            {
                var d = descriptors[i];
                var t = template[i];

                Assert.True(d.Name == t.Name,
                    $"[{table}] column {i}: descriptor name '{d.Name}' != template name '{t.Name}'.");

                Assert.True(d.Type.Sql == t.SqlType,
                    $"[{table}].{t.Name}: descriptor type '{d.Type.Sql}' != template type '{t.SqlType}'.");

                Assert.True(d.Default == t.Default,
                    $"[{table}].{t.Name}: descriptor default {Show(d.Default)} != template default {Show(t.Default)}.");

                Assert.True(d.IsNullable == t.IsNullable,
                    $"[{table}].{t.Name}: descriptor IsNullable={d.IsNullable} but template " +
                    $"{(t.IsNullable ? "does not declare" : "declares")} NOT NULL. " +
                    "Add or remove .Nullable() on the descriptor to match.");

                Assert.True(d.IsPrimaryKey == t.IsPrimaryKey,
                    $"[{table}].{t.Name}: descriptor IsPrimaryKey={d.IsPrimaryKey} but template " +
                    $"{(t.IsPrimaryKey ? "declares" : "does not declare")} PRIMARY KEY.");
            }
        }

        [Fact]
        public void At_least_one_converter_is_migrated()
        {
            Assert.NotEmpty(MigratedConverterTypes());
        }

        private static string Show(string? value) => value is null ? "<none>" : $"'{value}'";

        private static List<Type> MigratedConverterTypes() =>
            typeof(CsvToSqlBase).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(CsvToSqlBase).IsAssignableFrom(t))
                .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
                .Where(t => ((CsvToSqlBase)Activator.CreateInstance(t)!).GetColumnDescriptors() != null)
                .OrderBy(t => t.Name)
                .ToList();

        private sealed record TemplateColumn(
            string Name, string SqlType, string? Default, bool IsPrimaryKey, bool IsNullable);

        private static string? _template;

        private static string Template()
        {
            if (_template != null) return _template;
            var assembly = typeof(CsvToSqlBase).Assembly;
            using var stream = assembly.GetManifestResourceStream("CsvToSql.Core.sqlTemplate.sql")
                ?? throw new InvalidOperationException("Embedded resource CsvToSql.Core.sqlTemplate.sql not found.");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return _template = reader.ReadToEnd();
        }

        private static List<TemplateColumn> ParseCreateTable(string table)
        {
            // Strip /* ... */ comments; the template uses them both inline after a column and
            // on lines of their own.
            var sql = Regex.Replace(Template(), @"/\*.*?\*/", "", RegexOptions.Singleline);

            var match = Regex.Match(
                sql,
                @"CREATE\s+TABLE\s+" + Regex.Escape(table) + @"\s*\((?<body>.*?)\r?\n\s*\);",
                RegexOptions.Singleline);
            Assert.True(match.Success, $"No CREATE TABLE block for '{table}' found in sqlTemplate.sql.");

            var columns = new List<TemplateColumn>();
            // Indentation is two spaces for most tables and tabs for the combination* tables;
            // blank lines are used as visual grouping inside several blocks.
            foreach (var raw in match.Groups["body"].Value.Split('\n'))
            {
                var line = raw.Trim().TrimEnd(',').Trim();
                if (line.Length == 0) continue;

                // A type may embed a comma inside parens (DECIMAL(9,4), VARCHAR(64)), so it is
                // still a single whitespace-delimited token.
                var parts = Regex.Match(line, @"^(?<name>\S+)\s+(?<type>\S+)(?<mods>.*)$");
                Assert.True(parts.Success, $"[{table}] could not parse column line: '{line}'.");

                var mods = parts.Groups["mods"].Value;
                var isPk = Regex.IsMatch(mods, @"\bPRIMARY\s+KEY\b", RegexOptions.IgnoreCase);

                // A default is either a single-quoted literal (which may contain commas and
                // spaces, e.g. '0,*,0,*,0,*') or a bare token (0, 100, 'Scripts/...').
                var def = Regex.Match(mods, @"\bDEFAULT\s+(?<value>'(?:[^']|'')*'|\S+)", RegexOptions.IgnoreCase);

                // A PRIMARY KEY column carries no NOT NULL in the template but is not nullable
                // in the descriptor sense either — PrimaryKey() handles it.
                var isNullable = !isPk && !Regex.IsMatch(mods, @"\bNOT\s+NULL\b", RegexOptions.IgnoreCase);

                columns.Add(new TemplateColumn(
                    parts.Groups["name"].Value,
                    parts.Groups["type"].Value,
                    def.Success ? def.Groups["value"].Value : null,
                    isPk,
                    isNullable));
            }

            Assert.True(columns.Count > 0, $"[{table}] CREATE TABLE block parsed to zero columns.");
            return columns;
        }
    }
}
