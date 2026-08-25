using System.Text;

namespace Goose.Events
{
    /**
     * Player sold item to vendor
     *
     * Format: VSInpcid,invslotid,stack
     *
     */
    public class VendorSellInventoryEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int npcid = 0;
                int slotid = 0;
                int stack = 0;

                string[] t = ((string)this.Data).Substring(3).Split(',');

                // log bad packet
                if (t.Length != 3) return;

                try
                {
                    npcid = Convert.ToInt32(t[0]);
                    slotid = Convert.ToInt32(t[1]);
                    stack = Convert.ToInt32(t[2]);
                }
                catch (Exception)
                {
                    npcid = 0;
                    slotid = 0;
                    stack = 0;
                }

                // log bad npc/slot
                if (npcid <= 0 || slotid <= 0 || slotid > world.Settings.InventorySize) return;

                NPC npc = null;

                foreach (var window in this.Player.Windows)
                {
                    if (window.Type == Window.WindowTypes.Vendor &&
                        window.NPC.LoginID == npcid)
                    {
                        npc = window.NPC;
                        break;
                    }
                }

                // log bad npc
                if (npc is null) return;

                if (npc.State != NPC.States.Alive ||
                    npc.Map != this.Player.Map ||
                    Math.Abs(npc.MapX - this.Player.MapX) > Map.RANGE_X ||
                    Math.Abs(npc.MapY - this.Player.MapY) > Map.RANGE_Y)
                {
                    return;
                }

                ItemSlot slot = this.Player.Inventory.GetSlot(slotid);
                // log bad slot
                if (slot is null) return;

                // log bad stack size
                if (stack <= 0 || stack > slot.Stack) return;

                var currency = world.CurrencyHandler.Resolve(slot.Item.Template, npc);
                long price = currency.GetSellPrice(slot.Item, stack);

                // Refused before the item leaves the bag.
                if (price < 0)
                {
                    world.Send(this.Player, P.ServerMessage("I have no interest in purchasing " + slot.Item.Name + "."));
                    return;
                }

                ItemSlot sellslot = this.Player.Inventory.RemoveItem(slot.Item, stack, world);

                currency.Add(this.Player, price, world);

                world.Send(this.Player, P.ServerMessage("Sold " + sellslot.Item.Name +
                    (sellslot.Stack > 1 ? " (" + sellslot.Stack + ")" : "") +
                    " for " + price + " " + currency.Name + "."));

                world.LogHandler.Log(Log.Types.SellToVendor, this.Player.PlayerID,
                    $"{slot.Item.Name} ({slot.Item.TemplateID}) x{slot.Stack} ({price} {currency.ShortName})",
                    npc.NPCTemplateID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
