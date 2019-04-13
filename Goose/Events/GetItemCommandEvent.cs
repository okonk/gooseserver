using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /getitem templateid stack
     * 
     */
    public class GetItemCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GetItemCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (!this.Player.HasPrivilege(AccessPrivilege.SpawnItem)) return;

                int id = 0;
                int stack = 1;
                bool powerful = false;

                string[] t = ((string)this.Data).Split(" ".ToCharArray(), 5);

                if (t.Length >= 2)
                {
                    try
                    {
                        id = Convert.ToInt32(t[1]);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                }
                if (t.Length >= 3)
                {
                    try
                    {
                        stack = Convert.ToInt32(t[2]);
                    }
                    catch (Exception)
                    {
                        stack = 1;

                        powerful = t[2].Equals("powerful", StringComparison.OrdinalIgnoreCase);
                    }
                }
                if (t.Length >= 4)
                {
                    powerful = t[3].Equals("powerful", StringComparison.OrdinalIgnoreCase);
                }

                if (id <= 0 || stack <= 0) return;

                ItemTemplate template = world.ItemHandler.GetTemplate(id);
                if (template == null) return;

                Item item = new Item();
                item.LoadFromTemplate(template);

                if (powerful && (item.Template.UseType == ItemTemplate.UseTypes.Armor || item.Template.UseType == ItemTemplate.UseTypes.Weapon))
                {
                    item.Name = "Powerful " + item.Template.Name;
                    item.StatMultiplier = 2;
                    item.TotalStats = item.Template.BaseStats;
                    item.TotalStats *= item.StatMultiplier;
                    item.TotalStats += item.BaseStats;
                }

                world.ItemHandler.AddItem(item, world);

                this.Player.Inventory.AddItem(item, stack, world);

                world.LogHandler.Log(Log.Types.GetItem,
                    this.Player.PlayerID, item.Name + " " + item.ItemID + " " + stack,
                    0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
