using Goose.Scripting;
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

        public int ID { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public UseTypes UseType { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public long MinExperience { get; set; }
        public long MaxExperience { get; set; }
        public AttributeSet BaseStats { get; set; } = null!;
        public int WeaponDelay { get; set; }
        public int WeaponDamage { get; set; }
        public int TotalWeaponDamage { get => this.WeaponDamage; }
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
         * If the bit is set then that class id CAN use the item, and 0 means every class can.
         * See Class.CanUse.
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
        public SpellEffect? SpellEffect { get; set; }
        public decimal SpellEffectChance { get; set; }
        public int LearnSpellID { get; set; }

        public int Credits { get; set; }

        /// <summary>Overrides the vendor's currency for this item. Null means "use whatever
        /// the vendor deals in". Runtime-only - there is no items column for it, so sheet
        /// data never sets it; scripts do (Scripts/Global/Dimensions.csx).</summary>
        public string? CurrencyId { get; set; }

        public Script<IItemScript>? Script { get; set; }

        public string ScriptParams { get; set; } = null!;

        public ItemTemplate() { }

        /// <summary>Copies every field. Used by scripts that generate template variants
        /// (see Scripts/Global/Dimensions.csx). BaseStats is copied by value - a shared
        /// AttributeSet would let a generated clone mutate the sheet-authored original.</summary>
        public ItemTemplate(ItemTemplate other)
        {
            this.ID = other.ID;
            this.Name = other.Name;
            this.Description = other.Description;
            this.UseType = other.UseType;
            this.MinLevel = other.MinLevel;
            this.MaxLevel = other.MaxLevel;
            this.MinExperience = other.MinExperience;
            this.MaxExperience = other.MaxExperience;
            this.BaseStats = new AttributeSet() + other.BaseStats;
            this.WeaponDelay = other.WeaponDelay;
            this.WeaponDamage = other.WeaponDamage;
            this.Slot = other.Slot;
            this.Type = other.Type;
            this.GraphicEquipped = other.GraphicEquipped;
            this.GraphicTile = other.GraphicTile;
            this.GraphicFile = other.GraphicFile;
            this.GraphicR = other.GraphicR;
            this.GraphicG = other.GraphicG;
            this.GraphicB = other.GraphicB;
            this.GraphicA = other.GraphicA;
            this.Value = other.Value;
            this.IsLore = other.IsLore;
            this.IsBindOnPickup = other.IsBindOnPickup;
            this.IsBindOnEquip = other.IsBindOnEquip;
            this.IsEvent = other.IsEvent;
            this.ClassRestrictions = other.ClassRestrictions;
            this.StackSize = other.StackSize;
            this.BodyState = other.BodyState;
            this.SpellEffectID = other.SpellEffectID;
            this.SpellEffect = other.SpellEffect;
            this.SpellEffectChance = other.SpellEffectChance;
            this.LearnSpellID = other.LearnSpellID;
            this.Credits = other.Credits;
            this.CurrencyId = other.CurrencyId;
            this.Script = other.Script;
            this.ScriptParams = other.ScriptParams;
        }

        public int BodyType
        {
            get
            {
                return Slot switch
                {
                    ItemTemplate.ItemSlots.Belt => 8,
                    ItemTemplate.ItemSlots.Chest => 2,
                    ItemTemplate.ItemSlots.Cloak => 7,
                    ItemTemplate.ItemSlots.Gloves => 4,
                    ItemTemplate.ItemSlots.Helmet => 1,
                    ItemTemplate.ItemSlots.Necklace => 9,
                    ItemTemplate.ItemSlots.OneHanded => 11,
                    ItemTemplate.ItemSlots.Pants => 5,
                    ItemTemplate.ItemSlots.Pauldrons => 3,
                    ItemTemplate.ItemSlots.Ring => 10,
                    ItemTemplate.ItemSlots.Shield => 12,
                    ItemTemplate.ItemSlots.Shoes => 6,
                    ItemTemplate.ItemSlots.TwoHanded => 11,
                    ItemTemplate.ItemSlots.Mount => 13,
                    _ => 0,
                };
            }
        }

        public int Flags
        {
            get => (0 | (IsLore ? 8 : 0) | (IsBindOnPickup ? 2 : 0) | (IsBindOnEquip ? 0x80 : 0) | (IsEvent ? 0x10 : 0));
        }

        public static string FigureClassRestrictions(GameWorld world, long classRestrictions)
        {
            var canUse = new List<Class>();
            var cantUse = new List<Class>();
            var allClasses = world.ClassHandler.Classes;

            foreach (var cls in allClasses)
            {
                if (cls.CanUse(classRestrictions))
                {
                    canUse.Add(cls);
                }
                else
                {
                    cantUse.Add(cls);
                }
            }

            if (cantUse.Count == 0)
            {
                return "0|0|0|";
            }

            string output = "";

            if (canUse.Count <= 3)
            {
                foreach (var cls in canUse)
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
                foreach (var cls in cantUse)
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
