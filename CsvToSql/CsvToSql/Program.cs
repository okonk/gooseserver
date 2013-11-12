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
                Console.WriteLine("Not enough arguments. CsvToSql.exe [--spellEffects, --itemTemplates] csvPath");
                return;
            }

            if (args[0] == "--spellEffects")
            {
                new SpellEffectsCsvToSql().Convert(args[1], "spell_effects");
            }
            else if (args[0] == "--itemTemplates")
            {
                new ItemsCsvToSql().Convert(args[1], "item_templates");
            }
            else
            {
                Console.WriteLine("Unknown command: {0}", args[0]);
            }
        }
    }
}
