using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsvToSql
{
    class ClassInfoCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] { "class_id", "level", "level_up_exp", "player_hp", "player_mp", "player_sp", "stat_ac", "stat_str", "stat_sta", "stat_dex", "stat_int", 
                "res_fire", "res_water", "res_spirit", "res_air", "res_earth", "hp_percent_regen", "hp_static_regen", "mp_percent_regen", "mp_static_regen", 
                "haste", "spell_damage", "spell_crit", "melee_damage", "melee_crit", "damage_reduce" 
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                default:
                    return value;
            }
        }
    }
}
