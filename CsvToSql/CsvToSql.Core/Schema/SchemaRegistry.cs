using System;
using System.Collections.Generic;

namespace CsvToSql.Core.Schema
{
    /// <summary>One worksheet's schema: the sheet name the importer matches on, its target
    /// table, and its descriptors. Single source of truth for both SQL generation and the
    /// editor's generated schema.js.</summary>
    public sealed class TableSchema
    {
        public string Sheet { get; }
        public string Table { get; }

        /// <summary>The importer that reads this sheet. Behaviour, not schema — SchemaGen
        /// does not serialise it.</summary>
        public CsvToSqlBase Converter { get; }

        public IReadOnlyList<Column> Columns { get; }
        public IReadOnlyList<Composite> Composites { get; }

        /// <summary>Columns to declare a single-column secondary index on.</summary>
        public IReadOnlyList<string> Indexes { get; }

        public TableSchema(string sheet, string table, CsvToSqlBase converter,
                           IReadOnlyList<string> indexes = null)
        {
            Sheet = sheet;
            Table = table;
            Converter = converter;
            Columns = converter.GetColumnDescriptors();
            Composites = converter.GetComposites() ?? [];
            Indexes = indexes ?? [];
        }
    }

    public static class SchemaRegistry
    {
        /// <summary>Declaration order is emission order in the generated script. It preserves
        /// the pre-descriptor schema's table order, which is also creation order: dependants
        /// follow the tables they reference.</summary>
        public static IReadOnlyList<TableSchema> Tables { get; } = new[]
        {
            new TableSchema("Items", "item_templates", new ItemsCsvToSql()),
            new TableSchema("NPCs", "npc_templates", new NpcCsvToSql()),
            new TableSchema("NPC Spawns", "npc_spawns", new NpcSpawnsCsvToSql()),
            new TableSchema("NPC Drops", "npc_drops", new NpcDropsCsvToSql()),
            new TableSchema("NPC Vendor Items", "npc_vendor_items", new NpcVendorsCsvToSql(),
                            new[] { "npc_template_id" }),
            new TableSchema("Quests", "quests", new QuestsCsvToSql()),
            new TableSchema("Quest Reqs", "quest_requirements", new QuestRequirementsCsvToSql()),
            new TableSchema("Quest Rewards", "quest_rewards", new QuestRewardsCsvToSql()),
            new TableSchema("Spells", "spells", new SpellsCsvToSql()),
            new TableSchema("Spell Effects", "spell_effects", new SpellEffectsCsvToSql()),
            new TableSchema("Warptiles", "warptiles", new WarpTilesCsvToSql()),
            new TableSchema("Maps", "maps", new MapsCsvToSql()),
            new TableSchema("Map Required Items", "map_required_items", new MapRequiredItemsCsvToSql(),
                            new[] { "map_id" }),
            new TableSchema("Combinations", "combinations", new CombinationsCsvToSql()),
            new TableSchema("Combination Item Required", "combination_item_required",
                            new CombinationItemRequiredCsvToSql()),
            new TableSchema("Combination Item Result", "combination_item_results",
                            new CombinationItemResultsCsvToSql()),
            new TableSchema("Titles", "item_titles", new TitleCsvToSql()),
            new TableSchema("Surnames", "item_surnames", new SurnameCsvToSql()),
            new TableSchema("Classes", "classes", new ClassesCsvToSql()),
            new TableSchema("Class Info", "class_info", new ClassInfoCsvToSql()),
            new TableSchema("Class Levelup Spells", "classes_levelup_spells",
                            new ClassLevelupSpellsCsvToSql()),
        };
    }
}
