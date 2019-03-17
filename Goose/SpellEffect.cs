using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;

namespace Goose
{
    /**
     * SpellEffect, holds information for spell effects
     * 
     */
    public class SpellEffect
    {
        /**
         * Character only shows the animation if there is a character there
         * Tile shows animation all the time.
         * 
         */
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

        public int ID { get; set; }
        public string Name { get; set; }
        public int Animation { get; set; }
        public int AnimationFile { get; set; }
        public SpellDisplays Display { get; set; }
        public TargetTypes TargetType { get; set; }
        public int TargetSize { get; set; }
        public SpellEffected Effected { get; set; }
        public int MinimumLevelEffected { get; set; }
        public int MaximumLevelEffected { get; set; }
        public EffectTypes EffectType { get; set; }
        public long Duration { get; set; }
        public bool DoAttackAnimation { get; set; }
        public bool DoCastAnimation { get; set; }
        public bool SpellDamageEffects { get; set; }
        public long EnergyType { get; set; }

        public string HPFormula { get; set; }
        public string MPFormula { get; set; }
        public string SPFormula { get; set; }

        public string OnEffectText { get; set; }
        public string OffEffectText { get; set; }

        public long TauntAggro { get; set; }

        public int TeleportMapID { get; set; }
        public int TeleportMapX { get; set; }
        public int TeleportMapY { get; set; }

        public bool WorksInPVP { get; set; }
        public bool WorksNotInPVP { get; set; }

        public bool OnlyHitsOneNPC { get; set; }

        /**
         * If spell is a buff, can it be removed from buff bar by clicking it
         * 
         */
        public bool BuffCanBeRemoved { get; set; }

        public int BuffGraphic { get; set; }
        public int BuffGraphicFile { get; set; }

        public decimal RandomJoinChance { get; set; }

        public int OnMeleeAttackSpellID { get; set; }
        public int OnMeleeHitSpellID { get; set; }
        public SpellEffect OnMeleeAttackSpell { get; set; }
        public SpellEffect OnMeleeHitSpell { get; set; }
        public decimal OnMeleeAttackSpellChance { get; set; }
        public decimal OnMeleeHitSpellChance { get; set; }

        public decimal SnarePercent { get; set; }

        public string BuffStacksOverString { get; set; }
        public string BuffDoesntStackOverString { get; set; }
        public List<SpellEffect> BuffStacksOver { get; set; }
        public List<SpellEffect> BuffDoesntStackOver { get; set; }

        /**
         * Stats to change
         * 
         */
        public AttributeSet Stats { get; set; }
        /**
         * Hair style id
         */
        public int HairID { get; set; }
        /**
         * Hair colour r
         */
        public int HairR { get; set; }
        /**
         * Hair colour g
         */
        public int HairG { get; set; }
        /**
         * Hair colour b
         */
        public int HairB { get; set; }
        /**
         * Hair colour a
         */
        public int HairA { get; set; }
        /**
         * Body colour r
         */
        public int BodyR { get; set; }
        /**
         * Body colour g
         */
        public int BodyG { get; set; }
        /**
         * Body colour b
         */
        public int BodyB { get; set; }
        /**
         * Body colour a
         */
        public int BodyA { get; set; }
        /**
         * Face id
         */
        public int FaceID { get; set; }
        /**
         * Body ID
         */
        public int BodyID { get; set; }

        // Used in random spell
        struct Point
        {
            public int x;
            public int y;

