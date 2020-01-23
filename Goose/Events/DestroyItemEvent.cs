using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * DestroyItemEvent, destroy item
     * 
     * Packet: DITM<slot>
     * 
     * Slot can be inventory or an equipped item
     * 
     */
    public class DestroyItemEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new DestroyItemEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id = 0;
                string data = ((string)this.Data).Substring(4);
                try
                {
                    id = Convert.ToInt32(data);
                }
                catch (Exception)
                {
                    id = 0;
                }

                if (id <= 0 || id > GameWorld.Settings.InventorySize + 
                    GameWorld.Settings.EquippedSize) return;

                if (id <= GameWorld.Settings.InventorySize)
                {
                    ItemSlot slot = this.Player.Inventory.GetSlot(id);
                    if (slot == null) return;
                    slot = this.Player.Inventory.RemoveItem(slot.Item, slot.Stack, world);
                    if (slot == null) return;
                }
                else
                {
                    ItemSlot slot = this.Player.Inventory.GetEquippedSlot(id);
                    if (slot == null) return;
                    this.Player.Inventory.Unequip(id, world);
                    slot = this.Player.Inventory.RemoveItem(slot.Item, slot.Stack, world);
                    if (slot == null) return;
                }
            }
        }
    }
}
