namespace Goose
{
    public class BackpackWindow : ItemContainerWindow
    {
        private int columns;

        public BackpackWindow(GameWorld world, Player player)
        {
            this.ItemContainer = player.Inventory.GetBackpackContainer();
            this.columns = player.Inventory.EquippedBackpack?.BackpackColumns ?? 0;
            this.ID = ++player.LastWindowID;
            this.Title = "Backpack";
            this.Buttons = "0,1,0,0,0";
            this.Frame = WindowFrames.GridContainer;
            this.Type = WindowTypes.Backpack;
            this.SendCreate(player, world);
        }

        public override int FrameParam1 => this.ItemContainer.MaxSlots - 1;
        public override int FrameParam2 => this.columns;

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
                    player.Windows.Remove(this);
                    break;
                default:
                    base.Clicked(buttonid, npcid, id2, id3, player, world);
                    break;
            }
        }

        public static void Toggle(GameWorld world, Player player)
        {
            var existing = player.Windows.OfType<BackpackWindow>().FirstOrDefault();
            if (existing is not null)
            {
                existing.Close(player, world);
                return;
            }

            if (player.Inventory.EquippedBackpack is null)
            {
                world.Send(player, P.Notice(NoticeType.Info, "You don't have a backpack equipped."));
                return;
            }

            Open(world, player);
        }

        public static void Open(GameWorld world, Player player)
        {
            foreach (var existing in player.Windows.OfType<BackpackWindow>().ToList())
                existing.Close(player, world);

            player.Windows.Add(new BackpackWindow(world, player));
        }

        public void Resize(Player player, GameWorld world)
        {
            this.ItemContainer = player.Inventory.GetBackpackContainer();
            this.columns = player.Inventory.EquippedBackpack?.BackpackColumns ?? 0;
            this.SendCreate(player, world);
        }

        public static void CloseFor(Player player, GameWorld world)
        {
            foreach (var window in player.Windows.OfType<BackpackWindow>().ToList())
                window.Close(player, world);
        }
    }
}
