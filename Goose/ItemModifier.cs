using System;
using System.Collections.Generic;
using System.Data.Common;
using Goose.Scripting;

namespace Goose
{
    public class ItemModifier
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public int Id { get; set; }
        public string Name { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public long MinExperience { get; set; }
        public long MaxExperience { get; set; }
        public ItemTemplate.UseTypes UseType { get; set; }
        public ItemTemplate.ItemSlots Slot { get; set; }
        public double Chance { get; set; }
        public Script<IItemModifierScript> Script { get; set; }
        public string ScriptParams { get; set; }

        public static ItemModifier FromReader(DbDataReader reader, GameWorld world, Dictionary<int, ItemModifier> modifiers)
        {
            int id = Convert.ToInt32(reader["id"]);

            ItemModifier modifier = null;
            if (!modifiers.TryGetValue(id, out modifier))
                modifier = new ItemModifier();

            modifier.Id = id;
            modifier.Name = Convert.ToString(reader["name"]);
            modifier.MinLevel = Convert.ToInt32(reader["min_level"]);
            modifier.MaxLevel = Convert.ToInt32(reader["max_level"]);
            modifier.MinExperience = Convert.ToInt64(reader["min_experience"]);
            modifier.MaxExperience = Convert.ToInt64(reader["max_experience"]);
            modifier.UseType = (ItemTemplate.UseTypes)Convert.ToInt32(reader["item_usetype"]);
            modifier.Slot = (ItemTemplate.ItemSlots)Convert.ToInt32(reader["item_slot"]);
            modifier.Chance = Convert.ToDouble(reader["chance"]);

            string scriptPath = Convert.ToString(reader["script_path"]);
            if (!string.IsNullOrEmpty(scriptPath))
            {
                modifier.Script = world.ScriptHandler.GetScript<IItemModifierScript>(scriptPath);
            }

            modifier.ScriptParams = Convert.ToString(reader["script_params"]);

            return modifier;
        }

        public bool RollChance(Item item, GameWorld world)
        {
            if ((item.MinLevel > 0 && item.MinLevel < this.MinLevel) || (item.MaxLevel > 0 && item.MaxLevel > this.MaxLevel))
                return false;

            if (item.MinExperience < this.MinExperience || item.MaxExperience > this.MaxExperience)
                return false;

            if ((this.UseType == ItemTemplate.UseTypes.Armor || this.UseType == ItemTemplate.UseTypes.Weapon) && item.UseType != this.UseType)
                return false;

            if (this.Slot != ItemTemplate.ItemSlots.Misc && item.Slot != this.Slot)
                return false;

            return world.RollChance(this.Chance);
        }

        public void ApplyStats(Item item, GameWorld world)
        {
            try
            {
                this.Script?.Object?.OnExecuteEvent(this, item, world);
            }
            catch (Exception e)
            {
                log.Error(e, "Exception applying stats to item. Modifier: {modifierName} ({modifierId})", this.Name, this.Id);
            }
        }
    }
}