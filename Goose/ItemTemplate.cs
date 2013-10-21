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
            Equipment = 0,
            Consumable,
            Useless,
            Scroll
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
            Mount
        }
        public enum ItemTypes
        {
            Accessory = 0,
            Cloth,
            Silk,
            Plate,
            OneHandedBlunt,
            OneHandedSlash,
            OneHandedPierce,
            TwoHandedBlunt,
            TwoHandedSlash,
            TwoHandedPierce
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
    }
}