            public Point(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public string SPPString(int targetId)
        {
            return "SPP" +
                targetId + "," +
                this.Animation + "," +
                this.AnimationFile + "," +
                "0," + // todo: what are these 0s? Sound maybe?
                "0";
        }

        public string SPAString(int x, int y)
        {
            return "SPA" +
                x + "," +
                y + "," +
                this.Animation + "," +
                this.AnimationFile + "," +
                "0," +
                "0";
        }

        private string GetPercentageDescription(string label, decimal value, string prefix)
        {
            if (value < 0)
                return string.Format("{0}Decrease {1} by {2:F0}%", prefix, label, Math.Abs(value) * 100);
            else
                return string.Format("{0}Increase {1} by {2:F0}%", prefix, label, value * 100);
        }

        private string GetValueDescription(string label, long value, string prefix)
        {
            if (value < 0)
                return string.Format("{0}Decrease {1} by {2:N0}", prefix, label, Math.Abs(value));
            else
                return string.Format("{0}Increase {1} by {2:N0}", prefix, label, value);
        }

        private string ConvertFormulaVariable(string variable)
        {
            switch (variable)
            {
                case "%cchp":
                    return "Current HP";
                case "%ccmp":
                    return "Current MP";
                case "%tchp":
                    return "Target's Current HP";
                case "%tcmp":
                    return "Target's Current MP";
                case "%chp":
                    return "Max HP";
                case "%cmp":
                    return "Max MP";
                case "%thp":
                    return "Target's Max HP";
                case "%tmp":
                    return "Target's Max MP";
                default:
                    return null;
            }
        }

        private string ParseFormula(string formula)
        {
            var tokens = formula.Split('*');
            if (tokens.Length != 2) return "formula";

            tokens[0] = tokens[0].Trim();
            tokens[1] = tokens[1].Trim();

            string stat = "";
            double mult = 0;
            if (tokens[1][0] == '%')
            {
                stat = ConvertFormulaVariable(tokens[1]);
                double.TryParse(tokens[0], out mult);
            }
            else
            {
                stat = ConvertFormulaVariable(tokens[0]);
                double.TryParse(tokens[1], out mult);
            }

            if (stat == null || mult == 0) return "formula";

            return string.Format("{0:F0}% of {1}", mult * 100, stat);
        }

        private string GetFormulaDescription(string formula, string stat, string prefix)
        {
            if (long.TryParse(formula, out long val))
            {
                return GetValueDescription(stat, val, prefix);
            }
            else
            {
                if (formula[0] == '-')
                    return string.Format("{0}Decrease {1} by {2:N0}", prefix, stat, ParseFormula(formula.Substring(1)));
                else
                    return string.Format("{0}Increase {1} by {2:N0}", prefix, stat, ParseFormula(formula));
            }
        }

        private IEnumerable<string> GetBuffDescription(string prefix)
        {
            if (this.BodyID != 0)
                yield return string.Format("{0}Change Body ID to {1}", prefix, this.BodyID);
            if (this.BodyA != 0)
                yield return string.Format("{0}Change Body Color to {1},{2},{3},{4}", prefix, this.BodyR, this.BodyG, this.BodyB, this.BodyA);
            if (this.HairID != 0)
                yield return string.Format("{0}Change Hair ID to {1}", prefix, this.HairID);
            if (this.HairA != 0)
                yield return string.Format("{0}Change Hair Color to {1},{2},{3},{4}", prefix, this.HairR, this.HairG, this.HairB, this.HairA);
            if (this.FaceID != 0)
                yield return string.Format("{0}Change Face ID to {1}", prefix, this.FaceID);

            if (this.Stats.HP != 0)
                yield return GetValueDescription("Maximum HP", this.Stats.HP, prefix);
            if (this.Stats.MP != 0)
                yield return GetValueDescription("Maximum MP", this.Stats.MP, prefix);
            if (this.Stats.SP != 0)
                yield return GetValueDescription("Maximum SP", this.Stats.SP, prefix);

            if (this.Stats.HPPercentRegen != 0)
                yield return GetPercentageDescription("HP Regeneration", this.Stats.HPPercentRegen, prefix);
            if (this.Stats.HPStaticRegen != 0)
                yield return GetValueDescription("HP Regeneration", this.Stats.HPStaticRegen, prefix);
            if (this.Stats.MPPercentRegen != 0)
                yield return GetPercentageDescription("MP Regeneration", this.Stats.MPPercentRegen, prefix);
            if (this.Stats.MPStaticRegen != 0)
                yield return GetValueDescription("MP Regeneration", this.Stats.MPStaticRegen, prefix);

            if (this.Stats.Strength != 0)
                yield return GetValueDescription("Strength", this.Stats.Strength, prefix);
            if (this.Stats.Stamina != 0)
                yield return GetValueDescription("Stamina", this.Stats.Stamina, prefix);
            if (this.Stats.Intelligence != 0)
                yield return GetValueDescription("Intelligence", this.Stats.Intelligence, prefix);
            if (this.Stats.Dexterity != 0)
                yield return GetValueDescription("Dexterity", this.Stats.Dexterity, prefix);

            if (this.Stats.FireResist != 0)
                yield return GetValueDescription("Fire Resistance", this.Stats.FireResist, prefix);
            if (this.Stats.SpiritResist != 0)
                yield return GetValueDescription("Spirit Resistance", this.Stats.SpiritResist, prefix);
            if (this.Stats.WaterResist != 0)
                yield return GetValueDescription("Water Resistance", this.Stats.WaterResist, prefix);
            if (this.Stats.AirResist != 0)
                yield return GetValueDescription("Air Resistance", this.Stats.AirResist, prefix);
            if (this.Stats.EarthResist != 0)
                yield return GetValueDescription("Earth Resistance", this.Stats.EarthResist, prefix);

            if (this.Stats.AC != 0)
                yield return GetValueDescription("Armor", this.Stats.AC, prefix);

            if (this.Stats.Haste != 0)
                yield return GetPercentageDescription("Melee Attack Speed", this.Stats.Haste, prefix);
            if (this.Stats.SpellDamage != 0)
                yield return GetPercentageDescription("Spell Damage", this.Stats.SpellDamage, prefix);
            if (this.Stats.SpellCrit != 0)
                yield return GetPercentageDescription("Spell Critical Chance", this.Stats.SpellCrit, prefix);
            if (this.Stats.MeleeDamage != 0)
                yield return GetPercentageDescription("Melee Damage", this.Stats.MeleeDamage, prefix);
            if (this.Stats.MeleeCrit != 0)
                yield return GetPercentageDescription("Melee Critical Chance", this.Stats.MeleeCrit, prefix);
            if (this.Stats.DamageReduction != 0)
                yield return GetPercentageDescription("Damage Reduction", this.Stats.DamageReduction, prefix);

            if (this.Stats.MoveSpeedIncrease != 0)
                yield return GetPercentageDescription("Move Speed", this.Stats.MoveSpeedIncrease, prefix);
            if (this.Stats.MoveSpeed != 0)
                yield return GetValueDescription("Move Speed", this.Stats.MoveSpeed, prefix);

            if (this.SnarePercent != 0)
                yield return string.Format("{0}Decrease Move Speed by {1:F0}%", prefix, this.SnarePercent);
        }

        public IEnumerable<string> GetItemDescription(GameWorld world)
        {
            switch (this.EffectType)
            {
                case EffectTypes.Bind:
                    yield return "Set respawn point to this location";
                    break;
                case EffectTypes.Stun:
                    yield return "Stun";
                    break;
                case EffectTypes.Root:
                    yield return "Root";
                    break;
                case EffectTypes.PetTame:
                    yield return "Attempt to tame a pet";
                    break;
                case EffectTypes.PetDefend:
                    yield return "Set pet to defend mode";
                    break;
                case EffectTypes.PetDestroy:
                    yield return "Recall pet";
                    break;
                case EffectTypes.PetFollow:
                    yield return "Set pet to follow mode";
                    break;
                case EffectTypes.PetNeutral:
                    yield return "Set pet to neutral mode";
                    break;
                case EffectTypes.PetAttack:
                    yield return "Tell pet to attack";
                    break;
                case EffectTypes.Formula:
                    if (!string.IsNullOrWhiteSpace(this.HPFormula) && this.HPFormula != "0")
                        yield return GetFormulaDescription(this.HPFormula, "HP", "");
                    if (!string.IsNullOrWhiteSpace(this.MPFormula) && this.MPFormula != "0")
                        yield return GetFormulaDescription(this.MPFormula, "MP", "");
                    if (this.TauntAggro > 0)
                        yield return string.Format("Taunt for {0:N0}", this.TauntAggro);
                    break;
                case EffectTypes.Tick:
                case EffectTypes.Viral:
                    if (!string.IsNullOrWhiteSpace(this.HPFormula) && this.HPFormula != "0")
                        yield return GetFormulaDescription(this.HPFormula, "HP", "Tick> ");
                    if (!string.IsNullOrWhiteSpace(this.MPFormula) && this.MPFormula != "0")
                        yield return GetFormulaDescription(this.MPFormula, "MP", "Tick> ");
                    if (this.TauntAggro > 0)
                        yield return string.Format("Tick> Taunt for {0:N0}", this.TauntAggro);
                    break;
                case EffectTypes.Teleport:
                    var map = world.MapHandler.GetMap(this.TeleportMapID);
                    if (map != null)
                        yield return "Teleport to " + map.Name + " (" + this.TeleportMapX + ", " + this.TeleportMapY + ")";
                    break;
                case EffectTypes.Permanent:
                    foreach (var line in GetBuffDescription("Permanently ")) yield return line;
                    break;
                case EffectTypes.OnMeleeHit:
                    if (this.OnMeleeHitSpell != null)
                    {
                        yield return "When hit by melee, cast " + this.OnMeleeHitSpell.Name;
                        foreach (var line in this.OnMeleeHitSpell.GetItemDescription(world)) yield return line;
                    }
                    break;
                case EffectTypes.OnAttack:
                    if (this.OnMeleeAttackSpell != null)
                    {
                        yield return "When attacking with melee, cast " + this.OnMeleeAttackSpell.Name;
                        foreach (var line in this.OnMeleeAttackSpell.GetItemDescription(world)) yield return line;
                    }
                    break;
                default:
                    foreach (var line in GetBuffDescription("")) yield return line;
                    break;
            }
        }

        /**
         * CastFormulaSpell
         * 
         */
        public bool CastFormulaSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (!this.CanCastSpell(caster, target)) return false;

            List<Player> range = target.Map.GetPlayersInRange(target);
            string packet = "";

            int hpresult = this.ParseFormula(this.HPFormula, caster, target);
            int mpresult = this.ParseFormula(this.MPFormula, caster, target);
            //int spresult = this.ParseFormula(this.SPFormula, caster, target);

            // if target is a player then no sd for pvp if not a heal
            if (!(hpresult < 0 && target is Player) && this.SpellDamageEffects)
            {
                if (world.Random.Next(1, 10001) <= caster.MaxStats.SpellCrit * 10000) hpresult *= 2;
                hpresult = (int)(hpresult * (1 + caster.MaxStats.SpellDamage));

                if (world.Random.Next(1, 10001) <= caster.MaxStats.SpellCrit * 10000) mpresult *= 2;
                mpresult = (int)(mpresult * (1 + caster.MaxStats.SpellDamage));
            }
            hpresult = (int)((decimal)hpresult * GameSettings.Default.DamageModifier);
            target.CurrentMP += mpresult;
            if (hpresult != 0)
            {
                target.Attacked(caster, -hpresult, world);
            }
            else
            {
                packet = target.VPUString();
                if (target is Player)
                {
                    world.Send((Player)target, packet);
                    world.Send((Player)target, ((Player)target).SNFString());
                }
                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }
            }

