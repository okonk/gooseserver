using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

using Goose.Events;
using Goose.Scripting;

namespace Goose
{
    /**
     * ItemHandler, handles item templates/items
     * 
     */
    public class ItemHandler
    {
        private Dictionary<int, ItemTemplate> templates;
        private Dictionary<int, Item> items;
        private Dictionary<int, ItemModifier> titles;
        private Dictionary<int, ItemModifier> surnames;

        private int currentid = 5002;

        public ItemHandler()
        {
            this.templates = new Dictionary<int, ItemTemplate>();
            this.items = new Dictionary<int, Item>();
            this.titles = new Dictionary<int, ItemModifier>();
            this.surnames = new Dictionary<int, ItemModifier>();
        }

        /// <summary>
        /// Gets/sets the next available item id
        /// </summary>
        public int CurrentID
        {
            get { return this.currentid; }
            set { this.currentid = value; }
        }

        public IEnumerable<ItemTemplate> GetTemplates()
        {
            return templates.Values;
        }

        public IEnumerable<Item> GetItems()
        {
            return items.Values;
        }

        /**
         * LoadTemplates, loads item templates
         * 
         */
        public void LoadTemplates(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM item_templates";
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                int templateId = Convert.ToInt32(reader["item_template_id"]);
                ItemTemplate template = this.GetTemplate(templateId) ?? new ItemTemplate();

                template.ID = templateId;
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

                string scriptPath = Convert.ToString(reader["script_path"]);
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    template.Script = world.ScriptHandler.GetScript<IItemScript>(scriptPath);
                }

                template.ScriptParams = Convert.ToString(reader["script_params"]);

                this.templates[template.ID] = template;
            }

            reader.Close();
        }

        public int LoadTitles(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM item_titles";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var title = ItemModifier.FromReader(reader, world, this.titles);
                    this.titles[title.Id] = title;
                }
            }

            return this.titles.Count;
        }

        public int LoadSurnames(GameWorld world)
        {
            var command = world.SqlConnection.CreateCommand();
            command.CommandText = "SELECT * FROM item_surnames";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var surname = ItemModifier.FromReader(reader, world, this.surnames);
                    this.surnames[surname.Id] = surname;
                }
            }

            return this.surnames.Count;
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
            if (this.templates.TryGetValue(id, out ItemTemplate template))
                return template;

            return null;
        }

        public void AddAndAssignId(Item item, GameWorld world)
        {
            item.ItemID = this.CurrentID;
            this.CurrentID++;

            try
            {
                item.Script?.Object.OnCreateEvent(item, world);
            }
            catch (Exception e) { }

            this.items[item.ItemID] = item;
        }

        public void AddItem(Item item, GameWorld world)
        {
            if (item.ItemID >= this.CurrentID)
            {
                this.CurrentID = item.ItemID + 1;
            }

            this.items[item.ItemID] = item;
        }

        /**
         * GetGold, returns item for gold
         * 
         */
        public Item GetGold()
        {
            return this.items[GameWorld.Settings.ItemIDStartpoint + GameWorld.Settings.GoldItemID];
        }

        /// <summary>
        /// Called after reloading item templates to update stats
        /// </summary>
        /// <param name="world"></param>
        public void RefreshItemStats(GameWorld world)
        {
            foreach (var item in GetItems())
            {
                item.RefreshStats();
            }
        }

        public void RollTitleAndSurname(Item item, GameWorld world)
        {
            if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
                return;

            if (world.RollChance(GameWorld.Settings.ItemSurnameChancePercent))
            {
                var surname = RollModifier(item, surnames.Values, world);
                if (surname is not null)
                {
                    item.Name = $"{item.Name} {surname.Name}";
                    item.ItemProperties[ItemProperty.SurnameId] = surname.Id;
                    surname.ApplyStats(item, world);
                }
            }

            if (world.RollChance(GameWorld.Settings.ItemTitleChancePercent))
            {
                var title = RollModifier(item, titles.Values, world);
                if (title is not null)
                {
                    item.Name = $"{title.Name} {item.Name}";
                    item.ItemProperties[ItemProperty.TitleId] = title.Id;
                    title.ApplyStats(item, world);
                }
            }
        }

        private ItemModifier RollModifier(Item item, IReadOnlyCollection<ItemModifier> allModifiers, GameWorld world)
        {
            var modifiersWithRanges = new List<(ItemModifier Modifier, int StartRange, int EndRange)>();

            var nextStart = 0;
            foreach (var modifier in allModifiers)
            {
                if (!modifier.ModifierAppliesToItem(item, world))
                    continue;

                var currentLength = (int)(modifier.Chance * 100);
                var currentEnd = nextStart + currentLength - 1;
                modifiersWithRanges.Add((modifier, nextStart, currentEnd));

                nextStart = currentEnd + 1;
            }

            var number = world.Random.Next(0, nextStart);
            foreach (var (modifier, startRange, endRange) in modifiersWithRanges)
            {
                if (number >= startRange && number <= endRange)
                    return modifier;
            }

            return null;
        }
    }
}
