using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class ItemsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("item_template_id", SqlType.Integer).PrimaryKey(),
            Col.Enum<UseTypes>("item_usetype", SqlType.SmallInt),
            Col.Text("item_name"),
            Col.Text("item_description", def: "''"),

            // Stats granted while equipped
            Col.Int("player_hp", SqlType.Int, def: 0),
            Col.Int("player_mp", SqlType.Int, def: 0),
            Col.Int("player_sp", SqlType.Int, def: 0),
            Col.Int("stat_ac", SqlType.SmallInt, def: 0),
            Col.Int("stat_str", SqlType.SmallInt, def: 0),
            Col.Int("stat_sta", SqlType.SmallInt, def: 0),
            Col.Int("stat_dex", SqlType.SmallInt, def: 0),
            Col.Int("stat_int", SqlType.SmallInt, def: 0),
            Col.Int("res_fire", SqlType.SmallInt, def: 0),
            Col.Int("res_water", SqlType.SmallInt, def: 0),
            Col.Int("res_spirit", SqlType.SmallInt, def: 0),
            Col.Int("res_air", SqlType.SmallInt, def: 0),
            Col.Int("res_earth", SqlType.SmallInt, def: 0),

            // Requirements to equip
            Col.Int("min_experience", SqlType.BigInt, def: 0),
            Col.Int("min_level", SqlType.SmallInt, def: 0),
            Col.Int("max_experience", SqlType.BigInt, def: 0),
            Col.Int("max_level", SqlType.SmallInt, def: 0),

            // Weapon
            Col.Int("weapon_damage", SqlType.Int, def: 0),
            Col.Int("weapon_delay", SqlType.SmallInt, def: 10),

            Col.Enum<ItemSlots>("item_slot", SqlType.SmallInt, def: 20),
            Col.Enum<ItemTypes>("item_type", SqlType.SmallInt, def: 0),
            Col.Int("item_value", SqlType.BigInt, def: 0),

            // Flags
            Col.Bool("lore", def: false),
            Col.Bool("bindonpickup", def: false),
            Col.Bool("bindonequip", def: false),
            Col.Bool("event", def: false),

            // Graphics
            Col.Int("graphic_tile", SqlType.Int),
            Col.Int("graphic_file", SqlType.Int, def: 0),
            Col.Int("graphic_equip", SqlType.SmallInt, def: 0),
            Col.Int("graphic_r", SqlType.SmallInt, def: 0),
            Col.Int("graphic_g", SqlType.SmallInt, def: 0),
            Col.Int("graphic_b", SqlType.SmallInt, def: 0),
            Col.Int("graphic_a", SqlType.SmallInt, def: 0),

            Col.Int("class_restrictions", SqlType.BigInt, def: 0),
            Col.Int("stack_size", SqlType.SmallInt, def: 1),
            Col.Int("body_state", SqlType.SmallInt, def: 3),

            // 0 is the "none" sentinel for these two spell ids, so they are not
            // declared as foreign keys.
            Col.Int("spell_effect_id", SqlType.Int, def: 0),
            Col.Decimal("spell_effect_chance", SqlType.Decimal94, def: "100"),
            Col.Int("learn_spell_id", SqlType.Int, def: 0),

            Col.Int("credits_value", SqlType.Int, def: 0),
            Col.Text("script_path", def: "''"),
            Col.Text("script_params", def: "''"),
        };

        public override Composite[] GetComposites() => new[]
        {
            Composite.Graphic(tile: "graphic_tile", file: "graphic_file"),
            Composite.Rgba(r: "graphic_r", g: "graphic_g", b: "graphic_b", a: "graphic_a"),
            Composite.Bitmask("class_restrictions", from: "Classes"),
        };

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
