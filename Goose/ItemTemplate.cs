using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * ItemTemplate, base stats for an item
     * 
     */
    public class ItemTemplate : IItem
    {
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

        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public UseTypes UseType { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public long MinExperience { get; set; }
        public long MaxExperience { get; set; }
        public AttributeSet BaseStats { get; set; }
        public int WeaponDelay { get; set; }
        public int WeaponDamage { get; set; }
        public ItemSlots Slot { get; set; }
        public ItemTypes Type { get; set; }
        public int GraphicEquipped { get; set; }
        public int GraphicTile { get; set; }
        public int GraphicFile { get; set; }
        public int GraphicR { get; set; }
        public int GraphicG { get; set; }
        public int GraphicB { get; set; }
        public int GraphicA { get; set; }
        public long Value { get; set; }
        public bool IsLore { get; set; }
        public bool IsBindOnPickup { get; set; }
        public bool IsBindOnEquip { get; set; }
        public bool IsEvent { get; set; }
        /**
         * This is a bitmask
         * Therefore only limited to about 64 classes, which should be enough.
         * If the bit is set then that class id CAN'T use the item.
         * 
         */
        public long ClassRestrictions { get; set; }
        public int StackSize { get; set; }
        /**
         * Body pose/state 1 for normal, 3 for staff, 4 for sword
         */
        public int BodyState { get; set; }
        /**
         * Spell effect id
         */
        public int SpellEffectID { get; set; }
        /**
         * Spell effect
         */
        public SpellEffect SpellEffect { get; set; }
        public decimal SpellEffectChance { get; set; }
        public int LearnSpellID { get; set; }

        public int Credits { get; set; }

        public int BodyType
        {
            get
            {
                switch (Slot)
                {
                    case ItemTemplate.ItemSlots.Belt:
                        return 8;

                    case ItemTemplate.ItemSlots.Chest:
                        return 2;

                    case ItemTemplate.ItemSlots.Cloak:
                        return 7;

                    case ItemTemplate.ItemSlots.Gloves:
                        return 4;

                    case ItemTemplate.ItemSlots.Helmet:
                        return 1;

                    case ItemTemplate.ItemSlots.Necklace:
                        return 9;

                    case ItemTemplate.ItemSlots.OneHanded:
                        return 11;

                    case ItemTemplate.ItemSlots.Pants:
                        return 5;

                    case ItemTemplate.ItemSlots.Pauldrons:
                        return 3;

                    case ItemTemplate.ItemSlots.Ring:
                        return 10;

                    case ItemTemplate.ItemSlots.Shield:
                        return 12;

                    case ItemTemplate.ItemSlots.Shoes:
                        return 6;

                    case ItemTemplate.ItemSlots.TwoHanded:
                        return 11;

                    case ItemTemplate.ItemSlots.Mount:
                        return 13;

                    default:
                        return 0;

                }
            }
        }

        public int Flags
        {
            get
            {
                return (0 | (IsLore ? 8 : 0) | (IsBindOnPickup ? 2 : 0) | (IsBindOnEquip ? 0x80 : 0) | (IsEvent ? 0x10 : 0));
            }
        }

        public string GetSlotPacket(GameWorld world, int slotId, long stack)
        {
            return slotId + "|" +
                    this.GraphicTile + "|" +
                    this.GraphicFile + "|" +
                    "" + "|" + // title
                    this.Name + "|" +
                    "" + "|" + //surname
                    stack + "|" +
                    this.Value + "|" +
                    this.Flags + "|" +
                    this.Description + "|" +
                    this.WeaponDamage + "|" +
                    this.WeaponDamage + "|" +
                    (this.WeaponDamage > 0 ? this.WeaponDelay : 0) + "|" +
                    (int)this.Type + "|" +
                    this.BaseStats.AC + "|" +
                    this.BaseStats.HP + "|" +
                    this.BaseStats.MP + "|" +
                    this.BaseStats.SP + "|" +
                    this.BaseStats.Strength + "|" +
                    this.BaseStats.Stamina + "|" +
                    this.BaseStats.Intelligence + "|" +
                    this.BaseStats.Dexterity + "|" +
                    this.BaseStats.FireResist + "|" +
                    this.BaseStats.WaterResist + "|" +
                    this.BaseStats.EarthResist + "|" +
                    this.BaseStats.AirResist + "|" +
                    this.BaseStats.SpiritResist + "|" +
                    this.MinLevel + "|" +
                    this.MaxLevel + "|" +
                    FigureClassRestrictions(world, this.ClassRestrictions) +
                    "0" + "|" + // gm access
                    "0" + "|" + // gender, always 0 since we don't care about gender
                    (this.SpellEffect == null ? "" : this.SpellEffect.Name) + "|" +
                    (int)this.SpellEffectChance + "|" +
                    this.BodyType + "|" +
                    (int)this.UseType + "|" +
                    0 + "|" + // not sure
                    this.GraphicR + "|" +
                    this.GraphicG + "|" +
                    this.GraphicB + "|" +
                    this.GraphicA;
        }

        public static string FigureClassRestrictions(GameWorld world, long classRestrictions)
        {
            var canUse = new List<Class>();
            var cantUse = new List<Class>();
            var allClasses = world.ClassHandler.Classes;

            foreach (Class cls in allClasses)
            {
                if ((classRestrictions & Convert.ToInt64(Math.Pow(2.0, (double)cls.ClassID))) != 0)
                {
                    cantUse.Add(cls);
                }
                else
                {
                    canUse.Add(cls);
                }
            }

            if (cantUse.Count == 0)
            {
                return "0|0|0|";
            }

            string output = "";

            if (canUse.Count <= 3)
            {
                foreach (Class cls in canUse)
                {
                    output += (cls.ClassID);
                    output += "|";
                }

                for (int i = 0; i < 3 - canUse.Count; i++)
                {
                    output += "0|";
                }
            }
            else if (cantUse.Count <= 3)
            {
                foreach (Class cls in cantUse)
                {
                    // +50 = can't use
                    output += (cls.ClassID + 50);
                    output += "|";
                }

                for (int i = 0; i < 3 - cantUse.Count; i++)
                {
                    output += "0|";
                }
            }
            else
            {
                // more than 3 can and can't use.. what do?
            }

            return output;
        }
    }
}
