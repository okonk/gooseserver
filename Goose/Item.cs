using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

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
        public int ItemID { get; set; }
        public int TemplateID { get; set; }
        public ItemTemplate Template { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public int GraphicEquipped { get; set; }
        public int GraphicTile { get; set; }
        public int GraphicR { get; set; }
        public int GraphicG { get; set; }
        public int GraphicB { get; set; }
        public int GraphicA { get; set; }
        int weapondamage = 0;
        public int WeaponDamage 
        {
            get
            {
                return this.weapondamage + (int)Math.Ceiling(this.Template.WeaponDamage * this.StatMultiplier);
            }
            set
            {
                this.weapondamage = value;
            }
        }

        /**
         * Body pose/state 1 for normal, 3 for staff, 4 for sword
         */
        public int BodyState { get; set; }

        public AttributeSet BaseStats { get; set; }
        public AttributeSet TotalStats { get; set; }

        public decimal StatMultiplier { get; set; }
        /**
         * Dirty, has data changed since loading
         * 
         */
        public bool Dirty { get; set; }
        /**
         * Delete, item is no longer in game so delete it
         */
        public bool Delete { get; set; }
        public long Value { get; set; }

        bool bound = false;
        public bool IsBound
        {
            get { return this.bound; }
            set { this.bound = value; }
        }

        /**
         * These properties are read only and just pass along from the templates properties
         * 
         */
        public int WeaponDelay { get { return this.Template.WeaponDelay; } }
        public int StackSize { get { return this.Template.StackSize; } }
        public bool IsLore { get { return this.Template.IsLore; } }
        public bool IsBindOnPickup { get { return this.Template.IsBindOnPickup; } }
        public bool IsBindOnEquip { get { return this.Template.IsBindOnEquip; } }
        public bool IsEvent { get { return this.Template.IsEvent; } }
        public ItemTemplate.ItemSlots Slot { get { return this.Template.Slot; } }
        public ItemTemplate.ItemTypes Type { get { return this.Template.Type; } }
        public ItemTemplate.UseTypes UseType { get { return this.Template.UseType; } }
        public int MinLevel { get { return this.Template.MinLevel; } }
        public int MaxLevel { get { return this.Template.MaxLevel; } }
        public long MinExperience { get { return this.Template.MinExperience; } }
        public long MaxExperience { get { return this.Template.MaxExperience; } }
        /**
         * This is a bitmask
         * Therefore only limited to about 64 classes, which should be enough.
         * If the bit is set then that class id CAN'T use the item.
         * 
         */
        public long ClassRestrictions { get { return this.Template.ClassRestrictions; } }
        public SpellEffect SpellEffect { get { return this.Template.SpellEffect; } }
        public decimal SpellEffectChance { get { return this.Template.SpellEffectChance; } }
        public int LearnSpellID { get { return this.Template.LearnSpellID; } }

        public int Credits { get { return this.Template.Credits; } }

        public bool Unsaved { get; set; }

        public Item()
        {
            this.Unsaved = true;
            this.ItemID = 0;
            this.TotalStats = new AttributeSet();
            this.BaseStats = new AttributeSet();
            this.StatMultiplier = 1;
            this.Dirty = true;
            this.Delete = false;
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
            this.GraphicR = this.Template.GraphicR;
            this.GraphicG = this.Template.GraphicG;
            this.GraphicB = this.Template.GraphicB;
            this.GraphicA = this.Template.GraphicA;

            this.Value = this.Template.Value;
            this.BodyState = this.Template.BodyState;
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
            this.TotalStats += template.BaseStats;
            this.TotalStats *= this.StatMultiplier;
            this.TotalStats += this.BaseStats;
        }

        /**
         * AddItem, adds item to database
         * 
         */
        public void AddItem(GameWorld world)
        {
            SqlParameter nameParam = new SqlParameter("@itemName", SqlDbType.VarChar, 64);
            nameParam.Value = this.Name;
            SqlParameter descriptionParam = new SqlParameter("@itemDescription", SqlDbType.VarChar, 64);
            descriptionParam.Value = this.Description;

            string query = "INSERT INTO items (item_id, item_template_id, item_name, item_description, " +
            "player_hp, player_mp, player_sp, stat_ac, stat_str, stat_sta, stat_dex, stat_int, " +
            "res_fire, res_water, res_spirit, res_air, res_earth, weapon_damage, item_value, " +
            "graphic_tile, graphic_equip, graphic_r, graphic_g, graphic_b, graphic_a, stat_multiplier, " +
            "bound, body_state) VALUES (" +
            this.ItemID + "," +
            this.TemplateID + ", " +
            "@itemName, " +
            "@itemDescription, " +
            this.BaseStats.HP + ", " +
            this.BaseStats.MP + ", " +
            this.BaseStats.SP + ", " +
            this.BaseStats.AC + ", " +
            this.BaseStats.Strength + ", " +
            this.BaseStats.Stamina + ", " +
            this.BaseStats.Dexterity + ", " +
            this.BaseStats.Intelligence + ", " +
            this.BaseStats.FireResist + ", " +
            this.BaseStats.WaterResist + ", " +
            this.BaseStats.SpiritResist + ", " +
            this.BaseStats.AirResist + ", " +
            this.BaseStats.EarthResist + ", " +
            this.weapondamage + ", " +
            this.Value + ", " +
            this.GraphicTile + ", " +
            this.GraphicEquipped + ", " +
            this.GraphicR + ", " +
            this.GraphicG + ", " +
            this.GraphicB + ", " +
            this.GraphicA + ", " +
            this.StatMultiplier + ", " +
            (this.bound ? "'1'" : "'0'") + ", " +
            this.BodyState + ")";

            this.Dirty = false;

            this.Unsaved = false;

            SqlCommand command = new SqlCommand(query, world.SqlConnection);
            command.Parameters.Add(nameParam);
            command.Parameters.Add(descriptionParam);
            command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
        }

        /**
         * SaveItem, updates item info in database
         * 
         */
        public void SaveItem(GameWorld world)
        {
            SqlParameter nameParam = new SqlParameter("@itemName", SqlDbType.VarChar, 64);
            nameParam.Value = this.Name;
            SqlParameter descriptionParam = new SqlParameter("@itemDescription", SqlDbType.VarChar, 64);
            descriptionParam.Value = this.Description;

            string query = "UPDATE items SET " +
                "item_template_id=" + this.TemplateID + ", " +
                "item_name=" + "@itemName, " +
                "item_description=" + "@itemDescription, " + 
                "player_hp=" + this.BaseStats.HP + ", " +
                "player_mp=" + this.BaseStats.MP + ", " +
                "player_sp=" + this.BaseStats.SP + ", " +
                "stat_ac=" + this.BaseStats.AC + ", " +
                "stat_str=" + this.BaseStats.Strength + ", " +
                "stat_sta=" + this.BaseStats.Stamina + ", " +
                "stat_dex=" + this.BaseStats.Dexterity + ", " +
                "stat_int=" + this.BaseStats.Intelligence + ", " + 
                "res_fire=" + this.BaseStats.FireResist + ", " +
                "res_water=" + this.BaseStats.WaterResist + ", " +
                "res_spirit=" + this.BaseStats.SpiritResist + ", " +
                "res_air=" + this.BaseStats.AirResist + ", " +
                "res_earth=" + this.BaseStats.EarthResist + ", " +
                "weapon_damage=" + this.weapondamage + ", " +
                "item_value=" + this.Value + ", " + 
                "graphic_tile=" + this.GraphicTile + ", " +
                "graphic_equip=" + this.GraphicEquipped + ", " +
                "graphic_r=" + this.GraphicR + ", " +
                "graphic_g=" + this.GraphicG + ", " +
                "graphic_b=" + this.GraphicB + ", " +
                "graphic_a=" + this.GraphicA + ", " +
                "stat_multiplier=" + this.StatMultiplier + ", " +
                "bound=" + (this.bound ? "'1'" : "'0'") + ", " +
                "body_state=" + this.BodyState + 
                " WHERE item_id=" + this.ItemID;

            SqlCommand command = new SqlCommand(query, world.SqlConnection);
            command.Parameters.Add(nameParam);
            command.Parameters.Add(descriptionParam);
            command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
            this.Dirty = false;
        }

        /**
         * DeleteItem, deletes item from database
         * 
         */
        public void DeleteItem(GameWorld world)
        {
            SqlCommand command = new SqlCommand(
                "DELETE FROM items WHERE item_id=" + this.ItemID, 
                world.SqlConnection);
            command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
        }
    }
}
