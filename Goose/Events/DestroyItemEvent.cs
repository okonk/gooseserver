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

                bool wasCustom = false;

                if (id <= world.Settings.InventorySize)
                {
                    ItemSlot? slot = this.Player.Inventory.GetSlot(id);
                    if (slot is null || slot.Item is null) return;

                    wasCustom = slot.Item.Custom;
                    this.Player.Inventory.RemoveItem(slot.Item, slot.Stack, world);
                }
                else
                {
                    ItemSlot? slot = this.Player.Inventory.GetEquippedSlot(id);
                    if (slot is null || slot.Item is null) return;
                    if (!this.Player.Inventory.Unequip(id, world)) return;

                    wasCustom = slot.Item.Custom;

                    this.Player.Inventory.RemoveItem(slot.Item, slot.Stack, world);
                }

                if (wasCustom)
                {
                    ItemTemplate? template = world.ItemHandler.GetTemplate(world.Settings.RippedCustomTicketId);
                    if (template is null) return;

                    Item item = new Item();
                    item.LoadFromTemplate(template);

                    world.ItemHandler.AddAndAssignId(item, world);

                    this.Player.Inventory.AddItem(item, 1, world);
                }
            }
        }
    }
}
