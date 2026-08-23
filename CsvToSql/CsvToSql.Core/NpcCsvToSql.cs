using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class NpcCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("npc_id", SqlType.Integer).PrimaryKey(),
            Col.Enum<Types>("npc_type", SqlType.SmallInt, def: 2),
            Col.Text("npc_name"),
            Col.Text("npc_title", def: "''"),
            Col.Text("npc_surname", def: "''"),
            Col.Int("respawn_time", SqlType.Int, def: 0),
            Col.Int("npc_facing", SqlType.SmallInt, def: 3),
            Col.Int("npc_level", SqlType.SmallInt, def: 1),
            Col.Int("experience", SqlType.BigInt, def: 0),
            Col.Int("aggro_range", SqlType.SmallInt, def: 0),
            Col.Int("attack_range", SqlType.SmallInt, def: 0),
            Col.Decimal("attack_speed", SqlType.Decimal94, def: "2"),
            Col.Decimal("move_speed", SqlType.Decimal94, def: "2"),
            Col.Bool("stationary", def: false),
            Col.Bool("stunnable", def: false),
            Col.Bool("rootable", def: false),
            Col.Bool("slowable", def: false),
            Col.Bool("invincible", def: false),
            Col.Bool("see_invisible", def: false),

            // Stats
            Col.Int("npc_hp", SqlType.Int, def: 0),
            Col.Int("npc_mp", SqlType.Int, def: 0),
            Col.Int("npc_sp", SqlType.Int, def: 0),
            Col.Id("class_id", SqlType.SmallInt, def: 1).Ref("Classes"),
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

            // Appearance
            // 3 is the UNARMED resting pose, and it is the default a designer wants: an NPC with no
            // weapon drawn is the common case, and body_state's other values only ever pick a weapon
            // clip (AnimationNames.AttackVariant). Items already declares 3; these two are the only
            // sheets that carry the column, and them disagreeing about what a blank cell means was
            // the surprise. NOTE THAT THIS MOVES THE DDL DEFAULT: an npc_templates row with a blank
            // body_state cell imports as unarmed from here on, where it used to import as 1.
            Col.Int("body_state", SqlType.SmallInt, def: 3),
            Col.Int("body_id", SqlType.SmallInt, def: 1),
            Col.Int("body_r", SqlType.SmallInt, def: 0),
            Col.Int("body_g", SqlType.SmallInt, def: 0),
            Col.Int("body_b", SqlType.SmallInt, def: 0),
            Col.Int("body_a", SqlType.SmallInt, def: 0),
            Col.Int("face_id", SqlType.SmallInt, def: 0),
            Col.Int("hair_id", SqlType.SmallInt, def: 0),
            Col.Int("hair_r", SqlType.SmallInt, def: 0),
            Col.Int("hair_g", SqlType.SmallInt, def: 0),
            Col.Int("hair_b", SqlType.SmallInt, def: 0),
            Col.Int("hair_a", SqlType.SmallInt, def: 0),
            Col.Text("equipped_items", def: "'0,*,0,*,0,*,0,*,0,*,0,*'"),

            // Combat
            Col.Int("weapon_damage", SqlType.Int, def: 1),
            // Worksheet order. The pre-descriptor schema listed armor_pierce last, after
            // script_params —
            // do not "fix" this to match it: cells are read positionally, so moving it would
            // write every later column's value into the wrong column.
            Col.Int("armor_pierce", SqlType.Int, def: 0),

            // Regeneration
            Col.Decimal("hp_percent_regen", SqlType.Decimal94, def: "0"),
            Col.Int("hp_static_regen", SqlType.Int, def: 0),
            Col.Decimal("mp_percent_regen", SqlType.Decimal94, def: "0"),
            Col.Int("mp_static_regen", SqlType.Int, def: 0),

            // Behaviour
            Col.Text("npc_alliance", def: "''"),
            Col.Enum<BehaviourTypes>("stuck_behaviour", SqlType.SmallInt, def: 0),
            Col.Int("stuck_timeout", SqlType.Int, def: 20), // Time since last attack to do behaviour in seconds

            // Vendor, quests and scripting
            Col.Bool("credit_dealer", def: false),
            Col.Text("quest_ids", def: "''"),
            Col.Text("script_path", def: "'Scripts/NPC/BaseNPC.csx'"),
            Col.Text("script_params", def: "''"),
        };

        public override Composite[] GetComposites() => new[]
        {
            Composite.Rgba(r: "body_r", g: "body_g", b: "body_b", a: "body_a"),
            Composite.Rgba(r: "hair_r", g: "hair_g", b: "hair_b", a: "hair_a"),
            Composite.EquipSlots("equipped_items"),
            Composite.IdList("quest_ids", refSheet: "Quests"),
        };

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
