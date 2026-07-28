using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema
{
    public class SchemaRegistryTests
    {
        /// <summary>The 21 worksheet names the pre-registry CsvToSqlConverter.BuildConverterMapping()
        /// keyed on. Captured as a literal so the registry is pinned against what actually worked.</summary>
        private static readonly string[] LegacySheets =
        {
            "Items", "NPC Drops", "NPC Spawns", "NPC Vendor Items", "NPCs", "Spell Effects",
            "Spells", "Warptiles", "Quests", "Quest Reqs", "Quest Rewards", "Maps",
            "Map Required Items", "Combinations", "Combination Item Required",
            "Combination Item Result", "Titles", "Surnames", "Classes", "Class Info",
            "Class Levelup Spells",
        };

        /// <summary>The 21 target table names from the same mapping.</summary>
        private static readonly string[] LegacyTables =
        {
            "item_templates", "npc_drops", "npc_spawns", "npc_vendor_items", "npc_templates",
            "spell_effects", "spells", "warptiles", "quests", "quest_requirements",
            "quest_rewards", "maps", "map_required_items", "combinations",
            "combination_item_required", "combination_item_results", "item_titles",
            "item_surnames", "classes", "class_info", "classes_levelup_spells",
        };

        [Fact]
        public void Covers_all_twenty_one_sheets()
        {
            Assert.Equal(21, SchemaRegistry.Tables.Count);
        }

        [Fact]
        public void Every_table_has_descriptors()
        {
            foreach (var t in SchemaRegistry.Tables)
            {
                Assert.False(string.IsNullOrEmpty(t.Sheet));
                Assert.False(string.IsNullOrEmpty(t.Table));
                Assert.NotEmpty(t.Columns);
            }
        }

        [Fact]
        public void Maps_items_sheet_to_item_templates()
        {
            var items = SchemaRegistry.Tables.Single(t => t.Sheet == "Items");

            Assert.Equal("item_templates", items.Table);
            Assert.Equal("item_template_id", items.Columns[0].Name);
            Assert.True(items.Columns[0].IsPrimaryKey);
        }

        [Fact]
        public void Only_two_tables_declare_indexes()
        {
            var indexed = SchemaRegistry.Tables
                .Where(t => t.Indexes is { Count: > 0 })
                .Select(t => t.Table)
                .OrderBy(x => x)
                .ToArray();

            Assert.Equal(new[] { "map_required_items", "npc_vendor_items" }, indexed);
        }

        [Fact]
        public void Every_foreign_key_targets_a_known_sheet()
        {
            var sheets = SchemaRegistry.Tables.Select(t => t.Sheet).ToHashSet();

            foreach (var t in SchemaRegistry.Tables)
                foreach (var c in t.Columns.Where(c => c.RefSheet != null))
                    Assert.Contains(c.RefSheet, sheets);
        }

        /// <summary>A Composite names its columns by string, so a typo is silently inert.</summary>
        [Fact]
        public void Every_composite_references_a_real_column()
        {
            foreach (var t in SchemaRegistry.Tables)
            {
                var names = t.Columns.Select(c => c.Name).ToHashSet();

                foreach (var composite in t.Composites)
                    foreach (var name in composite.Columns)
                        Assert.True(names.Contains(name),
                            $"[{t.Table}] {composite.Kind} composite references column '{name}', " +
                            $"which is not a descriptor of that table. " +
                            $"Columns: {string.Join(", ", names)}.");
            }
        }

        [Fact]
        public void Covers_exactly_the_sheets_and_tables_of_the_legacy_mapping()
        {
            Assert.Equal(
                LegacySheets.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                SchemaRegistry.Tables.Select(t => t.Sheet).OrderBy(x => x, StringComparer.Ordinal).ToArray());

            Assert.Equal(
                LegacyTables.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                SchemaRegistry.Tables.Select(t => t.Table).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
    }
}
