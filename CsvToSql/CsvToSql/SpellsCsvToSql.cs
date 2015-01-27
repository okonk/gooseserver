using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using System.IO;

namespace CsvToSql
{
    class SpellsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {
                "spell_id", "spell_name", "spell_description", "spell_target", "class_restrictions", "spell_aether", "spellbook_graphic", "spellbook_graphic_file", "hp_static_cost", "hp_percent_cost",
                "mp_static_cost", "mp_percent_cost", "sp_static_cost", "sp_percent_cost", "spell_effect_id"
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "spell_name":
                case "spell_description":
                    return EscapeString(value);
                case "spell_target":
                    return ConvertEnum(value, typeof(SpellTargets));
                default:
                    return value;
            }
        }

        public enum SpellTargets
        {
            Target = 0,
            Self,
            Group
        }
    }
}
