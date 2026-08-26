using System.Text;
using System.Collections;
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
            this.effects = [];
            this.spells = [];
        }

        /**
         * LoadSpellEffects, loads all spell effects
         *
         */
        public void LoadSpellEffects(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT * FROM spell_effects";
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32("spell_effect_id");

                SpellEffect? effect = null;
                if (!this.effects.TryGetValue(id, out effect))
                    effect = new SpellEffect();

                effect.ID = id;
                effect.Name = reader.GetString("spell_effect_name");
                effect.Animation = reader.GetInt32("spell_animation");
                effect.AnimationFile = reader.GetInt32("spell_animation_file");
                effect.Display = (SpellEffect.SpellDisplays)reader.GetInt32("spell_display");
                effect.TargetType = (SpellEffect.TargetTypes)reader.GetInt32("target_type");
                effect.TargetSize = reader.GetInt32("target_size");
                effect.Effected = (SpellEffect.SpellEffected)reader.GetInt32("spell_effected");
                effect.MinimumLevelEffected = reader.GetInt32("min_level_effected");
                effect.MaximumLevelEffected = reader.GetInt32("max_level_effected");
                effect.EffectType = (SpellEffect.EffectTypes)reader.GetInt32("effect_type");
                effect.Duration = reader.GetInt64("effect_duration");
                effect.DoAttackAnimation =
                    reader.GetString("do_attack_animation") != "0";
                effect.DoCastAnimation =
                    reader.GetString("do_cast_animation") != "0";
                effect.SpellDamageEffects =
                    reader.GetString("spell_damage_effects") != "0";
                effect.EnergyType = reader.GetInt32("spell_energy_type");
                effect.HPFormula = reader.GetString("hp_change_formula");
                effect.MPFormula = reader.GetString("mp_change_formula");
                effect.SPFormula = reader.GetString("sp_change_formula");
                effect.OnEffectText = reader.GetString("oneffect_text");
                effect.OffEffectText = reader.GetString("offeffect_text");
                effect.TauntAggro = reader.GetInt64("taunt_aggro");
                effect.TeleportMapID = reader.GetInt32("teleport_map");
                effect.TeleportMapX = reader.GetInt32("teleport_x");
                effect.TeleportMapY = reader.GetInt32("teleport_y");

                effect.BodyID = reader.GetInt32("body_id");
                effect.BodyR = reader.GetInt32("body_r");
                effect.BodyG = reader.GetInt32("body_g");
                effect.BodyB = reader.GetInt32("body_b");
                effect.BodyA = reader.GetInt32("body_a");
                effect.FaceID = reader.GetInt32("face_id");
                effect.HairID = reader.GetInt32("hair_id");
                effect.HairR = reader.GetInt32("hair_r");
                effect.HairG = reader.GetInt32("hair_g");
                effect.HairB = reader.GetInt32("hair_b");
                effect.HairA = reader.GetInt32("hair_a");

                effect.Stats = new AttributeSet();
                effect.Stats.HP = reader.GetInt64("hp");
                effect.Stats.MP = reader.GetInt64("mp");
                effect.Stats.SP = reader.GetInt64("sp");
                effect.Stats.AC = reader.GetInt32("stat_ac");
                effect.Stats.Strength = reader.GetInt32("stat_str");
                effect.Stats.Stamina = reader.GetInt32("stat_sta");
                effect.Stats.Intelligence = reader.GetInt32("stat_int");
                effect.Stats.Dexterity = reader.GetInt32("stat_dex");
                effect.Stats.FireResist = reader.GetInt32("res_fire");
                effect.Stats.AirResist = reader.GetInt32("res_air");
                effect.Stats.EarthResist = reader.GetInt32("res_earth");
                effect.Stats.SpiritResist = reader.GetInt32("res_spirit");
                effect.Stats.WaterResist = reader.GetInt32("res_water");

                effect.Stats.HPPercentRegen = Decimal.Parse(reader.GetString("hp_percent_regen"));
                effect.Stats.HPStaticRegen = reader.GetInt32("hp_static_regen");
                effect.Stats.MPPercentRegen = Decimal.Parse(reader.GetString("mp_percent_regen"));
                effect.Stats.MPStaticRegen = reader.GetInt32("mp_static_regen");

                effect.Stats.DamageReduction = Decimal.Parse(reader.GetString("damage_reduce"));
                effect.Stats.Haste = Decimal.Parse(reader.GetString("haste"));
                effect.Stats.MeleeCrit = Decimal.Parse(reader.GetString("melee_crit"));
                effect.Stats.MeleeDamage = Decimal.Parse(reader.GetString("melee_damage"));
                effect.Stats.SpellCrit = Decimal.Parse(reader.GetString("spell_crit"));
                effect.Stats.SpellDamage = Decimal.Parse(reader.GetString("spell_damage"));
                effect.Stats.MoveSpeed = reader.GetInt32("move_speed");

                effect.WorksInPVP = reader.GetString("works_in_pvp") != "0";
                effect.WorksNotInPVP = reader.GetString("works_not_in_pvp") != "0";

                effect.BuffCanBeRemoved = reader.GetString("buff_removable") != "0";
                effect.BuffGraphic = reader.GetInt32("buff_graphic");
                effect.BuffGraphicFile = reader.GetInt32("buff_graphic_file");

                effect.RandomJoinChance = Decimal.Parse(reader.GetString("random_join_chance"));

                effect.OnMeleeAttackSpellID = reader.GetInt32("on_attack_spell_effect_id");
                effect.OnMeleeAttackSpellChance =
                    Decimal.Parse(reader.GetString("on_attack_spell_chance"));
                effect.OnMeleeHitSpellID = reader.GetInt32("on_hit_spell_effect_id");
                effect.OnMeleeHitSpellChance =
                    Decimal.Parse(reader.GetString("on_hit_spell_chance"));

                effect.SnarePercent = Decimal.Parse(reader.GetString("snare_percent"));

                effect.BuffStacksOverString = reader.GetString("buff_stacks_over");
                effect.BuffDoesntStackOverString = reader.GetString("buff_doesnt_stack_over");
                effect.BuffStacksOver = [];
                effect.BuffDoesntStackOver = [];

                effect.OnlyHitsOneNPC = reader.GetString("only_hits_one_npc") != "0";

                string scriptPath = reader.GetString("script_path");
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    effect.Script = world.ScriptHandler.GetScript<ISpellEffectScript>(scriptPath);
                    effect.ScriptParams = reader.GetString("script_params");
                }

                this.effects[effect.ID] = effect;
            }

            foreach (var s in this.effects.Values)
            {
                s.OnMeleeAttackSpell = this.GetSpellEffect(s.OnMeleeAttackSpellID);
                s.OnMeleeHitSpell = this.GetSpellEffect(s.OnMeleeHitSpellID);

                foreach (string effectid in s.BuffStacksOverString.Split(' '))
                {
                    try
                    {
                        SpellEffect? e = this.GetSpellEffect(Convert.ToInt32(effectid));
                        if (e is null)
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
                foreach (string effectid in s.BuffDoesntStackOverString.Split(' '))
                {
                    try
                    {
                        SpellEffect? e = this.GetSpellEffect(Convert.ToInt32(effectid));
                        if (e is null)
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
            });
        }

        /**
         * EffectCount, returns number of effects
         */
        public int EffectCount { get => this.effects.Count; }

        /**
         * GetSpellEffect, returns spell effect
         *
         */
        public SpellEffect? GetSpellEffect(int id)
        {
            SpellEffect? effect = null;
            this.effects.TryGetValue(id, out effect);
            return effect;
        }

        /// <summary>Registers a script-generated effect. Overwrites any existing entry with the
        /// same id - callers that must not collide should check GetSpellEffect first.</summary>
        public void AddSpellEffect(SpellEffect effect)
        {
            this.effects[effect.ID] = effect;
        }

        /// <summary>Every loaded effect, for scripts that need to enumerate rather than look up.</summary>
        public IEnumerable<SpellEffect> GetSpellEffects()
        {
            return this.effects.Values;
        }

        /**
         * LoadSpells, loads all spells
         *
         */
        public void LoadSpells(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT * FROM spells";
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32("spell_id");

                Spell? spell = null;
                if (!this.spells.TryGetValue(id, out spell))
                    spell = new Spell();

                spell.ID = id;
                spell.Name = reader.GetString("spell_name");
                spell.Description = reader.GetString("spell_description");
                spell.Target = (Spell.SpellTargets)reader.GetInt32("spell_target");
                spell.ClassRestrictions = reader.GetInt64("class_restrictions");
                spell.Aether = reader.GetInt64("spell_aether");
                spell.Graphic = reader.GetInt32("spellbook_graphic");
                spell.GraphicFile = reader.GetInt32("spellbook_graphic_file");
                spell.HPPercentCost = Decimal.Parse(reader.GetString("hp_percent_cost"));
                spell.HPStaticCost = reader.GetInt32("hp_static_cost");
                spell.MPPercentCost = Decimal.Parse(reader.GetString("mp_percent_cost"));
                spell.MPStaticCost = reader.GetInt32("mp_static_cost");
                spell.SPPercentCost = Decimal.Parse(reader.GetString("sp_percent_cost"));
                spell.SPStaticCost = reader.GetInt32("sp_static_cost");

                spell.SpellEffectID = reader.GetInt32("spell_effect_id");
                spell.SpellEffect = this.GetSpellEffect(spell.SpellEffectID)!;

                if (spell.SpellEffect is null)
                {
                    // log bad spell effect
                    continue;
                }

                this.spells[spell.ID] = spell;
            }
            });
        }

        /**
         * Count, returns number of spells
         */
        public int Count { get => this.spells.Count; }

        /**
         * GetSpell, returns spell
         *
         */
        public Spell? GetSpell(int id)
        {
            Spell? spell = null;
            this.spells.TryGetValue(id, out spell);
            return spell;
        }

        /// <summary>Registers a script-generated spell. Overwrites any existing entry with the same id.</summary>
        public void AddSpell(Spell spell)
        {
            this.spells[spell.ID] = spell;
        }

        public IEnumerable<Spell> GetSpells()
        {
            return this.spells.Values;
        }

        public Spell? GetSpellByName(string name)
        {
            return this.spells.Values.Where(s => s.Name == name).FirstOrDefault();
        }
    }
}
