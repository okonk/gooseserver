using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsvToSql
{
    class SpellEffectsCsvToSql : CsvToSqlBase
    {
        protected override string[] GetColumns()
        {
            return new[] { "spell_effect_id", "spell_effect_name", "spell_animation", "spell_animation_file", "spell_display", "target_type", "target_size", "spell_effected", 
                "min_level_effected", "max_level_effected", "effect_type", "effect_duration", "do_attack_animation", "do_cast_animation", "spell_damage_effects", "spell_energy_type", 
                "hp_change_formula", "mp_change_formula", "sp_change_formula", "hp", "mp", "sp", "stat_ac", "stat_str", "stat_sta", "stat_dex", "stat_int", "res_fire", "res_water", 
                "res_spirit", "res_air", "res_earth", "hp_percent_regen", "hp_static_regen", "mp_percent_regen", "mp_static_regen", "haste", "spell_damage", "spell_crit", "melee_damage", 
                "melee_crit", "damage_reduce", "move_speed", "body_id", "oneffect_text", "offeffect_text", "face_id", "hair_id", "hair_r", "hair_g", "hair_b", "hair_a", "body_r", "body_g", 
                "body_b", "body_a", "teleport_map", "teleport_x", "teleport_y", "taunt_aggro", "works_in_pvp", "works_not_in_pvp", "buff_removable", "buff_graphic", "buff_graphic_file", 
                "buff_doesnt_stack_over", "buff_stacks_over", "random_join_chance", "on_hit_spell_effect_id", "on_hit_spell_chance", "on_attack_spell_effect_id", "on_attack_spell_chance", 
                "snare_percent", "only_hits_one_npc", 
            };
        }

        protected override string TransformValue(string columnName, string value)
        {
            switch (columnName)
            {
                case "spell_effect_name":
                case "buff_doesnt_stack_over":
                case "buff_stacks_over":
                case "oneffect_text":
                case "offeffect_text":
                case "hp_change_formula":
                case "mp_change_formula":
                case "sp_change_formula":
                    return EscapeString(value);
                case "do_attack_animation":
                case "do_cast_animation":
                case "spell_damage_effects":
                case "works_in_pvp":
                case "works_not_in_pvp":
                case "buff_removable":
                case "only_hits_one_npc":
                    // booleans
                    return EscapeString(value);

                case "spell_display":
                    return ConvertEnum(value, typeof(SpellDisplays));
                case "target_type":
                    return ConvertEnum(value, typeof(TargetTypes));
                case "spell_effected":
                    return ConvertEnum(value, typeof(SpellEffected));
                case "effect_type":
                    return ConvertEnum(value, typeof(EffectTypes));
                case "spell_energy_type":
                    return ConvertEnum(value, typeof(EnergyTypes));
                default:
                    return value;
            }
        }

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
            PetNeutral = 20
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
