using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace CsvToSql.Core
{
    public class CsvToSqlConverter
    {
        public static string Convert(string dataLink)
        {
            var converterMapping = new Dictionary<string, dynamic>()
            {
                { "Items", new { Converter = new ItemsCsvToSql(), Table = "item_templates" } },
                { "NPC Drops", new { Converter = new NpcDropsCsvToSql(), Table = "npc_drops" } },
                { "NPC Spawns", new { Converter = new NpcSpawnsCsvToSql(), Table = "npc_spawns" } },
                { "NPC Vendor Items", new { Converter = new NpcVendorsCsvToSql(), Table = "npc_vendor_items" } },
                { "NPCs", new { Converter = new NpcCsvToSql(), Table = "npc_templates" } },
                { "Spell Effects", new { Converter = new SpellEffectsCsvToSql(), Table = "spell_effects" } },
                { "Spells", new { Converter = new SpellsCsvToSql(), Table = "spells" } },
                { "Warptiles", new { Converter = new WarpTilesCsvToSql(), Table = "warptiles" } },
                { "Quests", new { Converter = new QuestsCsvToSql(), Table = "quests" } },
                { "Quest Reqs", new { Converter = new QuestRequirementsCsvToSql(), Table = "quest_requirements" } },
                { "Quest Rewards", new { Converter = new QuestRewardsCsvToSql(), Table = "quest_rewards" } },
                { "Maps", new { Converter = new MapsCsvToSql(), Table = "maps" } },
                { "Map Required Items", new { Converter = new MapRequiredItemsCsvToSql(), Table = "map_required_items" } },
                { "Combinations", new { Converter = new CombinationsCsvToSql(), Table = "combinations" } },
                { "Combination Item Required", new { Converter = new CombinationItemRequiredCsvToSql(), Table = "combination_item_required" } },
                { "Combination Item Result", new { Converter = new CombinationItemResultsCsvToSql(), Table = "combination_item_results" } },
            };

            var spreadsheet = new MemoryStream(new HttpClient().GetByteArrayAsync(dataLink).Result);

            string sqlTemplate = Properties.Resources.sqlTemplate;
            using (var workbook = new XLWorkbook(spreadsheet, XLEventTracking.Disabled))
            {
                foreach (var worksheet in workbook.Worksheets)
                {
                    dynamic dyn = null;
                    if (converterMapping.TryGetValue(worksheet.Name, out dyn))
                    {
                        sqlTemplate = dyn.Converter.Convert(worksheet, sqlTemplate, dyn.Table);
                    }
                }
            }

            return sqlTemplate;
        }
    }
}
