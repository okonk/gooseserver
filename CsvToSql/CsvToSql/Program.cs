using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CsvToSql
{
    class Program
    {
        static void Main(string[] args)
        {
            //if (args.Length < 2)
            //{
            //    Console.WriteLine("Not enough arguments. CsvToSql.exe [--spellEffects, --itemTemplates, --npcTemplates] csvPath");
            //    return;
            //}

            //switch (args[0])
            //{
            //    case "--spellEffects":
            //        new SpellEffectsCsvToSql().Convert(args[1], "spell_effects");
            //        break;
            //    case "--itemTemplates":
            //        new ItemsCsvToSql().Convert(args[1], "item_templates");
            //        break;
            //    case "--npcTemplates":
            //        new NpcCsvToSql().Convert(args[1], "npc_templates");
            //        break;
            //    default:
            //        Console.WriteLine("Unknown command: {0}", args[0]);
            //        break;
            //}

            foreach (var file in Directory.EnumerateFiles(".", "*.csv"))
            {
                switch (Path.GetFileName(file))
                {
                    case "illutia - Items.csv":
                        new ItemsCsvToSql().Convert(file, "item_templates");
                        break;
                    case "illutia - NPC Drops.csv":
                        new NpcDropsCsvToSql().Convert(file, "npc_drops");
                        break;
                    case "illutia - NPC Spawns.csv":
                        new NpcSpawnsCsvToSql().Convert(file, "npc_spawns");
                        break;
                    case "illutia - NPC Vendor Items.csv":
                        new NpcVendorsCsvToSql().Convert(file, "npc_vendor_items");
                        break;
                    case "illutia - NPCs.csv":
                        new NpcCsvToSql().Convert(file, "npc_templates");
                        break;
                    case "illutia - Spell Effects.csv":
                        new SpellEffectsCsvToSql().Convert(file, "spell_effects");
                        break;
                    case "illutia - Spells.csv":
                        new SpellsCsvToSql().Convert(file, "spells");
                        break;
                    case "illutia - Warptiles.csv":
                        new WarpTilesCsvToSql().Convert(file, "warptiles");
                        break;
                    case "illutia - Quests.csv":
                        new QuestsCsvToSql().Convert(file, "quests");
                        break;
                    case "illutia - Quest Reqs.csv":
                        new QuestRequirementsCsvToSql().Convert(file, "quest_requirements");
                        break;
                    case "illutia - Quest Rewards.csv":
                        new QuestRewardsCsvToSql().Convert(file, "quest_rewards");
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
