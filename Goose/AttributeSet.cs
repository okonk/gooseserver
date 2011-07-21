using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * AttributeSet, holds a set of attributes
     * 
     */
    public class AttributeSet
    {
        public int HP { get; set; }
        public int MP { get; set; }
        public int SP { get; set; }
        public decimal HPPercentRegen { get; set; }
        public int HPStaticRegen { get; set; }
        public decimal MPPercentRegen { get; set; }
        public int MPStaticRegen { get; set; }
        public int Strength { get; set; }
        public int Stamina { get; set; }
        public int Intelligence { get; set; }
        public int Dexterity { get; set; }
        public int FireResist { get; set; }
        public int SpiritResist { get; set; }
        public int WaterResist { get; set; }
        public int AirResist { get; set; }
        public int EarthResist { get; set; }
        public int AC { get; set; }
        public decimal Haste { get; set; }
        public decimal SpellDamage { get; set; }
        public decimal SpellCrit { get; set; }
        public decimal MeleeDamage { get; set; }
        public decimal MeleeCrit { get; set; }
        public decimal DamageReduction { get; set; }

        public AttributeSet()
        {
            this.HP = 0;
            this.MP = 0;
            this.SP = 0;
            this.HPPercentRegen = 0;
            this.HPStaticRegen = 0;
            this.MPPercentRegen = 0;
            this.MPStaticRegen = 0;
            this.Strength = 0;
            this.Stamina = 0;
            this.Intelligence = 0;
            this.Dexterity = 0;
            this.FireResist = 0;
            this.SpiritResist = 0;
            this.WaterResist = 0;
            this.AirResist = 0;
            this.EarthResist = 0;
            this.AC = 0;
            this.Haste = 0;
            this.SpellDamage = 0;
            this.SpellCrit = 0;
            this.MeleeDamage = 0;
            this.MeleeCrit = 0;
            this.DamageReduction = 0;
        }

        public static AttributeSet operator +(AttributeSet a1, AttributeSet a2)
        {
            AttributeSet temp = new AttributeSet();

            temp.HP = a1.HP + a2.HP;
            temp.MP = a1.MP + a2.MP;
            temp.SP = a1.SP + a2.SP;
            temp.HPPercentRegen = a1.HPPercentRegen + a2.HPPercentRegen;
            temp.HPStaticRegen = a1.HPStaticRegen + a2.HPStaticRegen;
            temp.MPPercentRegen = a1.MPPercentRegen + a2.MPPercentRegen;
            temp.MPStaticRegen = a1.MPStaticRegen + a2.MPStaticRegen;
            temp.Strength = a1.Strength + a2.Strength;
            temp.Stamina = a1.Stamina + a2.Stamina;
            temp.Intelligence = a1.Intelligence + a2.Intelligence;
            temp.Dexterity = a1.Dexterity + a2.Dexterity;
            temp.FireResist = a1.FireResist + a2.FireResist;
            temp.SpiritResist = a1.SpiritResist + a2.SpiritResist;
            temp.WaterResist = a1.WaterResist + a2.WaterResist;
            temp.AirResist = a1.AirResist + a2.AirResist;
            temp.EarthResist = a1.EarthResist + a2.EarthResist;
            temp.AC = a1.AC + a2.AC;
            temp.Haste = a1.Haste + a2.Haste;
            temp.SpellDamage = a1.SpellDamage + a2.SpellDamage;
            temp.SpellCrit = a1.SpellCrit + a2.SpellCrit;
            temp.MeleeDamage = a1.MeleeDamage + a2.MeleeDamage;
            temp.MeleeCrit = a1.MeleeCrit + a2.MeleeCrit;
            temp.DamageReduction = a1.DamageReduction + a2.DamageReduction;

            return temp;
        }

        public static AttributeSet operator -(AttributeSet a1, AttributeSet a2)
        {
            AttributeSet temp = new AttributeSet();

            temp.HP = a1.HP - a2.HP;
            temp.MP = a1.MP - a2.MP;
            temp.SP = a1.SP - a2.SP;
            temp.HPPercentRegen = a1.HPPercentRegen - a2.HPPercentRegen;
            temp.HPStaticRegen = a1.HPStaticRegen - a2.HPStaticRegen;
            temp.MPPercentRegen = a1.MPPercentRegen - a2.MPPercentRegen;
            temp.MPStaticRegen = a1.MPStaticRegen - a2.MPStaticRegen;
            temp.Strength = a1.Strength - a2.Strength;
            temp.Stamina = a1.Stamina - a2.Stamina;
            temp.Intelligence = a1.Intelligence - a2.Intelligence;
            temp.Dexterity = a1.Dexterity - a2.Dexterity;
            temp.FireResist = a1.FireResist - a2.FireResist;
            temp.SpiritResist = a1.SpiritResist - a2.SpiritResist;
            temp.WaterResist = a1.WaterResist - a2.WaterResist;
            temp.AirResist = a1.AirResist - a2.AirResist;
            temp.EarthResist = a1.EarthResist - a2.EarthResist;
            temp.AC = a1.AC - a2.AC;
            temp.Haste = a1.Haste - a2.Haste;
            temp.SpellDamage = a1.SpellDamage - a2.SpellDamage;
            temp.SpellCrit = a1.SpellCrit - a2.SpellCrit;
            temp.MeleeDamage = a1.MeleeDamage - a2.MeleeDamage;
            temp.MeleeCrit = a1.MeleeCrit - a2.MeleeCrit;
            temp.DamageReduction = a1.DamageReduction - a2.DamageReduction;

            return temp;
        }

        public static AttributeSet operator *(AttributeSet a1, decimal multiplier)
        {
            AttributeSet temp = new AttributeSet();

            temp.HP = (int)Math.Ceiling(a1.HP * multiplier);
            temp.MP = (int)Math.Ceiling(a1.MP * multiplier);
            temp.SP = (int)Math.Ceiling(a1.SP * multiplier);
            temp.HPPercentRegen = a1.HPPercentRegen * multiplier;
            temp.HPStaticRegen = (int)Math.Ceiling(a1.HPStaticRegen * multiplier);
            temp.MPPercentRegen = a1.MPPercentRegen * multiplier;
            temp.MPStaticRegen = (int)Math.Ceiling(a1.MPStaticRegen * multiplier);
            temp.Strength = (int)Math.Ceiling(a1.Strength * multiplier);
            temp.Stamina = (int)Math.Ceiling(a1.Stamina * multiplier);
            temp.Intelligence = (int)Math.Ceiling(a1.Intelligence * multiplier);
            temp.Dexterity = (int)Math.Ceiling(a1.Dexterity * multiplier);
            temp.FireResist = (int)Math.Ceiling(a1.FireResist * multiplier);
            temp.SpiritResist = (int)Math.Ceiling(a1.SpiritResist * multiplier);
            temp.WaterResist = (int)Math.Ceiling(a1.WaterResist * multiplier);
            temp.AirResist = (int)Math.Ceiling(a1.AirResist * multiplier);
            temp.EarthResist = (int)Math.Ceiling(a1.EarthResist * multiplier);
            temp.AC = (int)Math.Ceiling(a1.AC * multiplier);
            temp.Haste = a1.Haste * multiplier;
            temp.SpellDamage = a1.SpellDamage * multiplier;
            temp.SpellCrit = a1.SpellCrit * multiplier;
            temp.MeleeDamage = a1.MeleeDamage * multiplier;
            temp.MeleeCrit = a1.MeleeCrit * multiplier;
            temp.DamageReduction = a1.DamageReduction * multiplier;

            return temp;
        }
    }
}
