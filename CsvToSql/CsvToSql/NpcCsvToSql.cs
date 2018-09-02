using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using System.IO;

namespace CsvToSql
{
    class NpcCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] {"npc_id", "npc_type", "npc_name", "npc_title", "npc_surname", "respawn_time", "npc_facing", "npc_level", "experience", "aggro_range",
                "attack_range", "attack_speed", "move_speed", "stationary", "stunnable", "rootable", "slowable", "invincible", "npc_hp", "npc_mp", "npc_sp",
                "class_id", "stat_ac", "stat_str", "stat_sta", "stat_dex", "stat_int", "res_fire", "res_water", "res_spirit", "res_air", "res_earth", "body_state",
                "body_id", "body_r", "body_g", "body_b", "body_a", "face_id", "hair_id", "hair_r", "hair_g", "hair_b", "hair_a", "equipped_items", "weapon_damage",
                "hp_percent_regen", "hp_static_regen", "mp_percent_regen", "mp_static_regen", "npc_alliance", "stuck_behaviour", "stuck_timeout", "credit_dealer",
                "quest_ids", "script_path"
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "npc_name":
                case "npc_title":
                case "npc_surname":
                case "quest_ids":
                case "equipped_items":
                case "npc_alliance":
                case "script_path":
                    return EscapeString(value);
                case "stationary":
                case "stunnable":
                case "rootable":
                case "slowable":
                case "invincible":
                case "credit_dealer":
                    // booleans
                    return EscapeString(value);
                case "npc_type":
                    return ConvertEnum(value, typeof(Types));
                case "stuck_behaviour":
                    return ConvertEnum(value, typeof(BehaviourTypes));
                default:
                    return value;
            }
        }

        public enum Types
        {
            Monster = 2,
            Vendor = 10,
            Banker = 11,
            Quest = 12
        }

        public enum BehaviourTypes
        {
            DoNothing = 0,
            TeleportToAggro,
            TeleportAggro,
        }
    }
}
