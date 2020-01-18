using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Data.SqlClient;
using Goose.Scripting;

namespace Goose
{
    /**
     * Manages Spell/SpellEffect objects
     * 
     */
    public class SpellHandler
    {
        private Dictionary<int, SpellEffect> effects;
        private Dictionary<int, Spell> spells;

        public SpellHandler()
        {
            this.effects = new Dictionary<int, SpellEffect>();
            this.spells = new Dictionary<int, Spell>();
        }

        /**
         * LoadSpellEffects, loads all spell effects
         * 
         */
        public void LoadSpellEffects(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM spell_effects";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int id = Convert.ToInt32(reader["spell_effect_id"]);

                SpellEffect effect = null;
                if (!this.effects.TryGetValue(id, out effect))
                    effect = new SpellEffect();

                effect.ID = id;
                effect.Name = Convert.ToString(reader["spell_effect_name"]);
                effect.Animation = Convert.ToInt32(reader["spell_animation"]);
                effect.AnimationFile = Convert.ToInt32(reader["spell_animation_file"]);
                effect.Display = (SpellEffect.SpellDisplays)Convert.ToInt32(reader["spell_display"]);
                effect.TargetType = (SpellEffect.TargetTypes)Convert.ToInt32(reader["target_type"]);
                effect.TargetSize = Convert.ToInt32(reader["target_size"]);
                effect.Effected = (SpellEffect.SpellEffected)Convert.ToInt32(reader["spell_effected"]);
                effect.MinimumLevelEffected = Convert.ToInt32(reader["min_level_effected"]);
                effect.MaximumLevelEffected = Convert.ToInt32(reader["max_level_effected"]);
                effect.EffectType = (SpellEffect.EffectTypes)Convert.ToInt32(reader["effect_type"]);
                effect.Duration = Convert.ToInt64(reader["effect_duration"]);
                effect.DoAttackAnimation = 
                    ("0".Equals(Convert.ToString(reader["do_attack_animation"])) ? false : true);
                effect.DoCastAnimation =
                    ("0".Equals(Convert.ToString(reader["do_cast_animation"])) ? false : true);
                effect.SpellDamageEffects = 
                    ("0".Equals(Convert.ToString(reader["spell_damage_effects"])) ? false : true);
                effect.EnergyType = Convert.ToInt32(reader["spell_energy_type"]);
                effect.HPFormula = Convert.ToString(reader["hp_change_formula"]);
                effect.MPFormula = Convert.ToString(reader["mp_change_formula"]);
                effect.SPFormula = Convert.ToString(reader["sp_change_formula"]);
                effect.OnEffectText = Convert.ToString(reader["oneffect_text"]);
                effect.OffEffectText = Convert.ToString(reader["offeffect_text"]);
                effect.TauntAggro = Convert.ToInt64(reader["taunt_aggro"]);
                effect.TeleportMapID = Convert.ToInt32(reader["teleport_map"]);
                effect.TeleportMapX = Convert.ToInt32(reader["teleport_x"]);
                effect.TeleportMapY = Convert.ToInt32(reader["teleport_y"]);

                effect.BodyID = Convert.ToInt32(reader["body_id"]);
                effect.BodyR = Convert.ToInt32(reader["body_r"]);
                effect.BodyG = Convert.ToInt32(reader["body_g"]);
                effect.BodyB = Convert.ToInt32(reader["body_b"]);
                effect.BodyA = Convert.ToInt32(reader["body_a"]);
                effect.FaceID = Convert.ToInt32(reader["face_id"]);
                effect.HairID = Convert.ToInt32(reader["hair_id"]);
                effect.HairR = Convert.ToInt32(reader["hair_r"]);
                effect.HairG = Convert.ToInt32(reader["hair_g"]);
                effect.HairB = Convert.ToInt32(reader["hair_b"]);
                effect.HairA = Convert.ToInt32(reader["hair_a"]);

                effect.Stats = new AttributeSet();
                effect.Stats.HP = Convert.ToInt32(reader["hp"]);
                effect.Stats.MP = Convert.ToInt32(reader["mp"]);
                effect.Stats.SP = Convert.ToInt32(reader["sp"]);
                effect.Stats.AC = Convert.ToInt32(reader["stat_ac"]);
                effect.Stats.Strength = Convert.ToInt32(reader["stat_str"]);
                effect.Stats.Stamina = Convert.ToInt32(reader["stat_sta"]);
                effect.Stats.Intelligence = Convert.ToInt32(reader["stat_int"]);
                effect.Stats.Dexterity = Convert.ToInt32(reader["stat_dex"]);
                effect.Stats.FireResist = Convert.ToInt32(reader["res_fire"]);
                effect.Stats.AirResist = Convert.ToInt32(reader["res_air"]);
                effect.Stats.EarthResist = Convert.ToInt32(reader["res_earth"]);
                effect.Stats.SpiritResist = Convert.ToInt32(reader["res_spirit"]);
                effect.Stats.WaterResist = Convert.ToInt32(reader["res_water"]);

                effect.Stats.HPPercentRegen = Decimal.Parse(Convert.ToString(reader["hp_percent_regen"]));
                effect.Stats.HPStaticRegen = Convert.ToInt32(reader["hp_static_regen"]);
                effect.Stats.MPPercentRegen = Decimal.Parse(Convert.ToString(reader["mp_percent_regen"]));
                effect.Stats.MPStaticRegen = Convert.ToInt32(reader["mp_static_regen"]);

                effect.Stats.DamageReduction = Decimal.Parse(Convert.ToString(reader["damage_reduce"]));
                effect.Stats.Haste = Decimal.Parse(Convert.ToString(reader["haste"]));
                effect.Stats.MeleeCrit = Decimal.Parse(Convert.ToString(reader["melee_crit"]));
                effect.Stats.MeleeDamage = Decimal.Parse(Convert.ToString(reader["melee_damage"]));
                effect.Stats.SpellCrit = Decimal.Parse(Convert.ToString(reader["spell_crit"]));
                effect.Stats.SpellDamage = Decimal.Parse(Convert.ToString(reader["spell_damage"]));
                effect.Stats.MoveSpeedIncrease = Decimal.Parse(Convert.ToString(reader["move_speed"]));

                effect.WorksInPVP = ("0".Equals(Convert.ToString(reader["works_in_pvp"])) ? false : true);
                effect.WorksNotInPVP = ("0".Equals(Convert.ToString(reader["works_not_in_pvp"])) ? false : true);

                effect.BuffCanBeRemoved = ("0".Equals(Convert.ToString(reader["buff_removable"])) ? false : true);
                effect.BuffGraphic = Convert.ToInt32(reader["buff_graphic"]);
                effect.BuffGraphicFile = Convert.ToInt32(reader["buff_graphic_file"]);

                effect.RandomJoinChance = Decimal.Parse(Convert.ToString(reader["random_join_chance"]));

                effect.OnMeleeAttackSpellID = Convert.ToInt32(reader["on_attack_spell_effect_id"]);
                effect.OnMeleeAttackSpellChance = 
                    Decimal.Parse(Convert.ToString(reader["on_attack_spell_chance"]));
                effect.OnMeleeHitSpellID = Convert.ToInt32(reader["on_hit_spell_effect_id"]);
                effect.OnMeleeHitSpellChance =
                    Decimal.Parse(Convert.ToString(reader["on_hit_spell_chance"]));

                effect.SnarePercent = Decimal.Parse(Convert.ToString(reader["snare_percent"]));

                effect.BuffStacksOverString = Convert.ToString(reader["buff_stacks_over"]);
                effect.BuffDoesntStackOverString = Convert.ToString(reader["buff_doesnt_stack_over"]);
                effect.BuffStacksOver = new List<SpellEffect>();
                effect.BuffDoesntStackOver = new List<SpellEffect>();

                effect.OnlyHitsOneNPC = ("0".Equals(Convert.ToString(reader["only_hits_one_npc"])) ? false : true);

                string scriptPath = Convert.ToString(reader["script_path"]);
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    effect.Script = world.ScriptHandler.GetScript<ISpellEffectScript>(scriptPath);
                    effect.ScriptParams = Convert.ToString(reader["script_params"]);
                }

