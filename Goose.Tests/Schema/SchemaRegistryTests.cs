using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema
{
    public class SchemaRegistryTests
    {
        /// <summary>The 21 sheet -> table pairs the pre-registry
        /// CsvToSqlConverter.BuildConverterMapping() carried, captured as literals so the
        /// registry is pinned against what actually worked rather than against the plan.
        /// Kept deliberately past the migration: it is the only remaining record of which
        /// worksheet feeds which table, and nothing else pins it — the equivalence test sees
        /// table names but never sheet names, so a mis-pairing would silently import the wrong
        /// sheet. Update it only alongside a genuine, intended sheet or table rename.</summary>
        private static readonly string[] LegacyPairs =
        {
            "Items -> item_templates",
            "NPC Drops -> npc_drops",
            "NPC Spawns -> npc_spawns",
            "NPC Vendor Items -> npc_vendor_items",
            "NPCs -> npc_templates",
            "Spell Effects -> spell_effects",
            "Spells -> spells",
            "Warptiles -> warptiles",
            "Quests -> quests",
            "Quest Reqs -> quest_requirements",
            "Quest Rewards -> quest_rewards",
            "Maps -> maps",
            "Map Required Items -> map_required_items",
            "Combinations -> combinations",
            "Combination Item Required -> combination_item_required",
            "Combination Item Result -> combination_item_results",
            "Titles -> item_titles",
            "Surnames -> item_surnames",
            "Classes -> classes",
            "Class Info -> class_info",
            "Class Levelup Spells -> classes_levelup_spells",
        };

        [Fact]
        public void Has_twenty_one_tables()
        {
            Assert.Equal(21, SchemaRegistry.Tables.Count);
        }

        [Fact]
        public void Every_table_is_fully_populated()
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
            // Compare the PAIRS, not sheets and tables independently — sorting the two lists
            // separately would let a mis-pairing (Titles -> item_surnames) pass unnoticed.
            Assert.Equal(
                LegacyPairs.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                SchemaRegistry.Tables.Select(t => $"{t.Sheet} -> {t.Table}")
                    .OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
    }
}
