using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Goose.Scripting;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Goose
{
    /**
     * Item, holds the actual item data
     * 
     * Each item in game is seperate
     * Holds the original template and modified/added stats
     * 
     */
    public class Item : IItem
    {
        [JsonProperty(PropertyName = "id")]
        public int ItemID { get; set; }
        [JsonProperty(PropertyName = "tid")]
        public int TemplateID { get; set; }
        [JsonIgnore]
        public ItemTemplate Template { get; set; }

        public string Name { get; set; }
        [JsonProperty(PropertyName = "desc")]
        [DefaultValue("")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "ge")]
        public int GraphicEquipped { get; set; }
        [JsonProperty(PropertyName = "gt")]
        public int GraphicTile { get; set; }
        [JsonProperty(PropertyName = "gf")]
        public int GraphicFile { get; set; }
        [JsonProperty(PropertyName = "r")]
        public int GraphicR { get; set; }
        [JsonProperty(PropertyName = "g")]
        public int GraphicG { get; set; }
        [JsonProperty(PropertyName = "b")]
        public int GraphicB { get; set; }
        [JsonProperty(PropertyName = "a")]
        public int GraphicA { get; set; }

        [JsonProperty(PropertyName = "wdmg")]
        public int WeaponDamage { get; set; }

        [DefaultValue(3)]
        public int BodyState { get; set; }
        [JsonProperty(PropertyName = "stats")]
        public AttributeSet BaseStats { get; set; }

        [JsonIgnore]
        public AttributeSet TotalStats { get; set; }

        [DefaultValue(1.0)]
        public double StatMultiplier { get; set; }

        public long Value { get; set; }

        public bool IsBound { get; set; }

        /**
         * These properties are read only and just pass along from the templates properties
         * 
         */
        [JsonIgnore]
        public int WeaponDelay { get { return this.Template.WeaponDelay; } }
        [JsonIgnore]
        public int StackSize { get { return this.Template.StackSize; } }
        [JsonIgnore]
        public bool IsLore { get { return this.Template.IsLore; } }
        [JsonIgnore]
        public bool IsBindOnPickup { get { return this.Template.IsBindOnPickup; } }
        [JsonIgnore]
        public bool IsBindOnEquip { get { return this.Template.IsBindOnEquip; } }
        [JsonIgnore]
        public bool IsEvent { get { return this.Template.IsEvent; } }
        [JsonIgnore]
        public ItemTemplate.ItemSlots Slot { get { return this.Template.Slot; } }
        [JsonIgnore]
        public ItemTemplate.ItemTypes Type { get { return this.Template.Type; } }
        [JsonIgnore]
        public ItemTemplate.UseTypes UseType { get { return this.Template.UseType; } }
        [JsonIgnore]
        public int MinLevel { get { return this.Template.MinLevel; } }
        [JsonIgnore]
        public int MaxLevel { get { return this.Template.MaxLevel; } }
        [JsonIgnore]
        public long MinExperience { get { return this.Template.MinExperience; } }
        [JsonIgnore]
        public long MaxExperience { get { return this.Template.MaxExperience; } }
        [JsonIgnore]
        public int Flags { get { return this.Template.Flags; } }
        [JsonIgnore]
        public int BodyType { get { return this.Template.BodyType; } }
        /**
         * This is a bitmask
         * Therefore only limited to about 64 classes, which should be enough.
         * If the bit is set then that class id CAN'T use the item.
         * 
         */
        [JsonIgnore]
        public long ClassRestrictions { get { return this.Template.ClassRestrictions; } }
        [JsonIgnore]
        public SpellEffect SpellEffect { get { return this.Template.SpellEffect; } }
        [JsonIgnore]
        public decimal SpellEffectChance { get { return this.Template.SpellEffectChance; } }
        [JsonIgnore]
        public int LearnSpellID { get { return this.Template.LearnSpellID; } }
        [JsonIgnore]

        public int Credits { get { return this.Template.Credits; } }

        [JsonIgnore]
        public bool Custom { get { return false; } }

        [JsonIgnore]
        public Script<IItemScript> Script { get { return this.Template.Script; } }

        [DefaultValue("")]
        public string ScriptParams { get; set; }

        public Item()
        {
            this.ItemID = 0;
            this.TotalStats = new AttributeSet();
            this.BaseStats = new AttributeSet();
            this.StatMultiplier = 1;
        }

        /**
         * LoadFromTemplate, loads item from a template
         * 
         * This is when we want an item the same as the template.
         * 
         */
        public void LoadFromTemplate(ItemTemplate template)
        {
            this.Template = template;
            this.TemplateID = this.Template.ID;
            this.TotalStats += this.Template.BaseStats;

            this.Name = this.Template.Name;
            this.Description = this.Template.Description;
            this.GraphicEquipped = this.Template.GraphicEquipped;
            this.GraphicTile = this.Template.GraphicTile;
            this.GraphicFile = this.Template.GraphicFile;
            this.GraphicR = this.Template.GraphicR;
            this.GraphicG = this.Template.GraphicG;
            this.GraphicB = this.Template.GraphicB;
            this.GraphicA = this.Template.GraphicA;

            this.WeaponDamage = this.Template.WeaponDamage;

            this.Value = this.Template.Value;
            this.BodyState = this.Template.BodyState;

            this.ScriptParams = this.Template.ScriptParams;
        }

        /**
         * LoadTemplate, adds template to item
         * 
         * This is when we want to just add the templates stats to our item
         * ie when loading the items database, eg for surname/titled items
         * 
         * Note: Doesn't load the value from the template as we want to keep the value as 0
         * if the item is custom for example
         * 
         */
        public void LoadTemplate(ItemTemplate template)
        {
            // Temporary measure for transition to new code
            if (this.BaseStats == null) this.BaseStats = new AttributeSet();

            this.TotalStats += template.BaseStats;
            this.TotalStats *= this.StatMultiplier;
            this.TotalStats += this.BaseStats;

            // Mainly here as a temporary measure for transition to new code
            this.WeaponDamage = (int)(this.Template.WeaponDamage * this.StatMultiplier);
        }

        public string GetSlotPacket(GameWorld world, int slotId, long stack)
        {
            var spellEffect = this.SpellEffect;
            int spellEffectChance = (int)this.SpellEffectChance;
            if (this.LearnSpellID != 0)
            {
                var spell = world.SpellHandler.GetSpell(this.LearnSpellID);
                spellEffect = spell?.SpellEffect;
                spellEffectChance = 100;
            }

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
                    this.TotalStats.AC + "|" +
                    this.TotalStats.HP + "|" +
                    this.TotalStats.MP + "|" +
                    this.TotalStats.SP + "|" +
                    this.TotalStats.Strength + "|" +
                    this.TotalStats.Stamina + "|" +
                    this.TotalStats.Intelligence + "|" +
                    this.TotalStats.Dexterity + "|" +
                    this.TotalStats.FireResist + "|" +
                    this.TotalStats.WaterResist + "|" +
                    this.TotalStats.EarthResist + "|" +
                    this.TotalStats.AirResist + "|" +
                    this.TotalStats.SpiritResist + "|" +
                    this.MinLevel + "|" +
                    this.MaxLevel + "|" +
                    ItemTemplate.FigureClassRestrictions(world, this.ClassRestrictions) +
                    "0" + "|" + // gm access
                    "0" + "|" + // gender, always 0 since we don't care about gender
                    (spellEffect == null ? "" : spellEffect.Name + ';' + string.Join(";", spellEffect.GetItemDescription(world))) + "|" +
                    spellEffectChance + "|" +
                    this.BodyType + "|" +
                    (int)this.UseType + "|" +
                    0 + "|" + // not sure
                    this.GraphicR + "|" +
                    this.GraphicG + "|" +
                    this.GraphicB + "|" +
                    this.GraphicA;
        }
    }
}
