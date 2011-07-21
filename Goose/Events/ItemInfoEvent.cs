using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * ItemInfoEvent
     * 
     * Packet: GIDitemid
     * 
     * Note: if itemid is lessthan GameSettings.Default.ItemIDStartpoint then it's a template id
     * else it's an item id
     * 
     */
    public class ItemInfoEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ItemInfoEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int itemid = 0;

                string data = ((string)this.Data).Substring(3);

                // log bad id
                if (data.Length <= 0) return;

                try
                {
                    itemid = Convert.ToInt32(data);
                }
                catch (Exception)
                {
                    itemid = 0;
                }

                // log bad item id
                if (itemid <= 0) return;

                if (itemid >= GameSettings.Default.ItemIDStartpoint)
                {
                    this.Player.Inventory.ShowStatsWindow(itemid, world);
                }
                else
                {
                    foreach (Window window in this.Player.Windows)
                    {
                        if (window.Type != Window.WindowTypes.Vendor) continue;

                        foreach (NPCVendorSlot slot in window.NPC.VendorItems)
                        {
                            if (slot == null) continue;

                            if (slot.ItemTemplate.ID == itemid)
                            {
                                this.Player.ShowStatsWindow(itemid, world);

                                return;
                            }
                        }
                    }

                    // if got here log bad GID packet since they have no open vendors with that template id
                }
            }
        }
    }
}
