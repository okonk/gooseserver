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
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int npcid = 0;
                int slotid = 0;

                string[] t = ((string)this.Data).Substring(3).Split(',');

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

                if (npcid <= 0 || slotid <= 0 || slotid > world.Settings.VendorSlotSize) return;

                NPC npc = null;

                foreach (var window in this.Player.Windows)
                {
                    if (window.Type == Window.WindowTypes.Vendor &&
                        window.NPC!.LoginID == npcid)
                    {
                        npc = window.NPC!;
                        break;
                    }
                }

                if (npc is null) return;

                if (npc.State != NPC.States.Alive ||
                    npc.Map != this.Player.Map ||
                    Math.Abs(npc.MapX - this.Player.MapX) > Map.RANGE_X ||
                    Math.Abs(npc.MapY - this.Player.MapY) > Map.RANGE_Y)
                {
                    return;
                }

                // log bad npc
                if (npc is null) return;

                NPCVendorSlot slot = npc.VendorItems![slotid];

                // log bad slot purchase
                if (slot is null) return;

                if (slot.ItemTemplate.IsLore && this.Player.HasItem(slot.ItemTemplate.ID))
                {
                    world.Send(this.Player, P.ServerMessage("Can't purchase " + slot.ItemTemplate.Name +
                        " as it is LORE and you already have this item."));
                    return;
                }

                var currency = world.CurrencyHandler.Resolve(slot.ItemTemplate, npc);
                long cost = currency.GetBuyPrice(slot.ItemTemplate, slot.Stack);

                if (cost < 0 || currency.GetBalance(this.Player) < cost)
                {
                    world.Send(this.Player, P.ServerMessage("Can't purchase " + slot.ItemTemplate.Name +
                        (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") +
                        " as you don't have enough " + currency.Name + "."));
                    return;
                }

                Item item = new Item();
                item.LoadFromTemplate(slot.ItemTemplate);

                world.ItemHandler.RollTitleAndSurname(item, world);

                world.ItemHandler.AddAndAssignId(item, world);

                if (this.Player.Inventory.AddItem(item, slot.Stack, world))
                {
                    // Charged only after the item lands, so a full inventory costs nothing.
                    currency.Remove(this.Player, cost, world);

                    world.Send(this.Player, P.ServerMessage("Purchased " + item.Name +
                        (slot.Stack > 1 ? " (" + slot.Stack + ")" : "") +
                        " for " + cost + " " + currency.Name + "."));

                    world.LogHandler.Log(Log.Types.BuyFromVendor, this.Player.PlayerID,
                        $"{item.Name} ({item.TemplateID}) x{slot.Stack} ({cost} {currency.ShortName})",
                        npc.NPCTemplateID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);

                    if (item.IsBindOnPickup)
                    {
                        item.IsBound = true;
                    }

                    return;
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Can't purchase " + slot.ItemTemplate.Name +
                        " as your inventory is full."));
                    return;
                }
            }
        }
    }
}
