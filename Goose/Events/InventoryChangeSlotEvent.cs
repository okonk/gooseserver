using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * InventoryChangeSlotEvent, event for "CHANGE" packet
     * 
     * Called when someone moves an item in their inventory
     * Packet format: CHANGEslotid1,slotid2
     * 
     */
    class InventoryChangeSlotEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new InventoryChangeSlotEvent();
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
                string[] ids = ((string)this.Data).Substring(6).Split(",".ToCharArray());

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

                if (id1 <= 0 || id2 <= 0)
                {
                    // log something bad about packet
                    return;
                }

                this.Player.Inventory.SwapSlots(id1, id2, world);
            }
        }
    }
}
