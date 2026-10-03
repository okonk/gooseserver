using System.Text;

namespace Goose
{
    public abstract class ItemContainerWindow : Window
    {
        public ItemContainer ItemContainer { get; protected set; } = null!;

        public override void Populate(Player player, GameWorld world)
        {
            for (int i = 1; i < this.ItemContainer.MaxSlots; i++)
            {
                this.SendSlot(i, player, world);
            }
        }

        public abstract void SendSlot(int slotIndex, Player player, GameWorld world);

        public virtual ItemSlot? GetSlot(int slotIndex)
        {
            return this.ItemContainer.GetSlot(slotIndex);
        }

        public virtual void SetSlot(int slotIndex, ItemSlot? slot)
        {
            this.ItemContainer.SetSlot(slotIndex, slot);
        }

        public virtual bool CanDeposit(Player player, ItemSlot? incoming, GameWorld world) => true;

        public virtual bool CanWithdraw(Player player, ItemSlot? outgoing, GameWorld world) => true;

        protected virtual bool PushesViaBroadcast => false;

        protected virtual int GetSlotOffset() => 0;

        // Every drag is a swap: both slots change hands, so both directions must be
        // validated before anything mutates.
        protected void SetSlotAnnouncing(Player player, int windowSlotIndex, ItemSlot? newSlot,
                                         ItemSlot? priorSlot, long priorStack, GameWorld world)
        {
            this.SetSlot(windowSlotIndex, newSlot);
            if (ReferenceEquals(priorSlot, newSlot) && priorSlot is not null &&
                priorSlot.Stack != priorStack)
                this.ItemContainer.NotifySlotChanged(windowSlotIndex + GetSlotOffset());
            if (!PushesViaBroadcast) this.SendSlot(windowSlotIndex, player, world);
        }

        public override void InventoryToWindow(Player player, int invSlotIndex, int toSlotIndex, GameWorld world)
        {
            if (!ValidateSlotIndex(toSlotIndex)) return;

            ItemSlot? inventorySlot = player.Inventory.GetSlot(invSlotIndex);
            ItemSlot? containerSlot = this.GetSlot(toSlotIndex);

            if (!CanWithdraw(player, containerSlot, world) || !CanDeposit(player, inventorySlot, world)) return;

            ItemSlot? priorSlot = containerSlot;
            long priorStack = containerSlot?.Stack ?? 0;
            ItemSlot.SwapSlots(ref inventorySlot, ref containerSlot);

            player.Inventory.SetSlot(invSlotIndex, inventorySlot);
            this.SetSlotAnnouncing(player, toSlotIndex, containerSlot, priorSlot, priorStack, world);

            player.Inventory.SendSlot(invSlotIndex, world);

            if (inventorySlot is not null || containerSlot is not null)
                world.QuestHandler.RefreshIcons(player, world);
        }

        public override void WindowToInventory(Player player, int fromSlotIndex, int invSlotIndex, GameWorld world)
        {
            if (!ValidateSlotIndex(fromSlotIndex)) return;

            ItemSlot? containerSlot = this.GetSlot(fromSlotIndex);
            ItemSlot? inventorySlot = player.Inventory.GetSlot(invSlotIndex);

            if (!CanWithdraw(player, containerSlot, world) || !CanDeposit(player, inventorySlot, world)) return;

            ItemSlot? priorSlot = containerSlot;
            long priorStack = containerSlot?.Stack ?? 0;
            ItemSlot.SwapSlots(ref containerSlot, ref inventorySlot);

            this.SetSlotAnnouncing(player, fromSlotIndex, containerSlot, priorSlot, priorStack, world);
            player.Inventory.SetSlot(invSlotIndex, inventorySlot);

            player.Inventory.SendSlot(invSlotIndex, world);

            if (inventorySlot is not null || containerSlot is not null)
                world.QuestHandler.RefreshIcons(player, world);
        }

        public static void WindowToWindow(Player player, ItemContainerWindow fromWindow, int fromSlotIndex, ItemContainerWindow toWindow, int toSlotIndex, GameWorld world)
        {
            if (!fromWindow.ValidateSlotIndex(fromSlotIndex) || !toWindow.ValidateSlotIndex(toSlotIndex)) return;

            if (fromWindow.WindowToWindowBlocked(player) || toWindow.WindowToWindowBlocked(player)) return;

            ItemSlot? fromContainer = fromWindow.GetSlot(fromSlotIndex);
            ItemSlot? toContainer = toWindow.GetSlot(toSlotIndex);

            if (!fromWindow.CanWithdraw(player, fromContainer, world) ||
                !toWindow.CanDeposit(player, fromContainer, world) ||
                !toWindow.CanWithdraw(player, toContainer, world) ||
                !fromWindow.CanDeposit(player, toContainer, world))
                return;

            ItemSlot? fromPriorSlot = fromContainer;
            ItemSlot? toPriorSlot = toContainer;
            long fromPriorStack = fromContainer?.Stack ?? 0;
            long toPriorStack = toContainer?.Stack ?? 0;
            ItemSlot.SwapSlots(ref fromContainer, ref toContainer);

            fromWindow.SetSlotAnnouncing(player, fromSlotIndex, fromContainer, fromPriorSlot, fromPriorStack, world);
            toWindow.SetSlotAnnouncing(player, toSlotIndex, toContainer, toPriorSlot, toPriorStack, world);
        }

        protected virtual bool WindowToWindowBlocked(Player player) => false;

        public virtual bool ValidateSlotIndex(int index)
        {
            // Slots are 1-based and containers are allocated as size+1, so the last
            // usable index is MaxSlots-1. Using <= here let a client address one past
            // the end of the backing array.
            return (index > 0 && index < ItemContainer.MaxSlots);
        }
    }
}
