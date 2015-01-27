using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvToSql
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough arguments. CsvToSql.exe [--spellEffects, --itemTemplates, --npcTemplates] csvPath");
                return;
            }

            switch (args[0])
            {
                case "--spellEffects":
                    new SpellEffectsCsvToSql().Convert(args[1], "spell_effects");
                    break;
                case "--itemTemplates":
                    new ItemsCsvToSql().Convert(args[1], "item_templates");
                    break;
                case "--npcTemplates":
                    new NpcCsvToSql().Convert(args[1], "npc_templates");
                    break;
                default:
                    Console.WriteLine("Unknown command: {0}", args[0]);
                    break;
            }
        }
    }
}