                this.effects[effect.ID] = effect;
            }

            reader.Close();

            foreach (SpellEffect s in this.effects.Values)
            {
                s.OnMeleeAttackSpell = this.GetSpellEffect(s.OnMeleeAttackSpellID);
                s.OnMeleeHitSpell = this.GetSpellEffect(s.OnMeleeHitSpellID);

                foreach (string effectid in s.BuffStacksOverString.Split(" ".ToCharArray()))
                {
                    try
                    {
                        SpellEffect e = this.GetSpellEffect(Convert.ToInt32(effectid));
                        if (e == null)
                        {
                            // log bad spell effect id
                        }
                        else
                        {
                            s.BuffStacksOver.Add(e);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                foreach (string effectid in s.BuffDoesntStackOverString.Split(" ".ToCharArray()))
                {
                    try
                    {
                        SpellEffect e = this.GetSpellEffect(Convert.ToInt32(effectid));
                        if (e == null)
                        {
                            // log bad spell effect id
                        }
                        else
                        {
                            s.BuffDoesntStackOver.Add(e);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        /**
         * EffectCount, returns number of effects
         */
        public int EffectCount { get { return this.effects.Count; } }

        /**
         * GetSpellEffect, returns spell effect
         * 
         */
        public SpellEffect GetSpellEffect(int id)
        {
            SpellEffect effect = null;
            this.effects.TryGetValue(id, out effect);
            return effect;
        }

        /**
         * LoadSpells, loads all spells
         * 
         */
        public void LoadSpells(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM spells";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int id = Convert.ToInt32(reader["spell_id"]);

                Spell spell = null;
                if (!this.spells.TryGetValue(id, out spell))
                    spell = new Spell();

                spell.ID = id;
                spell.Name = Convert.ToString(reader["spell_name"]);
                spell.Description = Convert.ToString(reader["spell_description"]);
                spell.Target = (Spell.SpellTargets)Convert.ToInt32(reader["spell_target"]);
                spell.ClassRestrictions = Convert.ToInt64(reader["class_restrictions"]);
                spell.Aether = Convert.ToInt64(reader["spell_aether"]);
                spell.Graphic = Convert.ToInt32(reader["spellbook_graphic"]);
                spell.GraphicFile = Convert.ToInt32(reader["spellbook_graphic_file"]);
                spell.HPPercentCost = Decimal.Parse(Convert.ToString(reader["hp_percent_cost"]));
                spell.HPStaticCost = Convert.ToInt32(reader["hp_static_cost"]);
                spell.MPPercentCost = Decimal.Parse(Convert.ToString(reader["mp_percent_cost"]));
                spell.MPStaticCost = Convert.ToInt32(reader["mp_static_cost"]);
                spell.SPPercentCost = Decimal.Parse(Convert.ToString(reader["sp_percent_cost"]));
                spell.SPStaticCost = Convert.ToInt32(reader["sp_static_cost"]);

                spell.SpellEffectID = Convert.ToInt32(reader["spell_effect_id"]);
                spell.SpellEffect = this.GetSpellEffect(spell.SpellEffectID);

                if (spell.SpellEffect == null)
                {
                    // log bad spell effect
                    continue;
                }

                this.spells[spell.ID] = spell;
            }

            reader.Close();
        }

        /**
         * Count, returns number of spells
         */
        public int Count { get { return this.spells.Count; } }

        /**
         * GetSpell, returns spell
         * 
         */
        public Spell GetSpell(int id)
        {
            Spell spell = null;
            this.spells.TryGetValue(id, out spell);
            return spell;
        }

        public Spell GetSpellByName(string name)
        {
            return this.spells.Values.Where(s => s.Name == name).FirstOrDefault();
        }
    }
}
