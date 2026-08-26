using System.Data.Common;
using Goose.Scripting;

namespace Goose
{
    public class ItemModifier
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public long MinExperience { get; set; }
        public long MaxExperience { get; set; }
        public ItemTemplate.UseTypes UseType { get; set; }
        public ItemTemplate.ItemSlots Slot { get; set; }
        public double Chance { get; set; }
        public Script<IItemModifierScript>? Script { get; set; }
        public string ScriptParams { get; set; } = null!;

        public static ItemModifier FromReader(DbDataReader reader, GameWorld world, Dictionary<int, ItemModifier> modifiers)
        {
            int id = reader.GetInt32("id");

            ItemModifier modifier = null;
            if (!modifiers.TryGetValue(id, out modifier))
                modifier = new ItemModifier();

            modifier.Id = id;
            modifier.Name = reader.GetString("name");
            modifier.MinLevel = reader.GetInt32("min_level");
            modifier.MaxLevel = reader.GetInt32("max_level");
            modifier.MinExperience = reader.GetInt64("min_experience");
            modifier.MaxExperience = reader.GetInt64("max_experience");
            modifier.UseType = (ItemTemplate.UseTypes)reader.GetInt32("item_usetype");
            modifier.Slot = (ItemTemplate.ItemSlots)reader.GetInt32("item_slot");
            modifier.Chance = reader.GetDouble("chance");

            string scriptPath = reader.GetString("script_path");
            if (!string.IsNullOrEmpty(scriptPath))
            {
                modifier.Script = world.ScriptHandler.GetScript<IItemModifierScript>(scriptPath);
            }

            modifier.ScriptParams = reader.GetString("script_params");

            return modifier;
        }

        public bool ModifierAppliesToItem(Item item, GameWorld world)
        {
            if ((this.MinLevel > 0 && item.MinLevel < this.MinLevel) || (this.MaxLevel > 0 && item.MinLevel > this.MaxLevel))
                return false;

            if ((this.MinExperience > 0 && item.MinExperience < this.MinExperience) || (this.MaxExperience > 0 && item.MinExperience > this.MaxExperience))
                return false;

            if ((this.UseType == ItemTemplate.UseTypes.Armor || this.UseType == ItemTemplate.UseTypes.Weapon) && item.UseType != this.UseType)
                return false;

            if (this.Slot != ItemTemplate.ItemSlots.Misc && item.Slot != this.Slot)
                return false;

            return true;
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