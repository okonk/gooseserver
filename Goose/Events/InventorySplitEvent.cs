using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * InventorySplitEvent, event for "SPLIT" packet
     * 
     * Called when someone splits an item stack in their inventory
     * Packet format: SPLITslotid1,slotid2
     * 
     */
    public class InventorySplitEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new InventorySplitEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id1 = 0;
                int id2 = 0;
                string[] ids = ((string)this.Data).Substring(5).Split(",".ToCharArray());

                try
                {
                    id1 = Convert.ToInt32(ids[0]);
                    id2 = Convert.ToInt32(ids[1]);
                }
                catch (Exception)
                {
                    id1 = 0;
                    id2 = 0;
                }

                if (id1 <= 0 || id2 <= 0 ||
                    id1 > GameWorld.Settings.InventorySize || id2 > GameWorld.Settings.InventorySize)
                {
                    // log something bad about packet
                    return;
                }

                this.Player.Inventory.SplitSlots(id1, id2, world);
            }
        }
    }
}
