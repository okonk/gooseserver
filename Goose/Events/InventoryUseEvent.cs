using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * InventoryUseEvent, event for "USE" packet
     * 
     * Called when someone uses an item in their inventory
     * Packet format: USEslotid
     * 
     */
    class InventoryUseEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new InventoryUseEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id = 0;
                try
                {
                    id = Convert.ToInt32(((string)this.Data).Substring(3));
                }
                catch (Exception)
                {
                    id = 0;
                }

                if (id <= 0)
                {
                    // log bad id
                    return;
                }

                int INVSIZE = GameWorld.Settings.InventorySize;

                if (id > 0 && id <= INVSIZE)
                {
                    this.Player.Inventory.Use(id, world);
                }
                else if (id > INVSIZE && id <= INVSIZE + GameWorld.Settings.EquippedSize + 1)
                {
                    this.Player.Inventory.Unequip(id, world);
                }
            }
        }
    }
}