            if (this.Animation != 0) packet = this.SPPString(target.LoginID);
            if (this.DoCastAnimation) packet += "\x1" + "CST" + caster.LoginID;

            if (target is NPC && this.TauntAggro > 0)
            {
                ((NPC)target).AddAggro((Player)caster, this.TauntAggro, true, world);

                packet += "\x1" + "BT" + target.LoginID + ",60,Taunted";
            }

            packet.TrimStart("\x1".ToCharArray());

            if (target is Player) world.Send((Player)target, packet);
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }

            return true;
        }

        /**
         * CastBindSpell
         * 
         */
        public bool CastBindSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (!this.CanCastSpell(caster, target)) return false;
            if (!caster.Map.CanBind) return false;

            if (caster != target)
            {
                if (((Player)caster).Group == null || !((Player)caster).Group.Players.Contains((Player)target))
                {
                    return false;
                }
            }

            ((Player)target).BoundMap = target.Map;
            ((Player)target).BoundID = target.MapID;
            ((Player)target).BoundX = target.MapX;
            ((Player)target).BoundY = target.MapY;

            List<Player> range = target.Map.GetPlayersInRange(target);
            string packet = "BT" + target.LoginID + ",60,Bound";

            if (this.Animation != 0) packet += "\x1" + this.SPPString(target.LoginID);
            if (this.DoCastAnimation) packet += "\x1" + "CST" + caster.LoginID;

