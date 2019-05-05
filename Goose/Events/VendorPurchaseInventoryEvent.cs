using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * Player bought item from vendor
     * 
     * Format: VPInpcid,slotid
     * 
     */
    public class VendorPurchaseInventoryEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new VendorPurchaseInventoryEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int npcid = 0;
                int slotid = 0;

                string[] t = ((string)this.Data).Substring(3).Split(",".ToCharArray());
                
                // log bad packet
                if (t.Length != 2) return;

                try
                {
                    npcid = Convert.ToInt32(t[0]);
                    slotid = Convert.ToInt32(t[1]);
                }
                catch (Exception)
                {
                    npcid = 0;
                    slotid = 0;
                }

                if (npcid <= 0 || slotid <= 0 || slotid > GameSettings.Default.VendorSlotSize) return;

                NPC npc = null;

                foreach (Window window in this.Player.Windows)
                {
                    if (window.Type == Window.WindowTypes.Vendor &&
                        window.NPC.LoginID == npcid)
                    {
                        npc = window.NPC;
                        break;
                    }
                }

                if (npc == null) return;

                if (npc.State != NPC.States.Alive ||
                    npc.Map != this.Player.Map ||
                    Math.Abs(npc.MapX - this.Player.MapX) > Map.RANGE_X ||
                    Math.Abs(npc.MapY - this.Player.MapY) > Map.RANGE_Y)
                {
                    return;
                }

                // log bad npc
                if (npc == null) return;

                NPCVendorSlot slot = npc.VendorItems[slotid];

                // log bad slot purchase
                if (slot == null) return;

                if (slot.ItemTemplate.IsLore && this.Player.HasItem(slot.ItemTemplate.ID))
                {
                    world.Send(this.Player, "$7Can't purchase " + slot.ItemTemplate.Name + 
                        " as it is LORE and you already have this item.");
                    return;
                }

                if (!npc.CreditDealer && slot.ItemTemplate.Value * slot.Stack > this.Player.Gold)
                {
                    world.Send(this.Player, "$7Can't purchase " + slot.ItemTemplate.Name + 
                        (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") +
                        " as you don't have enough gold.");
                    return;
                }

                if (npc.CreditDealer && slot.ItemTemplate.Credits * slot.Stack > this.Player.Credits)
                {
                    world.Send(this.Player, "$7Can't purchase " + slot.ItemTemplate.Name +
                        (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") +
                        " as you don't have enough credits.");
                    return;
                }

                Item item = new Item();
                item.LoadFromTemplate(slot.ItemTemplate);

                world.ItemHandler.AddAndAssignId(item, world);

                if (this.Player.Inventory.AddItem(item, slot.Stack, world))
                {
                    if (npc.CreditDealer)
                    {
                        this.Player.Credits -= (slot.ItemTemplate.Credits * slot.Stack);

                        world.Send(this.Player, "$7Purchased " + slot.ItemTemplate.Name +
                            (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") +
                            " for " + slot.ItemTemplate.Credits * slot.Stack + " credits.");
                    }
                    else
                    {
                        this.Player.RemoveGold(slot.ItemTemplate.Value * slot.Stack, world);

                        world.Send(this.Player, "$7Purchased " + slot.ItemTemplate.Name +
                            (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") +
                            " for " + slot.ItemTemplate.Value * slot.Stack + " gold.");
                    }

                    if (item.IsBindOnPickup)
                    {
                        item.IsBound = true;
                    }

                    return;
                }
                else
                {
                    world.Send(this.Player, "$7Can't purchase " + slot.ItemTemplate.Name +
                        " as your inventory is full.");
                    return;
                }
            }
        }
    }
}
