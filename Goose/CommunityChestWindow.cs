namespace Goose
{
    public class CommunityChestWindow : ItemContainerWindow
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public int SlotsPerPage { get; private set; }

        public int CurrentPage { get; set; }

        public int MaxPages { get; set; }

        public override string Title
        {
            get => $"Community Chest Page {CurrentPage}/{MaxPages}";
        }

        public override string Buttons
        {
            get => $"0,1,{(CurrentPage - 1 <= 0 ? "0" : "1")},{(CurrentPage == MaxPages ? "0" : "1")},0";
        }

        public CommunityChestWindow(GameWorld world, Player player, NPC npc)
        {
            this.SlotsPerPage = world.Settings.BankSlotsPerPage;
            this.ItemContainer = world.ChestHandler.GetOrCreateContainer(world, npc.NPCTemplateID);
            this.CurrentPage = 1;
            this.MaxPages = (this.ItemContainer.MaxSlots - 1 + this.SlotsPerPage - 1) / this.SlotsPerPage;

            this.ID = ++player.LastWindowID;
            this.Frame = WindowFrames.GenericContainer;
            this.Type = WindowTypes.CommunityChest;
            this.NPC = npc;

            world.ChestHandler.AddViewer(this.ItemContainer, player, this);
            this.SendCreate(player, world);
        }

        // Chests are shared storage, so the same acquisition gates as pickup apply to anything
        // crossing the boundary in either direction (both sides of every swap are validated).
        public override bool CanDeposit(Player player, ItemSlot? incoming, GameWorld world)
        {
            if (incoming?.Item is { IsBound: true } or { IsBindOnPickup: true })
            {
                world.Send(player, P.ServerMessage("That item is bound to you."));
                return false;
            }

            return true;
        }

        public override bool CanWithdraw(Player player, ItemSlot? outgoing, GameWorld world)
        {
            if (outgoing is null) return true;

            Item item = outgoing.Item;

            if (item.IsBound)
            {
                world.Send(player, P.ServerMessage("That item is bound."));
                return false;
            }

            if (item.IsLore && player.HasItem(item.Template.ID))
            {
                world.Send(player, P.ServerMessage("Already have LORE item " + item.Name + "."));
                return false;
            }

            string? refusal = null;
            try
            {
                refusal = item.Script?.Object.CanPickup(player, item, world);
            }
            catch (Exception e)
            {
                // Fail CLOSED: a broken gate script must refuse rather than admit.
                log.Error(e, "Item CanPickup {0} Exception", item.TemplateID);
                refusal = "You cannot pick that up right now.";
            }
            if (refusal is not null)
            {
                world.Send(player, P.ServerMessage(refusal));
                return false;
            }

            // An unbound BOP item can only exist via persisted-older-state or GM/script
            // generation; withdrawal is acquisition, so refuse rather than launder it out unbound.
            if (item.IsBindOnPickup && !item.IsBound)
            {
                world.Send(player, P.ServerMessage("That item is bound to you."));
                return false;
            }

            return true;
        }

        // The client's v1 widget is single-instance per frame: a second MKW retargets it, so a
        // second server-side chest window would be a ghost. Replacing on open keeps server, viewer registry, and widget state identical.
        public static void Open(GameWorld world, Player player, NPC npc)
        {
            var existing = player.Windows.Where(w => w.Type == WindowTypes.CommunityChest).ToList();
            foreach (var old in existing)
            {
                world.ChestHandler.RemoveViewer(player, (CommunityChestWindow)old);
                player.Windows.Remove(old);
            }

            player.Windows.Add(new CommunityChestWindow(world, player, npc));
        }

        public override void Populate(Player player, GameWorld world)
        {
            for (int i = 1; i <= SlotsPerPage; i++)
            {
                this.SendSlot(i, player, world);
            }
        }

        public override void Refresh(Player player, GameWorld world)
        {
            this.Populate(player, world);
        }

        protected override int GetSlotOffset()
        {
            return (CurrentPage - 1) * SlotsPerPage;
        }

        protected override bool PushesViaBroadcast => true;

        private bool ChestInRange(Player player)
        {
            return Map.InRange(player, this.NPC!);
        }

        protected override bool WindowToWindowBlocked(Player player)
        {
            return !ChestInRange(player);
        }

        // Page-local indices must resolve to real container slots: a page-2 index into a shorter
        // container would swap the item out of the inventory and discard it.
        public override bool ValidateSlotIndex(int index)
        {
            return index > 0 && index + GetSlotOffset() < ItemContainer.MaxSlots;
        }

        public override ItemSlot? GetSlot(int slotIndex)
        {
            return this.ItemContainer.GetSlot(slotIndex + GetSlotOffset());
        }

        public override void SetSlot(int slotIndex, ItemSlot? slot)
        {
            this.ItemContainer.SetSlot(slotIndex + GetSlotOffset(), slot);
        }

        public override void InventoryToWindow(Player player, int invSlotIndex, int toSlotIndex, GameWorld world)
        {
            if (!ChestInRange(player)) return;

            base.InventoryToWindow(player, invSlotIndex, toSlotIndex, world);
        }

        public override void WindowToInventory(Player player, int fromSlotIndex, int invSlotIndex, GameWorld world)
        {
            if (!ChestInRange(player)) return;

            base.WindowToInventory(player, fromSlotIndex, invSlotIndex, world);
        }

        public override void SendSlot(int slotIndex, Player player, GameWorld world)
        {
            ItemSlot? slot = this.GetSlot(slotIndex);
            if (slot is not null)
            {
                world.Send(player, P.GenericWindowSlot(this, slot.Item, world, slotIndex, slot.Stack));
            }
            else
            {
                world.Send(player, P.ClearGenericWindowSlot(this, slotIndex));
            }
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    world.ChestHandler.RemoveViewer(player, this);
                    player.Windows.Remove(this);
                    break;
                case ButtonTypes.Next:
                    if (CurrentPage == MaxPages)
                        return;

                    CurrentPage++;
                    this.SendCreate(player, world);
                    break;
                case ButtonTypes.Back:
                    if (CurrentPage - 1 <= 0)
                        return;

                    CurrentPage--;
                    this.SendCreate(player, world);
                    break;
                default:
                    base.Clicked(buttonid, npcid, id2, id3, player, world);
                    break;
            }
        }
    }
}