            world.Send((Player)target, "$7Your soul has been bound to this spot.");
            world.Send((Player)target, packet);
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }

            return true;
        }

        /**
         * CastPermanentSpell
         * 
         */
        public bool CastPermanentSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (!this.CanCastSpell(caster, target)) return false;

            List<Player> range = target.Map.GetPlayersInRange(target);

            target.BaseStats += this.Stats;

            if (this.HairID != 0) target.HairID = this.HairID;
            // an alpha value of 0 means don't dye hair. 
            // kinda hackish but it's better than having another field
            if (this.HairA != 0)
            {
                target.HairR = this.HairR;
                target.HairG = this.HairG;
                target.HairB = this.HairB;
                target.HairA = this.HairA;
            }
            if (this.FaceID != 0) target.FaceID = this.FaceID;
            if (this.BodyID != 0)
            {
                if (target.CurrentBodyID == target.BodyID) target.CurrentBodyID = this.BodyID;
                target.BodyID = this.BodyID;
            }
            // an alpha value of 0 means don't dye body. 
            if (this.BodyA != 0)
            {
                target.BodyR = this.BodyR;
                target.BodyG = this.BodyG;
                target.BodyB = this.BodyB;
                target.BodyA = this.BodyA;
            }

            ((Player)target).AddRegenEvent(world);
            string packet = ((Player)target).VPUString() + "\x1" + ((Player)target).CHPString();

            if (this.Animation != 0) packet += "\x1" + this.SPPString(target.LoginID);
            if (this.DoCastAnimation) packet += "\x1" + "CST" + caster.LoginID;
            if (this.OnEffectText != "") world.Send((Player)target, "$7" + this.OnEffectText);

            world.Send((Player)target, ((Player)target).SNFString());
            world.Send((Player)target, packet);
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }

            return true;
        }

        /**
         * CastBuffSpell
         * 
         */
        public bool CastBuffSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (!this.CanCastSpell(caster, target)) return false;

            if (target is Pet && this.BodyID > 0)
            {
                Console.WriteLine("OMG DONT CRASH MY SERVER " + caster.Name);
                // TODO: Is the crash fixed, or am I just logging it?
            }


            if (this.EffectType == EffectTypes.Stun)
            {
                List<Player> range = target.Map.GetPlayersInRange(target);
                string packet = "BT" + target.LoginID + ",50";

                if (target is Player) world.Send((Player)target, packet);
                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }
            }
            else if (this.EffectType == EffectTypes.Root)
            {
                List<Player> range = target.Map.GetPlayersInRange(target);
                string packet = "BT" + target.LoginID + ",11";

                if (target is Player) world.Send((Player)target, packet);
                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }
            }

            Buff buff = new Buff();
            buff.Target = target;
            buff.Caster = caster;
            buff.ItemBuff = false;
            buff.SpellEffect = this;

            target.AddBuff(buff, world);

            return true;
        }

        /**
         * CastTickSpell
         * 
         */
        public bool CastTickSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            return this.CastBuffSpell(caster, target, world);
        }

        /**
         * CastTeleportSpell
         * 
         */
        public bool CastTeleportSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (!this.CanCastSpell(caster, target)) return false;

            if (this.Animation != 0)
            {
                List<Player> range = target.Map.GetPlayersInRange(target);

                string packet = this.SPPString(target.LoginID);

                world.Send((Player)target, packet);
                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }
            }

            // if null tele to bound spot. used for gate spells
            Map map = world.MapHandler.GetMap(this.TeleportMapID);
            if (map == null)
            {
                ((Player)target).WarpTo(world, ((Player)target).BoundMap, 
                    ((Player)target).BoundX, ((Player)target).BoundY);
            }
            else
            {
                if (map.PlayerCanJoin((Player)target, world))
                {
                    ((Player)target).WarpTo(world, map, this.TeleportMapX, this.TeleportMapY);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanCastSpell(ICharacter caster, ICharacter target)
        {
            if (target == null) return false;
            if ((caster.Map != null && !caster.Map.CanCast) && (caster is Player && !((caster as Player).HasPrivilege(AccessPrivilege.CastSpellsWhileBlocked)))) return false;
            if (caster == target)
            {
                if (((int)this.Effected & (int)SpellEffected.Self) == 0) return false;
                else return true;
            }
            if (caster is Player && target is Player && !this.WorksNotInPVP && !target.Map.CanPVP) return false;
            if (target.Level >= this.MinimumLevelEffected && target.Level <= this.MaximumLevelEffected)
            {
                if (target is Player)
                {
                    if (((int)this.Effected & (int)SpellEffected.Player) != 0) return true;
                }
                else if (target is NPC)
                {
                    if (((int)this.Effected & (int)SpellEffected.NPC) != 0)
                    {
                        if (this.EffectType == EffectTypes.Root)
                        {
                            if (((NPC)target).CanBeRooted) return true;
                            else return false;
                        }
                        else if (this.EffectType == EffectTypes.Stun)
                        {
                            if (((NPC)target).CanBeStunned) return true;
                            else return false;
                        }
                        else if (this.EffectType == EffectTypes.Snare)
                        {
                            if (((NPC)target).CanBeSlowed) return true;
                            else return false;
                        }
                        else return true;
                    }
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Pet Taming spell
        /// </summary>
        /// <param name="caster">player who casted spell</param>
        /// <param name="target">npc target</param>
        /// <param name="world"></param>
        /// <returns>true if successful</returns>
        public bool CastTameSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (!((NPC)target).CanBeKilled || ((NPC)target).MoveSpeed == 0) return false;

            Player player = (Player)caster;

            if (player.Pets.Count == GameSettings.Default.PetCountLimit)
            {
                world.Send(player, "$7You have reached the maximum amount of pets. Use /petdelete <id> to release one");
                return false;
            }

            double successrate = (double)(
                player.BaseStats.HP + player.Class.GetLevel(player.Level).BaseStats.HP + 
                player.BaseStats.MP + player.Class.GetLevel(player.Level).BaseStats.MP) / (double)target.MaxStats.HP;


            if (world.Random.Next(1, 101) <= successrate * 100)
            {
                Pet newpet = Pet.FromCharacter((NPC)target);
                player.AddPet(newpet);
                world.Send(player, "$7Successfully tamed " + target.Name + ".");
                newpet.SaveToDatabase(world);
                return true;
            }
            else
            {
                world.Send(player, "$7Failed to tame " + target.Name + ". (" + target.MaxStats.HP + " hp)");
                return false;
            }
        }

        /// <summary>
        /// Sets the targetted pet into defend mode
        /// </summary>
        /// <param name="caster">should be the pet owner but gets checked</param>
        /// <param name="target">a pet</param>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool CastPetDefendSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            Pet pet = (Pet)target;
            if (pet.Owner != caster) return false;

            pet.Target = null;
            pet.Mode = Pet.Modes.Defend;

            return true;
        }

        /// <summary>
        /// Destroys the selected pet
        /// </summary>
        /// <param name="caster">should be the pet owner but gets checked</param>
        /// <param name="target">a pet</param>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool CastPetDestroySpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            Pet pet = (Pet)target;
            if (pet.Owner != caster) return false;

            pet.Destroy(world);

            return true;
        }

        /// <summary>
        /// Sets the targetted pet into follow mode
        /// </summary>
        /// <param name="caster">should be the pet owner but gets checked</param>
        /// <param name="target">a pet</param>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool CastPetFollowSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            Pet pet = (Pet)target;
            if (pet.Owner != caster) return false;

            pet.Target = null;
            pet.Mode = Pet.Modes.Follow;

            return true;
        }

        /// <summary>
        /// Sets the targetted pet into neutral mode
        /// </summary>
        /// <param name="caster">should be the pet owner but gets checked</param>
        /// <param name="target">a pet</param>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool CastPetNeutralSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            Pet pet = (Pet)target;
            if (pet.Owner != caster) return false;

            pet.Target = null;
            pet.Mode = Pet.Modes.Neutral;

            return true;
        }

        /// <summary>
        /// Sets a random neutral pet into attack mode
        /// </summary>
        /// <param name="caster">pet owner</param>
        /// <param name="target">a pet</param>
        /// <param name="world"></param>
        /// <returns></returns>
        public bool CastPetAttackSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (target is Player && !target.Map.CanPVP) return false;

            List<Pet> pets = (from p in ((Player)caster).Pets where p.IsAlive && p.Mode == Pet.Modes.Neutral select p).ToList();

            if (pets.Count == 0) return false;

            Pet pet = pets[world.Random.Next(0, pets.Count)];

            pet.Mode = Pet.Modes.Attack;
            pet.Target = target;
            pet.AddAttackEvent(world);

            return true;
        }

        /**
         * CastSpell, uses the right spell function to cast the spell
         * 
         */
        public bool CastSpell(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (this.EffectType == EffectTypes.Formula)
            {
                return this.CastFormulaSpell(caster, target, world);
            }
            // bind can only effect players
            else if (this.EffectType == EffectTypes.Bind && target is Player)
            {
                return this.CastBindSpell(caster, target, world);
            }
            // I think permanent can only effect players
            else if (this.EffectType == EffectTypes.Permanent && target is Player)
            {
                return this.CastPermanentSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.Buff)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.Tick)
            {
                return this.CastTickSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.TickBuff)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.Stun)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.Root)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            // snare can only effect npcs
            else if (this.EffectType == EffectTypes.Snare && target is NPC)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.Invisible)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.SeeInvisible)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            // teleport can only effect players
            else if (this.EffectType == EffectTypes.Teleport && target is Player)
            {
                return this.CastTeleportSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.Viral)
            {
                return this.CastFormulaSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.OnMeleeHit)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.OnAttack)
            {
                return this.CastBuffSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.PetTame && target is NPC)
            {
                return this.CastTameSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.PetDefend && target is Pet)
            {
                this.CastPetDefendSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.PetDestroy && target is Pet)
            {
                this.CastPetDestroySpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.PetFollow && target is Pet)
            {
                this.CastPetFollowSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.PetNeutral && target is Pet)
            {
                this.CastPetNeutralSpell(caster, target, world);
            }
            else if (this.EffectType == EffectTypes.PetAttack)
            {
                this.CastPetAttackSpell(caster, target, world);
            }

            return false;
        }

        /**
         * Cast, figures out how to cast the spell, centered on target
         * 
         */
        public bool Cast(ICharacter caster, ICharacter target, GameWorld world)
        {
            if (!this.WorksInPVP && target.Map.CanPVP) return false;

            if (this.TargetType == TargetTypes.Target)
            {
                return this.CastSpell(caster, target, world);
            }
            else
            {
                int ox = target.MapX;
                int oy = target.MapY;
                ICharacter hit;
                string packet = "";
                Map map = target.Map;
                List<Player> range = map.GetPlayersInRange(target);

                bool hitone = false;

                if (this.DoAttackAnimation) packet += "ATT" + caster.LoginID;
                if (this.DoCastAnimation) packet += "\x1" + "CST" + caster.LoginID;

                if (this.TargetType == TargetTypes.LineFront)
                {
                    int x = ox;
                    int y = oy;

                    for (int i = 1; i <= this.TargetSize; i++)
                    {
                        switch (caster.Facing)
                        {
                            case 1: y--; break;
                            case 2: x++; break;
                            case 3: y++; break;
                            case 4: x--; break;
                        }

                        if ((hit = map.GetCharacterAt(x, y)) != null)
                        {
                            this.CastSpell(caster, hit, world);

                            if (this.OnlyHitsOneNPC && hit is NPC) hitone = true;
                        }
                        if (this.Display == SpellDisplays.Tile && this.Animation != 0)
                        {
                            packet += "\x1" + this.SPAString(x, y);
                        }

                        if (hitone) break;
                    }
                }
                else if (this.TargetType == TargetTypes.Area)
                {
                    for (int y = oy - this.TargetSize; y <= oy + this.TargetSize; y++)
                    {
                        for (int x = ox - this.TargetSize; x <= ox + this.TargetSize; x++)
                        {
                            if ((hit = map.GetCharacterAt(x, y)) != null)
                            {
                                this.CastSpell(caster, hit, world);

                                if (this.OnlyHitsOneNPC && hit is NPC) hitone = true;
                            }
                            if (this.Display == SpellDisplays.Tile && this.Animation != 0)
                            {
                                packet += "\x1" + this.SPAString(x, y);
                            }

                            if (hitone) break;
                        }

                        if (hitone) break;
                    }
                }
                else if (this.TargetType == TargetTypes.Cross)
                {
                    for (int y = oy - this.TargetSize; y <= oy + this.TargetSize; y++)
                    {
                        for (int x = ox - this.TargetSize; x <= ox + this.TargetSize; x++)
                        {
                            if (x == ox - (y - oy) || x == ox + (y - oy))
                            {
                                if ((hit = map.GetCharacterAt(x, y)) != null)
                                {
                                    this.CastSpell(caster, hit, world);

                                    if (this.OnlyHitsOneNPC && hit is NPC) hitone = true;
                                }
                                if (this.Display == SpellDisplays.Tile && this.Animation != 0)
                                {
                                    packet += "\x1" + this.SPAString(x, y);
                                }
                            }
                            if (hitone) break;
                        }
                        if (hitone) break;
                    }
                }
                else if (this.TargetType == TargetTypes.Plus)
                {
                    for (int y = oy - this.TargetSize; y <= oy + this.TargetSize; y++)
                    {
                        for (int x = ox - this.TargetSize; x <= ox + this.TargetSize; x++)
                        {
                            if (x == ox || y == oy)
                            {
                                if ((hit = map.GetCharacterAt(x, y)) != null)
                                {
                                    this.CastSpell(caster, hit, world);

                                    if (this.OnlyHitsOneNPC && hit is NPC) hitone = true;
                                }
                                if (this.Display == SpellDisplays.Tile && this.Animation != 0)
                                {
                                    packet += "\x1" + this.SPAString(x, y);
                                }
                            }
                            if (hitone) break;
                        }
                        if (hitone) break;
                    }
                }
                else if (this.TargetType == TargetTypes.Random)
                {
                    List<Point> points = new List<Point>();
                    List<Point> done = new List<Point>();

                    Point point;

                    points.Add(new Point(ox, oy));
                    while (points.Count > 0)
                    {
                        point = points[0];
                        points.RemoveAt(0);

                        if (point.x >= ox - this.TargetSize &&
                            point.x <= ox + this.TargetSize &&
                            point.y >= oy - this.TargetSize &&
                            point.y <= oy + this.TargetSize)
                        {
                            if (!done.Contains(point))
                            {
                                done.Add(point);

                                if (world.Random.Next(1, 10001) <= this.RandomJoinChance * 100)
                                {
                                    points.Add(new Point(point.x - 1, point.y));
                                }
                                if (world.Random.Next(1, 10001) <= this.RandomJoinChance * 100)
                                {
                                    points.Add(new Point(point.x + 1, point.y));
                                }
                                if (world.Random.Next(1, 10001) <= this.RandomJoinChance * 100)
                                {
                                    points.Add(new Point(point.x, point.y - 1));
                                }
                                if (world.Random.Next(1, 10001) <= this.RandomJoinChance * 100)
                                {
                                    points.Add(new Point(point.x, point.y + 1));
                                }
                            }
                        }
                    }

                    foreach (Point p in done)
                    {
                        if ((hit = map.GetCharacterAt(p.x, p.y)) != null)
                        {
                            this.CastSpell(caster, hit, world);

                            if (this.OnlyHitsOneNPC && hit is NPC) hitone = true;
                        }
                        if (this.Display == SpellDisplays.Tile && this.Animation != 0)
                        {
                            packet += "\x1" + this.SPAString(p.x, p.y);
                        }
                        if (hitone) break;
                    }
                }

                else if (this.TargetType == TargetTypes.TriangleFront)
                {
                    int x = ox;
                    int y = oy;

                    for (int i = 1; i <= this.TargetSize; i++)
                    {
                        switch (caster.Facing)
                        {
                            case 1: 
                                y--;
                                x = ox - i;
                                break;
                            case 2: 
                                x++;
                                y = oy - i;
                                break;
                            case 3: 
                                y++;
                                x = ox - i;
                                break;
                            case 4: 
                                x--;
                                y = oy - i;
                                break;
                        }

                        for (int j = 0; j < i * 2 - 1; j++)
                        {
                            switch (caster.Facing)
                            {
                                case 1:
                                    x++;
                                    break;
                                case 2:
                                    y++;
                                    break;
                                case 3:
                                    x++;
                                    break;
                                case 4:
                                    y++;
                                    break;
                            }

                            if ((hit = map.GetCharacterAt(x, y)) != null)
                            {
                                this.CastSpell(caster, hit, world);

                                if (this.OnlyHitsOneNPC && hit is NPC) hitone = true;
                            }

                            if (this.Display == SpellDisplays.Tile && this.Animation != 0)
                            {
                                packet += "\x1" + this.SPAString(x, y);
                            }

                            if (hitone) break;
                        }

                        if (hitone) break;
                    }
                }

                packet.TrimStart("\x1".ToCharArray());

                if (target is Player) world.Send((Player)target, packet);

                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }
            }


            return true;
        }

        /**
         * ParseFormula, parse spell formula
         * 
         * Uses http://en.wikipedia.org/wiki/Shunting-yard_algorithm to convert formula to reverse polish notation
         * 
         * Then uses http://en.wikipedia.org/wiki/Reverse_Polish_notation#The_postfix_algorithm to calculate the
         * result
         * 
         */
        public int ParseFormula(string formula, ICharacter caster, ICharacter target)
        {
            List<Object> result = new List<Object>();
            Stack<char> operators = new Stack<char>();

            string buffer = "";
            char token;
            char op;
            decimal value;

            Hashtable symbolToValue = new Hashtable();
            symbolToValue.Add("%cchp", caster.CurrentHP);
            symbolToValue.Add("%ccmp", caster.CurrentMP);
            symbolToValue.Add("%ccsp", caster.CurrentSP);
            symbolToValue.Add("%cstr", caster.MaxStats.Strength);
            symbolToValue.Add("%cwdmg", caster.WeaponDamage);
            symbolToValue.Add("%clevel", caster.Level);
            symbolToValue.Add("%chp", caster.MaxStats.HP);
            symbolToValue.Add("%cmp", caster.MaxStats.MP);

            symbolToValue.Add("%tchp", target.CurrentHP);
            symbolToValue.Add("%tcmp", target.CurrentMP);
            symbolToValue.Add("%tcsp", target.CurrentSP);
            symbolToValue.Add("%tstr", target.MaxStats.Strength);
            symbolToValue.Add("%twdmg", target.WeaponDamage);
            symbolToValue.Add("%tlevel", target.Level);
            symbolToValue.Add("%thp", target.MaxStats.HP);
            symbolToValue.Add("%tmp", target.MaxStats.MP);

            for (int i = 0; i < formula.Length; i++)
            {
                token = formula[i];

                if (token == '-' || token == '+')
                {
                    if (buffer.Length == 0 && result.Count == 0) buffer += '0';
                    if (buffer.Length > 0)
                    {
                        if (buffer[0] == '%')
                        {
                            value = (int)symbolToValue[buffer];
                            // bad symbol
                            // needa log here
                            //if (value == null)
                            //{
                            //    throw new Exception("Parse error: bad symbol " + buffer);
                            //}
                        }
                        else
                        {
                            value = Convert.ToDecimal(buffer);
                        }

                        result.Add(value);
                        buffer = "";
                    }

                    while (operators.Count > 0)
                    {
                        op = operators.Peek() ;
                        // +/- is lowest precedence so pop all operators
                        if (op == '-' || op == '+' || op == '*' || op == '/')
                            result.Add(operators.Pop());
                        else
                            break;
                    }

                    operators.Push(token);
                }

                else if (token == '*' || token == '/')
                {
                    if (buffer.Length != 0)
                    {
                        if (buffer[0] == '%')
                        {
                            value = (int)symbolToValue[buffer];
                            // bad symbol
                            // needa log here
                            //if (value == null)
                            //{
                            //    throw new Exception("Parse error: bad symbol " + buffer);
                            //}
                        }
                        else
                        {
                            value = Convert.ToDecimal(buffer);
                        }

                        result.Add(value);
                        buffer = "";
                    }

                    while (operators.Count > 0)
                    {
                        op = operators.Peek();
                        if (op == '*' || op == '/')
                            result.Add(operators.Pop());
                        else
                            break;
                    }

                    operators.Push(token);
                }

                else if (token >= 'a' && token <= 'z')
                {
                    if (buffer.Length > 0 && buffer[0] == '%')
                    {
                        buffer += token;
                    }
                    else
                    {
                        throw new Exception("Parse error: can't mix numbers and symbols");
                    }
                }
                else if (token == '.' || token >= '0' && token <= '9')
                {
                    if (buffer.Length > 0 && buffer[0] == '%')
                    {
                        throw new Exception("Parse error: can't mix symbols and numbers");
                    }
                    else
                    {
                        buffer += token;
                    }
                }
                else if (token == '%')
                {
                    if (buffer.Length == 0)
                    {
                        buffer += token;
                    }
                    else
                    {
                        throw new Exception("Parse error: can't mix numbers and symbols");
                    }
                }

                else if (token == '(')
                {
                    operators.Push(token);
                }

                else if (token == ')')
                {
                    if (buffer.Length != 0)
                    {
                        if (buffer[0] == '%')
                        {
                            value = (int)symbolToValue[buffer];
                            // bad symbol
                            // needa log here
                            //if (value == null)
                            //{
                            //    throw new Exception("Parse error: bad symbol " + buffer);
                            //}
                        }
                        else
                        {
                            value = Convert.ToDecimal(buffer);
                        }

                        result.Add(value);
                        buffer = "";
                    }

                    while (operators.Count > 0)
                    {
                        op = operators.Peek();
                        if (op != '(')
                            result.Add(operators.Pop());
                        else
                            break;
                    }

                    if (operators.Count > 0 && operators.Pop() != '(')
                    {
                        throw new Exception("Parse error: parenthesis mismatch");
                    }
                }
            }

            if (buffer.Length != 0)
            {
                if (buffer[0] == '%')
                {
                    value = (int)symbolToValue[buffer];
                    // bad symbol
                    // needa log here
                    //if (value == null)
                    //{
                    //    throw new Exception("Parse error: bad symbol " + buffer);
                    //}
                }
                else
                {
                    value = Convert.ToDecimal(buffer);
                }

                result.Add(value);
                buffer = "";
            }

            while (operators.Count > 0)
            {
                op = operators.Peek();
                if (op == '(')
                    throw new Exception("Parse error: parenthesis mismatch");
                else
                    result.Add(operators.Pop());
            }

            decimal rs, ls;
            Object cur;

            while (result.Count > 1)
            {
                cur = result[0];
                result.RemoveAt(0);

                if (cur is char)
                {
                    rs = (decimal)result[result.Count - 1];
                    ls = (decimal)result[result.Count - 2];

                    result.RemoveAt(result.Count - 1);
                    result.RemoveAt(result.Count - 1);

                    op = (char)cur;
                    switch (op)
                    {
                        case '*':
                            result.Add(ls * rs);
                            break;
                        case '/':
                            result.Add(ls / rs);
                            break;
                        case '+':
                            result.Add(ls + rs);
                            break;
                        case '-':
                            result.Add(ls - rs);
                            break;
                    }
                }
                else
                {
                    result.Add(cur);
                }
            }

            return Convert.ToInt32(result[0]);
        }
    }
}
