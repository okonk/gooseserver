using System.Collections;
using System.Text;

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
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private Dictionary<int, ItemTemplate> templates;
        private Dictionary<int, Item> items;
        private Dictionary<int, ItemModifier> titles;
        private Dictionary<int, ItemModifier> surnames;

        private int currentid = 5002;

        public ItemHandler()
        {
            this.templates = [];
            this.items = [];
            this.titles = [];
            this.surnames = [];
        }

        /// <summary>
        /// Gets/sets the next available item id
        /// </summary>
        public int CurrentID
        {
            get => this.currentid;
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
            world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM item_templates";
                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int templateId = reader.GetInt32("item_template_id");
                    ItemTemplate template = this.GetTemplate(templateId) ?? new ItemTemplate();

                    template.ID = templateId;
                    template.Type = (ItemTemplate.ItemTypes)reader.GetInt32("item_type");
                    template.Slot = (ItemTemplate.ItemSlots)reader.GetInt32("item_slot");
                    template.UseType = (ItemTemplate.UseTypes)reader.GetInt32("item_usetype");
                    template.Name = reader.GetString("item_name");
                    template.Description = reader.GetString("item_description");

                    template.BaseStats = new AttributeSet();
                    template.BaseStats.HP = reader.GetInt64("player_hp");
                    template.BaseStats.MP = reader.GetInt64("player_mp");
                    template.BaseStats.SP = reader.GetInt64("player_sp");
                    template.BaseStats.AC = reader.GetInt32("stat_ac");
                    template.BaseStats.Strength = reader.GetInt32("stat_str");
                    template.BaseStats.Stamina = reader.GetInt32("stat_sta");
                    template.BaseStats.Intelligence = reader.GetInt32("stat_int");
                    template.BaseStats.Dexterity = reader.GetInt32("stat_dex");
                    template.BaseStats.FireResist = reader.GetInt32("res_fire");
                    template.BaseStats.AirResist = reader.GetInt32("res_air");
                    template.BaseStats.EarthResist = reader.GetInt32("res_earth");
                    template.BaseStats.SpiritResist = reader.GetInt32("res_spirit");
                    template.BaseStats.WaterResist = reader.GetInt32("res_water");

                    template.MinLevel = reader.GetInt32("min_level");
                    template.MaxLevel = reader.GetInt32("max_level");
                    template.MinExperience = reader.GetInt64("min_experience");
                    template.MaxExperience = reader.GetInt64("max_experience");

                    template.WeaponDamage = reader.GetInt32("weapon_damage");
                    template.WeaponDelay = reader.GetInt32("weapon_delay");
                    template.Value = reader.GetInt64("item_value");
                    template.GraphicTile = reader.GetInt32("graphic_tile");
                    template.GraphicFile = reader.GetInt32("graphic_file");
                    template.GraphicEquipped = reader.GetInt32("graphic_equip");
                    template.GraphicR = reader.GetInt32("graphic_r");
                    template.GraphicG = reader.GetInt32("graphic_g");
                    template.GraphicB = reader.GetInt32("graphic_b");
                    template.GraphicA = reader.GetInt32("graphic_a");
                    template.ClassRestrictions = reader.GetInt64("class_restrictions");

                    template.IsLore = reader.GetString("lore") != "0";
                    template.IsBindOnPickup = reader.GetString("bindonpickup") != "0";
                    template.IsBindOnEquip = reader.GetString("bindonequip") != "0";
                    template.IsEvent = reader.GetString("event") != "0";

                    template.StackSize = reader.GetInt32("stack_size");
                    template.BodyState = reader.GetInt32("body_state");

                    template.SpellEffectID = reader.GetInt32("spell_effect_id");
                    template.SpellEffect = world.SpellHandler.GetSpellEffect(template.SpellEffectID);
                    if (template.SpellEffectID != 0 && template.SpellEffect is null)
                    {
                        // log bad spell effect on item
                        continue;
                    }
                    template.SpellEffectChance = Decimal.Parse(reader.GetString("spell_effect_chance"));
                    template.LearnSpellID = reader.GetInt32("learn_spell_id");

                    template.Credits = reader.GetInt32("credits_value");

                    string scriptPath = reader.GetString("script_path");
                    if (!string.IsNullOrEmpty(scriptPath))
                    {
                        template.Script = world.ScriptHandler.GetScript<IItemScript>(scriptPath);
                    }

                    template.ScriptParams = reader.GetString("script_params");

                    this.templates[template.ID] = template;
                }
            });
        }

        public int LoadTitles(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM item_titles";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var title = ItemModifier.FromReader(reader, world, this.titles);
                    this.titles[title.Id] = title;
                }
            });

            return this.titles.Count;
        }

        public int LoadSurnames(GameWorld world)
        {
            world.Database.Execute(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT * FROM item_surnames";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var surname = ItemModifier.FromReader(reader, world, this.surnames);
                    this.surnames[surname.Id] = surname;
                }
            });

            return this.surnames.Count;
        }

        /**
         * TemplateCount, returns item template count
         * 
         */
        public int TemplateCount { get => this.templates.Count; }

        public int TitleCount { get => this.titles.Count; }
        public int SurnameCount { get => this.surnames.Count; }

        /**
         * GetTemplate, returns template by id
         */
        public ItemTemplate GetTemplate(int id)
        {
            if (this.templates.TryGetValue(id, out ItemTemplate template))
                return template;

            return null;
        }

        /// <summary>Registers a generated template. Mirrors NPCHandler.AddTemplate
        /// (NPCHandler.cs:231) and SpellHandler.AddSpell. Overwrites silently, so callers
        /// generating ids must check GetTemplate first.</summary>
        public void AddTemplate(ItemTemplate template)
        {
            this.templates[template.ID] = template;
        }

        /// <summary>Registers a generated title. A modifier with Chance 0 can never be
        /// selected by RollModifier (its range is empty), so script-owned modifiers
        /// register at 0 and are applied explicitly.</summary>
        public void AddTitle(ItemModifier title)
        {
            this.titles[title.Id] = title;
        }

        public void AddSurname(ItemModifier surname)
        {
            this.surnames[surname.Id] = surname;
        }

        public ItemModifier GetTitle(int id)
        {
            return this.titles.TryGetValue(id, out ItemModifier title) ? title : null;
        }

        public ItemModifier GetSurname(int id)
        {
            return this.surnames.TryGetValue(id, out ItemModifier surname) ? surname : null;
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
        public Item GetGold(GameWorld world)
        {
            return this.items[world.Settings.ItemIDStartpoint + world.Settings.GoldItemID];
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

        /// <summary>Returns an item to template state: no title, no surname, no modifier
        /// stats, no modifier weapon damage. Safe to call repeatedly.
        ///
        /// Every field a modifier can write has to be listed here. ItemModifier.ApplyStats
        /// runs through ItemModifierScript.csx's AddStats (`:60-80`), which writes
        /// StatMultiplier, WeaponDamage and BaseStats — WeaponDamage included, and
        /// RefreshStats folds it into TotalWeaponDamage (`Item.cs:256`). Forget it and
        /// repeated paid rerolls stack weapon damage without bound.
        ///
        /// Deliberately not built on Item.LoadFromTemplate, which accumulates rather than
        /// assigns (TotalStats += template.BaseStats, Item.cs:159) and would double-count
        /// the template's stats on a second call.</summary>
        public void ResetModifiers(Item item)
        {
            item.Name = item.Template.Name;
            item.BaseStats = new AttributeSet();
            item.WeaponDamage = 0;
            item.StatMultiplier = 1;
            item.ItemProperties.Remove(ItemProperty.TitleId);
            item.ItemProperties.Remove(ItemProperty.SurnameId);
            item.RefreshStats();
        }

        public void RollTitleAndSurname(Item item, GameWorld world)
        {
            // Above the use-type filter deliberately: a script-owned item (dimension tomes)
            // must be able to claim the roll even when nothing native would apply to it.
            if (item.Script is not null)
            {
                try
                {
                    if (item.Script.Object.OnRollModifiersEvent(item, world)) return;
                }
                catch (Exception e)
                {
                    log.Error(e, "Exception in OnRollModifiersEvent for template {templateId}", item.TemplateID);
                }
            }

            if (item.UseType != ItemTemplate.UseTypes.Armor && item.UseType != ItemTemplate.UseTypes.Weapon)
                return;

            if (world.RollChance(world.Settings.ItemSurnameChancePercent))
            {
                var surname = RollModifier(item, surnames.Values, world);
                if (surname is not null)
                {
                    item.Name = $"{item.Name} {surname.Name}";
                    item.ItemProperties[ItemProperty.SurnameId] = surname.Id;
                    surname.ApplyStats(item, world);
                }
            }

            if (world.RollChance(world.Settings.ItemTitleChancePercent))
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
