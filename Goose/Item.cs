using System.Text;
using System.Data.SqlClient;
using System.Data;
using Goose.Scripting;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goose
{
    public enum ItemProperty
    {
        TitleId,
        SurnameId,
    }

    /**
     * Item, holds the actual item data
     *
     * Each item in game is seperate
     * Holds the original template and modified/added stats
     *
     */
    public class Item : IItem
    {
        [JsonPropertyName("id")]
        public int ItemID { get; set; }
        [JsonPropertyName("tid")]
        public int TemplateID { get; set; }
        [JsonIgnore]
        public ItemTemplate Template { get; set; } = null!;

        public string Name { get; set; } = null!;
        [JsonPropertyName("desc")]
        [DefaultValue("")]
        public string Description { get; set; } = "";

        [JsonPropertyName("ge")]
        public int GraphicEquipped { get; set; }
        [JsonPropertyName("gt")]
        public int GraphicTile { get; set; }
        [JsonPropertyName("gf")]
        public int GraphicFile { get; set; }
        [JsonPropertyName("r")]
        public int GraphicR { get; set; }
        [JsonPropertyName("g")]
        public int GraphicG { get; set; }
        [JsonPropertyName("b")]
        public int GraphicB { get; set; }
        [JsonPropertyName("a")]
        public int GraphicA { get; set; }

        [JsonPropertyName("wdmg")]
        public int WeaponDamage { get; set; }

        [JsonIgnore]
        public int TotalWeaponDamage { get; set; }

        [DefaultValue(3)]
        public int BodyState { get; set; } = 3;
        [JsonPropertyName("stats")]
        public AttributeSet BaseStats { get; set; }

        [JsonIgnore]
        public AttributeSet TotalStats { get; set; }

        [DefaultValue(1.0)]
        public double StatMultiplier { get; set; }

        public long Value { get; set; }

        public bool IsBound { get; set; }

        [JsonPropertyName("props")]
        public Dictionary<ItemProperty, object> ItemProperties { get; set; }

        /**
         * These properties are read only and just pass along from the templates properties
         *
         */
        [JsonIgnore]
        public int WeaponDelay { get => this.Template.WeaponDelay; }
        [JsonIgnore]
        public int StackSize { get => this.Template.StackSize; }
        [JsonIgnore]
        public bool IsLore { get => this.Template.IsLore; }
        [JsonIgnore]
        public bool IsBindOnPickup { get => this.Template.IsBindOnPickup; }
        [JsonIgnore]
        public bool IsBindOnEquip { get => this.Template.IsBindOnEquip; }
        [JsonIgnore]
        public bool IsEvent { get => this.Template.IsEvent; }
        [JsonIgnore]
        public ItemTemplate.ItemSlots Slot { get => this.Template.Slot; }
        [JsonIgnore]
        public ItemTemplate.ItemTypes Type { get => this.Template.Type; }
        [JsonIgnore]
        public ItemTemplate.UseTypes UseType { get => this.Template.UseType; }
        [JsonIgnore]
        public int MinLevel { get => this.Template.MinLevel; }
        [JsonIgnore]
        public int MaxLevel { get => this.Template.MaxLevel; }
        [JsonIgnore]
        public long MinExperience { get => this.Template.MinExperience; }
        [JsonIgnore]
        public long MaxExperience { get => this.Template.MaxExperience; }
        [JsonIgnore]
        public int Flags { get => this.Template.Flags; }
        [JsonIgnore]
        public int BodyType { get => this.Template.BodyType; }
        /**
         * This is a bitmask
         * Therefore only limited to about 64 classes, which should be enough.
         * If the bit is set then that class id CAN'T use the item.
         *
         */
        [JsonIgnore]
        public long ClassRestrictions { get => this.Template.ClassRestrictions; }
        [JsonIgnore]
        public SpellEffect? SpellEffect { get => this.Template.SpellEffect; }
        [JsonIgnore]
        public decimal SpellEffectChance { get => this.Template.SpellEffectChance; }
        [JsonIgnore]
        public int LearnSpellID { get => this.Template.LearnSpellID; }
        [JsonIgnore]
        public int Credits { get => this.Template.Credits; }

        [JsonIgnore]
        public bool Custom { get => this.Description?.StartsWith("Custom created by ") ?? false; }

        [JsonIgnore]
        public Script<IItemScript>? Script { get => this.Template.Script; }

        [DefaultValue("")]
        public string ScriptParams { get; set; } = "";

        public Item()
        {
            this.ItemID = 0;
            this.TotalStats = new AttributeSet();
            this.BaseStats = new AttributeSet();
            this.StatMultiplier = 1;
            this.ItemProperties = [];
        }

        /**
         * LoadFromTemplate, loads item from a template
         *
         * This is when we want an item the same as the template.
         *
         */
        public bool LoadFromTemplate(ItemTemplate template)
        {
            if (!ItemHandler.Validate(template)) return false;

            this.Template = template;
            this.TemplateID = this.Template.ID;
            this.TotalStats += this.Template.BaseStats;

            this.TotalWeaponDamage = this.Template.WeaponDamage;

            this.Name = this.Template.Name;
            this.Description = this.Template.Description;
            this.GraphicEquipped = this.Template.GraphicEquipped;
            this.GraphicTile = this.Template.GraphicTile;
            this.GraphicFile = this.Template.GraphicFile;
            this.GraphicR = this.Template.GraphicR;
            this.GraphicG = this.Template.GraphicG;
            this.GraphicB = this.Template.GraphicB;
            this.GraphicA = this.Template.GraphicA;

            this.Value = this.Template.Value;
            this.BodyState = this.Template.BodyState;

            this.ScriptParams = this.Template.ScriptParams;

            return true;
        }

        /**
         * CloneWithoutId, copies every per item value onto a new Item
         *
         * Used when a stack is split, where the new slot must be the same item rather
         * than a fresh one built from the template. Rebuilding from the template loses
         * everything that made this specific item what it is - most importantly IsBound,
         * which let a player launder a bound item into a freely tradeable one by
         * splitting it, but also rolled stats, description and title/surname properties.
         *
         * ItemID is deliberately not copied. The caller must assign a new one via
         * ItemHandler.AddAndAssignId so the two stacks remain distinct items.
         *
         */
        public Item CloneWithoutId()
        {
            var copy = new Item();

            copy.Template = this.Template;
            copy.TemplateID = this.TemplateID;

            copy.Name = this.Name;
            copy.Description = this.Description;

            copy.GraphicEquipped = this.GraphicEquipped;
            copy.GraphicTile = this.GraphicTile;
            copy.GraphicFile = this.GraphicFile;
            copy.GraphicR = this.GraphicR;
            copy.GraphicG = this.GraphicG;
            copy.GraphicB = this.GraphicB;
            copy.GraphicA = this.GraphicA;

            copy.WeaponDamage = this.WeaponDamage;
            copy.TotalWeaponDamage = this.TotalWeaponDamage;
            copy.BodyState = this.BodyState;

            copy.BaseStats = new AttributeSet() + this.BaseStats;
            copy.TotalStats = new AttributeSet() + this.TotalStats;
            copy.StatMultiplier = this.StatMultiplier;

            copy.Value = this.Value;
            copy.IsBound = this.IsBound;
            copy.ScriptParams = this.ScriptParams;

            copy.ItemProperties = new Dictionary<ItemProperty, object>(this.ItemProperties);

            return copy;
        }

        public T GetProperty<T>(ItemProperty prop)
        {
            if (!this.ItemProperties.TryGetValue(prop, out object? value) || value is null)
                return default!;

            if (value is T typed)
                return typed;

            // System.Text.Json deserializes Dictionary<..., object> values as JsonElement.
            if (value is JsonElement element)
                return element.Deserialize<T>()!;

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public bool HasProperty(ItemProperty prop)
        {
            return this.ItemProperties.ContainsKey(prop);
        }

        public void RefreshStats()
        {
            var newTotalStats = new AttributeSet();
            newTotalStats += this.Template.BaseStats;
            newTotalStats += this.BaseStats;
            newTotalStats *= this.StatMultiplier;

            this.TotalStats = newTotalStats;

            this.TotalWeaponDamage = (int)((this.Template.WeaponDamage + this.WeaponDamage) * this.StatMultiplier);
        }
    }
}
