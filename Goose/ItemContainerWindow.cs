using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public abstract class ItemContainerWindow : Window
    {
        public ItemContainer ItemContainer { get; protected set; }

        public override void Populate(Player player, GameWorld world)
        {
            for (int i = 1; i < this.ItemContainer.MaxSlots; i++)
            {
                this.SendSlot(i, player, world);
            }
        }

        public abstract void SendSlot(int slotIndex, Player player, GameWorld world);

        public virtual ItemSlot GetSlot(int slotIndex)
        {
            return this.ItemContainer.GetSlot(slotIndex);
        }

        public virtual void SetSlot(int slotIndex, ItemSlot slot)
        {
            this.ItemContainer.SetSlot(slotIndex, slot);
        }

        public override void InventoryToWindow(Player player, int invSlotIndex, int toSlotIndex, GameWorld world)
        {
            if (toSlotIndex <= 0 || toSlotIndex > this.ItemContainer.MaxSlots) return; // log bad attempt at crash

            ItemSlot inventorySlot = player.Inventory.GetSlot(invSlotIndex);
            ItemSlot containerSlot = this.ItemContainer.GetSlot(toSlotIndex);

            ItemSlot.SwapSlots(ref inventorySlot, ref containerSlot);

            player.Inventory.SetSlot(invSlotIndex, inventorySlot);
            this.ItemContainer.SetSlot(toSlotIndex, containerSlot);

            player.Inventory.SendSlot(invSlotIndex, world);
            this.SendSlot(toSlotIndex, player, world);
        }

        public override void WindowToInventory(Player player, int fromSlotIndex, int invSlotIndex, GameWorld world)
        {
            if (fromSlotIndex <= 0 || fromSlotIndex > this.ItemContainer.MaxSlots) return; // log bad attempt at crash

            ItemSlot containerSlot = this.ItemContainer.GetSlot(fromSlotIndex);
            ItemSlot inventorySlot = player.Inventory.GetSlot(invSlotIndex);

            ItemSlot.SwapSlots(ref containerSlot, ref inventorySlot);

            this.ItemContainer.SetSlot(fromSlotIndex, containerSlot);
            player.Inventory.SetSlot(invSlotIndex, inventorySlot);

            this.SendSlot(fromSlotIndex, player, world);
            player.Inventory.SendSlot(invSlotIndex, world);
        }

        public static void WindowToWindow(Player player, ItemContainerWindow fromWindow, int fromSlotIndex, ItemContainerWindow toWindow, int toSlotIndex, GameWorld world)
        {
            if (!fromWindow.ValidateSlotIndex(fromSlotIndex) || !toWindow.ValidateSlotIndex(toSlotIndex)) return;

            if (fromWindow is BankWindow && !(fromWindow as BankWindow).BankerInRange(player)) return;
            if (toWindow is BankWindow && !(toWindow as BankWindow).BankerInRange(player)) return;

            ItemSlot fromContainer = fromWindow.GetSlot(fromSlotIndex);
            ItemSlot toContainer = toWindow.GetSlot(toSlotIndex);

            ItemSlot.SwapSlots(ref fromContainer, ref toContainer);

            fromWindow.SetSlot(fromSlotIndex, fromContainer);
            toWindow.SetSlot(toSlotIndex, toContainer);

            fromWindow.SendSlot(fromSlotIndex, player, world);
            toWindow.SendSlot(toSlotIndex, player, world);
        }

        public virtual bool ValidateSlotIndex(int index)
        {
            return (index > 0 && index <= ItemContainer.MaxSlots);
        }
    }
}
