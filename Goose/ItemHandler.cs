using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

using Goose.Events;

namespace Goose
{
    /**
     * ItemHandler, handles item templates/items
     * 
     */
    public class ItemHandler
    {
        Hashtable templates;
        Hashtable items;
        List<Item> newitems;

        int currentid = 5002;


        public ItemHandler()
        {
            this.templates = new Hashtable();
            this.items = new Hashtable();
            this.newitems = new List<Item>();
        }

        /// <summary>
        /// Gets/sets the next available item id
        /// </summary>
        public int CurrentID
        {
            get { return this.currentid; }
            set { this.currentid = value; }
        }
        
        /**
         * LoadTemplates, loads item templates
         * 
         */
        public void LoadTemplates(GameWorld world)
        {
            SqlCommand command = new SqlCommand("SELECT * FROM item_templates", world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                ItemTemplate template = new ItemTemplate();

                template.ID = Convert.ToInt32(reader["item_template_id"]);
                template.Type = (ItemTemplate.ItemTypes)Convert.ToInt32(reader["item_type"]);
                template.Slot = (ItemTemplate.ItemSlots)Convert.ToInt32(reader["item_slot"]);
                template.UseType = (ItemTemplate.UseTypes)Convert.ToInt32(reader["item_usetype"]);
                template.Name = Convert.ToString(reader["item_name"]);
                template.Description = Convert.ToString(reader["item_description"]);

                template.BaseStats = new AttributeSet();
                template.BaseStats.HP = Convert.ToInt32(reader["player_hp"]);
                template.BaseStats.MP = Convert.ToInt32(reader["player_mp"]);
                template.BaseStats.SP = Convert.ToInt32(reader["player_sp"]);
                template.BaseStats.AC = Convert.ToInt32(reader["stat_ac"]);
                template.BaseStats.Strength = Convert.ToInt32(reader["stat_str"]);
                template.BaseStats.Stamina = Convert.ToInt32(reader["stat_sta"]);
                template.BaseStats.Intelligence = Convert.ToInt32(reader["stat_int"]);
                template.BaseStats.Dexterity = Convert.ToInt32(reader["stat_dex"]);
                template.BaseStats.FireResist = Convert.ToInt32(reader["res_fire"]);
                template.BaseStats.AirResist = Convert.ToInt32(reader["res_air"]);
                template.BaseStats.EarthResist = Convert.ToInt32(reader["res_earth"]);
                template.BaseStats.SpiritResist = Convert.ToInt32(reader["res_spirit"]);
                template.BaseStats.WaterResist = Convert.ToInt32(reader["res_water"]);

                template.MinLevel = Convert.ToInt32(reader["min_level"]);
                template.MaxLevel = Convert.ToInt32(reader["max_level"]);
                template.MinExperience = Convert.ToInt64(reader["min_experience"]);
                template.MaxExperience = Convert.ToInt64(reader["max_experience"]);

                template.WeaponDamage = Convert.ToInt32(reader["weapon_damage"]);
                template.WeaponDelay = Convert.ToInt32(reader["weapon_delay"]);
                template.Value = Convert.ToInt64(reader["item_value"]);
                template.GraphicTile = Convert.ToInt32(reader["graphic_tile"]);
                template.GraphicFile = Convert.ToInt32(reader["graphic_file"]);
                template.GraphicEquipped = Convert.ToInt32(reader["graphic_equip"]);
                template.GraphicR = Convert.ToInt32(reader["graphic_r"]);
                template.GraphicG = Convert.ToInt32(reader["graphic_g"]);
                template.GraphicB = Convert.ToInt32(reader["graphic_b"]);
                template.GraphicA = Convert.ToInt32(reader["graphic_a"]);
                template.ClassRestrictions = Convert.ToInt64(reader["class_restrictions"]);

                template.IsLore = ("0".Equals(Convert.ToString(reader["lore"])) ? false : true);
                template.IsBindOnPickup = ("0".Equals(Convert.ToString(reader["bindonpickup"])) ? false : true);
                template.IsBindOnEquip = ("0".Equals(Convert.ToString(reader["bindonequip"])) ? false : true);
                template.IsEvent = ("0".Equals(Convert.ToString(reader["event"])) ? false : true);

                template.StackSize = Convert.ToInt32(reader["stack_size"]);
                template.BodyState = Convert.ToInt32(reader["body_state"]);

                template.SpellEffectID = Convert.ToInt32(reader["spell_effect_id"]);
                template.SpellEffect = world.SpellHandler.GetSpellEffect(template.SpellEffectID);
                if (template.SpellEffectID != 0 && template.SpellEffect == null)
                {
                    // log bad spell effect on item
                    continue;
                }
                template.SpellEffectChance = Decimal.Parse(Convert.ToString(reader["spell_effect_chance"]));
                template.LearnSpellID = Convert.ToInt32(reader["learn_spell_id"]);

                template.Credits = Convert.ToInt32(reader["credits_value"]);

                this.templates[template.ID] = template;
            }

            reader.Close();
        }

        /**
         * TemplateCount, returns item template count
         * 
         */
        public int TemplateCount { get { return this.templates.Count; } }

        /**
         * GetTemplate, returns template by id
         */
        public ItemTemplate GetTemplate(int id)
        {
            return (ItemTemplate)this.templates[id];
        }

