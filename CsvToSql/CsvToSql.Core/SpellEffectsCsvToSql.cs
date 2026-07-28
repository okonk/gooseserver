using CsvToSql.Core.Schema;

namespace CsvToSql
{
    public class SpellEffectsCsvToSql : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("spell_effect_id", SqlType.Integer).PrimaryKey(),
            Col.Text("spell_effect_name"),
            Col.Int("spell_animation", SqlType.Int, def: 0),
            Col.Int("spell_animation_file", SqlType.Int, def: 0),
            Col.Enum<SpellDisplays>("spell_display", SqlType.Int, def: 0),
            Col.Enum<TargetTypes>("target_type", SqlType.Int, def: 0),
            Col.Int("target_size", SqlType.Int, def: 0),

            Col.Enum<SpellEffected>("spell_effected", SqlType.Int),
            Col.Int("min_level_effected", SqlType.Int, def: 1),
            Col.Int("max_level_effected", SqlType.Int, def: 50),

            Col.Enum<EffectTypes>("effect_type", SqlType.Int),
            Col.Int("effect_duration", SqlType.BigInt, def: 0),

            Col.Bool("do_attack_animation", def: false),
            Col.Bool("do_cast_animation", def: true),
            Col.Bool("spell_damage_effects", def: false), // does spell damage/crit effect this spell
            Col.Enum<EnergyTypes>("spell_energy_type", SqlType.Int, def: 0), // bitfield fire, water, spirit, air, earth, none?

            // for damage/heal kinda spells
            // change_formulas are what to do to the effected person's stat, for damage/heals
            Col.Text("hp_change_formula", def: "'0'"),
            Col.Text("mp_change_formula", def: "'0'"),
            Col.Text("sp_change_formula", def: "'0'"),

            // Stuff for buffs/permanent
            Col.Int("hp", SqlType.Int, def: 0),
            Col.Int("mp", SqlType.Int, def: 0),
            Col.Int("sp", SqlType.Int, def: 0),
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
            Col.Decimal("hp_percent_regen", SqlType.Decimal94, def: "0"),
            Col.Int("hp_static_regen", SqlType.Int, def: 0),
            Col.Decimal("mp_percent_regen", SqlType.Decimal94, def: "0"),
            Col.Int("mp_static_regen", SqlType.Int, def: 0),
            Col.Decimal("haste", SqlType.Decimal94, def: "0"),
            Col.Decimal("spell_damage", SqlType.Decimal94, def: "0"),
            Col.Decimal("spell_crit", SqlType.Decimal94, def: "0"),
            Col.Decimal("melee_damage", SqlType.Decimal94, def: "0"),
            Col.Decimal("melee_crit", SqlType.Decimal94, def: "0"),
            Col.Decimal("damage_reduce", SqlType.Decimal94, def: "0"),
            Col.Int("move_speed", SqlType.SmallInt, def: 0),
            Col.Int("body_id", SqlType.SmallInt, def: 0),

            Col.Text("oneffect_text", def: "''"),
            Col.Text("offeffect_text", def: "''"),

            // For permanent
            Col.Int("face_id", SqlType.SmallInt, def: 0),
            Col.Int("hair_id", SqlType.SmallInt, def: 0),
            Col.Int("hair_r", SqlType.SmallInt, def: 0),
            Col.Int("hair_g", SqlType.SmallInt, def: 0),
            Col.Int("hair_b", SqlType.SmallInt, def: 0),
            Col.Int("hair_a", SqlType.SmallInt, def: 0),
            Col.Int("body_r", SqlType.SmallInt, def: 0),
            Col.Int("body_g", SqlType.SmallInt, def: 0),
            Col.Int("body_b", SqlType.SmallInt, def: 0),
            Col.Int("body_a", SqlType.SmallInt, def: 0),

            // Stuff for teleport
            Col.Int("teleport_map", SqlType.Int, def: 1),
            Col.Int("teleport_x", SqlType.Int, def: 50),
            Col.Int("teleport_y", SqlType.Int, def: 50),

            // Aggro for taunt
            Col.Int("taunt_aggro", SqlType.Int, def: 0),

            Col.Bool("works_in_pvp", def: true),
            Col.Bool("works_not_in_pvp", def: false),

            Col.Bool("buff_removable", def: true),
            Col.Int("buff_graphic", SqlType.Int, def: 0),
            Col.Int("buff_graphic_file", SqlType.Int, def: 0),
            Col.Text("buff_doesnt_stack_over", def: "''"),
            Col.Text("buff_stacks_over", def: "''"),

            Col.Decimal("random_join_chance", SqlType.Decimal52, def: "0"),

            Col.Int("on_hit_spell_effect_id", SqlType.Int, def: 0),
            Col.Decimal("on_hit_spell_chance", SqlType.Decimal52, def: "100"),
            Col.Int("on_attack_spell_effect_id", SqlType.Int, def: 0),
            Col.Decimal("on_attack_spell_chance", SqlType.Decimal52, def: "100"),

            Col.Decimal("snare_percent", SqlType.Decimal52, def: "0"),

            Col.Bool("only_hits_one_npc", def: false),

            Col.Text("script_path", def: "''"),
            Col.Text("script_params", def: "''"),
        };

        public override Composite[] GetComposites() => new[]
        {
            Composite.Graphic(tile: "spell_animation", file: "spell_animation_file"),
            Composite.Rgba(r: "hair_r", g: "hair_g", b: "hair_b", a: "hair_a"),
            Composite.Rgba(r: "body_r", g: "body_g", b: "body_b", a: "body_a"),
            Composite.Graphic(tile: "buff_graphic", file: "buff_graphic_file"),
        };

        public enum SpellDisplays
        {
            Character = 0,
            Tile
        }

        /**
         * Target types, what squares to hit
         */
        public enum TargetTypes
        {
            Target = 0,
            LineFront,
            Cross,
            Plus,
            Random,
            Area,
            TriangleFront
        }

        /**
         * Who is effected by the spell, bitfield
         */
        public enum SpellEffected
        {
            Self = 1,
            NPC = 2,
            Player = 4,

            SelfNPC = 3,
            SelfNPCPlayer = 7,
            SelfPlayer = 5,

            NPCPlayer = 6
        }

        /**
         * Spell Types
         * 
         * Formula = only use formulas
         * Buff, temporarily increase stats/change body until effect wears off
         * Permanent, permanently change stats/body
         * Tick, do formula on every tick until effect wears off
         * TickBuff, buff but with animation every tick
         * Taunt, uses formula but does taunt damage also
         * Viral, uses tick but also if it effects anyone it infects them too
         * Tame, used for pet taming spells
         * 
         */
        public enum EffectTypes
        {
            Formula = 0,
            Buff,
            Permanent,
            Tick,
            TickBuff,
            Teleport = 5,
            Bind,
            Stun,
            Root,
            Snare,
            Viral = 10,
            Invisible,
            SeeInvisible,
            OnAttack,
            OnMeleeHit,
            PetTame = 15,
            PetAttack,
            PetDefend,
            PetDestroy,
            PetFollow,
            PetNeutral = 20,
            Script,
        }

        /**
         * Spell Energy Type
         * Possibly values for bitmask
         * 
         */
        public enum EnergyTypes
        {
            None = 1,
            Fire = 2,
            Water = 4,
            Spirit = 8,
            Air = 16,
            Earth = 32
        }
    }
}
