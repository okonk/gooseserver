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
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

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

                if (id <= 0 || id > world.Settings.InventorySize +
                    world.Settings.EquippedSize) return;

                ItemSlot? slot = id <= world.Settings.InventorySize
                    ? this.Player.Inventory.GetSlot(id)
                    : this.Player.Inventory.GetEquippedSlot(id);
                if (slot is null || slot.Item is null) return;

                Item? replacement = null;
                if (slot.Item.Custom)
                {
                    ItemTemplate? template = world.ItemHandler.GetTemplate(world.Settings.RippedCustomTicketId);
                    if (template is null) return;

                    replacement = new Item();
                    if (!replacement.LoadFromTemplate(template))
                    {
                        log.Error("custom ticket template {0}: invalid template; destroy skipped", world.Settings.RippedCustomTicketId);
                        return;
                    }
                }

                if (id > world.Settings.InventorySize && !this.Player.Inventory.Unequip(id, world)) return;

                if (replacement is not null)
                {
                    this.Player.Inventory.RemoveItem(slot.Item, slot.Stack, world);

                    world.ItemHandler.AddAndAssignId(replacement, world);
                    this.Player.Inventory.AddItem(replacement, 1, world);
                    return;
                }

                this.Player.Inventory.RemoveItem(slot.Item, slot.Stack, world);
            }
        }
    }
}