        /**
         * LoadItems, loads items
         * 
         */
        public void LoadItems(GameWorld world)
        {
            SqlCommand command = new SqlCommand("SELECT * FROM items", world.SqlConnection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Item item = new Item();
                item.ItemID = Convert.ToInt32(reader["item_id"]);
                item.TemplateID = Convert.ToInt32(reader["item_template_id"]);
                item.Template = this.GetTemplate(item.TemplateID);

                if (item.Template == null)
                {
                    // something went wrong, continue
                    // need to log later
                    continue;
                }

                item.Name = Convert.ToString(reader["item_name"]);
                item.Description = Convert.ToString(reader["item_description"]);

                item.BaseStats = new AttributeSet();
                item.BaseStats.HP = Convert.ToInt32(reader["player_hp"]);
                item.BaseStats.MP = Convert.ToInt32(reader["player_mp"]);
                item.BaseStats.SP = Convert.ToInt32(reader["player_sp"]);
                item.BaseStats.AC = Convert.ToInt32(reader["stat_ac"]);
                item.BaseStats.Strength = Convert.ToInt32(reader["stat_str"]);
                item.BaseStats.Stamina = Convert.ToInt32(reader["stat_sta"]);
                item.BaseStats.Intelligence = Convert.ToInt32(reader["stat_int"]);
                item.BaseStats.Dexterity = Convert.ToInt32(reader["stat_dex"]);
                item.BaseStats.FireResist = Convert.ToInt32(reader["res_fire"]);
                item.BaseStats.AirResist = Convert.ToInt32(reader["res_air"]);
                item.BaseStats.EarthResist = Convert.ToInt32(reader["res_earth"]);
                item.BaseStats.SpiritResist = Convert.ToInt32(reader["res_spirit"]);
                item.BaseStats.WaterResist = Convert.ToInt32(reader["res_water"]);

                item.WeaponDamage = Convert.ToInt32(reader["weapon_damage"]);
                item.Value = Convert.ToInt64(reader["item_value"]);
                item.GraphicTile = Convert.ToInt32(reader["graphic_tile"]);
                item.GraphicFile = Convert.ToInt32(reader["graphic_file"]);
                item.GraphicEquipped = Convert.ToInt32(reader["graphic_equip"]);
                item.GraphicR = Convert.ToInt32(reader["graphic_r"]);
                item.GraphicG = Convert.ToInt32(reader["graphic_g"]);
                item.GraphicB = Convert.ToInt32(reader["graphic_b"]);
                item.GraphicA = Convert.ToInt32(reader["graphic_a"]);

                item.BodyState = Convert.ToInt32(reader["body_state"]);

                item.StatMultiplier = Convert.ToDecimal(reader["stat_multiplier"]);

                item.LoadTemplate(item.Template);
                item.Dirty = false;

                item.IsBound = ("0".Equals(Convert.ToString(reader["bound"])) ? false : true);

                item.Unsaved = false;

                this.items[item.ItemID] = item;

                if (item.ItemID >= this.CurrentID)
                {
                    this.CurrentID = item.ItemID + 1;
                }
            }

            reader.Close();
        }

        /**
         * ItemCount, returns item count
         * 
         */
        public int ItemCount { get { return this.items.Count; } }

        /**
         * AddItem, adds an item to the handler
         * 
         * What it does is adds the item to the newitems list, which temporarily holds the item
         * until the ItemHandler is called to save the items to database. So once it has an ID it gets put
         * into the items hashtable.
         * 
         */
        public void AddItem(Item item)
        {
            item.ItemID = this.CurrentID;
            this.CurrentID++;

            this.newitems.Add(item);
        }

        /**
         * GetGold, returns item for gold
         * 
         */
        public Item GetGold()
        {
            return (Item)this.items[GameSettings.Default.ItemIDStartpoint + GameSettings.Default.GoldItemID];
        }

        /**
         * Save, saves items
         * 
         */
        public void Save(GameWorld world)
        {
            List<int> remove = new List<int>();

            foreach (Item item in this.items.Values)
            {
                if (item.Delete)
                {
                    item.DeleteItem(world);
                    remove.Add(item.ItemID);
                }
                else if (item.Dirty)
                {
                    item.SaveItem(world);
                }
            }

            foreach (int id in remove)
            {
                this.items.Remove(id);
            }

            foreach (Item item in this.newitems)
            {
                if (item.Delete) continue;
                if (item.Unsaved) item.AddItem(world);

                this.items[item.ItemID] = item;
            }

            this.newitems.Clear();

            this.AddSaveEvent(world);
        }

        /**
         * AddSaveEvent, adds save event to eventhandler
         * 
         */
        public void AddSaveEvent(GameWorld world)
        {
            ItemSaveEvent ev = new ItemSaveEvent();
            ev.Ticks += (long)(GameSettings.Default.ItemSavePeriod * world.TimerFrequency);

            world.EventHandler.AddEvent(ev);
        }

        /**
         * GetItem, returns item by id
         * 
         */
        public Item GetItem(int id)
        {
            Item item = null;
            item = (Item)this.items[id];

            if (item == null)
            {
                foreach (Item i in this.newitems)
                {
                    if (i.ItemID == id) return i;
                }
            }

            return item;
        }
    }
}
