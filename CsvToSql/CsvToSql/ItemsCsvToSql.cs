using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsvHelper;
using System.IO;

namespace CsvToSql
{
    class ItemsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] { "item_template_id", "item_usetype", "item_name", "item_description", "player_hp", "player_mp",
                "player_sp", "stat_ac", "stat_str", "stat_sta", "stat_dex", "stat_int", "res_fire", "res_water", "res_spirit", "res_air",
                "res_earth", "min_experience", "min_level", "max_experience", "max_level", "weapon_damage", "weapon_delay", "item_slot",
                "item_type", "item_value", "lore", "bindonpickup", "bindonequip", "event", "graphic_tile", "graphic_file", "graphic_equip",
                "graphic_r", "graphic_g", "graphic_b", "graphic_a", "class_restrictions", "stack_size", "body_state", "spell_effect_id",
                "spell_effect_chance", "learn_spell_id", "credits_value",
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "item_description":
                case "item_name":
                    return EscapeString(value);
                case "item_usetype":
                    return ConvertEnum(value, typeof(UseTypes));
                case "item_slot":
                    return ConvertEnum(value, typeof(ItemSlots));
                case "item_type":
                    return ConvertEnum(value, typeof(ItemTypes));
                default:
                    return value;
            }
        }

        public enum UseTypes
        {
            NoUse = 0,
            OneTime,
            Armor,
            Weapon,
            Scroll,
            HairDye,
            Letter,
            Money,
            Recipe,
        }
        public enum ItemSlots
        {
            Helmet = 0,
            Shield,
            OneHanded,
            TwoHanded,
            Ring,
            Necklace,
            Pauldrons,
            Cloak,
            Belt,
            Gloves,
            Chest,
            Pants,
            Shoes,
            Mount,
            Misc = 20,
        }
        public enum ItemTypes
        {
            None = 0,
            Plate = 10,
            Leather,
            Cloth,
            Mail,
            OneHandedSword,
            TwoHandedSword,
            OneHandedBlunt,
            TwoHandedBlunt,
            OneHandedPierce,
            TwoHandedPierce,
            Fist,
        }
    }
}
