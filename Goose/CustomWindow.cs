namespace Goose
{
    public class CustomWindow : Window
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        // Ticket's inventory slot id, not the ItemSlot: the slot object can be
        // replaced by item operations while the window is open.
        public int TicketSlotId { get; }

        public CustomWindow(Player player, GameWorld world, int ticketSlotId)
        {
            this.TicketSlotId = ticketSlotId;

            this.ID = ++player.LastWindowID;
            this.Title = "Custom";
            this.Buttons = "0,1,0,0,1";
            this.Frame = WindowFrames.Custom;
            this.Type = WindowTypes.Custom;

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    player.Windows.Remove(this);
                    break;
            }
        }

        public static CustomWindow? FindOpen(Player player)
        {
            return player.Windows.OfType<CustomWindow>().FirstOrDefault();
        }

        public static void HandleSlot(GameWorld world, Player player, int lookSlotId, int statsSlotId)
        {
            CustomWindow? window = FindOpen(player);
            if (window is null) return;

            Item? lookItem = lookSlotId != 0 ? player.Inventory.GetSlot(lookSlotId)?.Item : null;
            Item? statsItem = statsSlotId != 0 ? player.Inventory.GetSlot(statsSlotId)?.Item : null;

            if ((lookSlotId != 0 && lookItem is null) || (statsSlotId != 0 && statsItem is null))
            {
                world.Send(player, P.ServerMessage("Items missing for customisation"));
                return;
            }

            if ((lookSlotId != 0 && lookSlotId == statsSlotId)
                || lookSlotId == window.TicketSlotId
                || statsSlotId == window.TicketSlotId)
            {
                world.Send(player, P.ServerMessage("Items to be customised must be equipment and must be visible items."));
                return;
            }

            if (lookItem is not null && statsItem is not null)
            {
                if (!CustomItem.ValidateItems(world, player, statsItem, lookItem)) return;
            }
            else if (lookItem is not null && !CustomItem.ValidateSingleItem(world, player, lookItem)) return;
            else if (statsItem is not null && !CustomItem.ValidateSingleItem(world, player, statsItem)) return;

            if (lookItem is not null)
                world.Send(player, "CWG" + lookItem.GraphicEquipped + "," + lookItem.BodyState);
        }

        public static void Create(GameWorld world, Player player, int lookSlotId, int statsSlotId,
                                  int r, int g, int b, int a, string rawName)
        {
            CustomWindow? window = FindOpen(player);
            if (window is null) return;

            ItemSlot? ticketSlot = player.Inventory.GetSlot(window.TicketSlotId);
            if (ticketSlot is null || ticketSlot.Item.TemplateID != world.Settings.CustomTicketId)
            {
                world.Send(player, P.ServerMessage("You need a custom ticket to customise an item."));
                return;
            }

            ItemSlot? lookSlot = lookSlotId != 0 ? player.Inventory.GetSlot(lookSlotId) : null;
            ItemSlot? statsSlot = statsSlotId != 0 ? player.Inventory.GetSlot(statsSlotId) : null;
            if (lookSlot is null || statsSlot is null
                || (lookSlotId != 0 && lookSlotId == statsSlotId)
                || lookSlotId == window.TicketSlotId
                || statsSlotId == window.TicketSlotId)
            {
                world.Send(player, P.ServerMessage("Items missing for customisation"));
                return;
            }

            if (!CustomItem.ValidateItems(world, player, statsSlot.Item, lookSlot.Item)) return;

            string? rgbaError = CustomItem.ParseRGBA(r, g, b, a, maxAlpha: 200);
            if (rgbaError is not null)
            {
                world.Send(player, P.ServerMessage(rgbaError));
                return;
            }

            string? name = CustomItem.SanitizeName(rawName);
            if (name is null)
            {
                world.Send(player, P.ServerMessage("Custom name cannot be empty."));
                return;
            }

            Item? item = CustomItem.BuildCustomItem(statsSlot.Item, lookSlot.Item, r, g, b, a, name, player.Name);
            if (item is null)
            {
                log.Error("Custom create for player {0}: stats template {1} is invalid", player.Name, statsSlot.Item.TemplateID);
                world.Send(player, P.ServerMessage("Items to be customised must be equipment and must be visible items."));
                return;
            }

            // Stacked items are decremented, not freed: only stack-1 consumes free a slot.
            int freedByConsumes = (lookSlot.Stack == 1 ? 1 : 0)
                + (statsSlot.Stack == 1 ? 1 : 0)
                + (ticketSlot.Stack == 1 ? 1 : 0);
            if (player.Inventory.GetNumberOfFreeSlots() + freedByConsumes < 1)
            {
                world.Send(player, P.ServerMessage("Not enough inventory space for the custom."));
                return;
            }

            // Target is computed before consuming: AddItem's CanStack path could merge the
            // custom item into a remaining stack of the same template.
            int target = ticketSlot.Stack == 1 ? window.TicketSlotId : player.Inventory.GetNextFreeSlot();
            if (target == -1 && lookSlot.Stack == 1) target = lookSlotId;
            if (target == -1 && statsSlot.Stack == 1) target = statsSlotId;

            // A zero/negative stack from corrupted data would otherwise be partially
            // consumed before a later RemoveItem fails; guard up front so nothing is lost.
            if (lookSlot.Stack < 1 || statsSlot.Stack < 1 || ticketSlot.Stack < 1)
            {
                world.Send(player, P.ServerMessage("Items missing for customisation"));
                return;
            }

            if (player.Inventory.RemoveItem(lookSlot.Item, 1, world) is null
                || player.Inventory.RemoveItem(statsSlot.Item, 1, world) is null
                || player.Inventory.RemoveItem(ticketSlot.Item, 1, world) is null)
            {
                log.Error("Custom create for player {0}: item vanished during consumption", player.Name);
                world.Send(player, P.ServerMessage("Items missing for customisation"));
                return;
            }

            world.ItemHandler.AddAndAssignId(item, world);
            player.Inventory.SetSlot(target, new ItemSlot { Item = item, Stack = 1 });
            player.Inventory.SendSlot(target, world);

            world.LogHandler.Log(Log.Types.CreatedCustom, player,
                $"{item.Name} ({item.TemplateID}) {lookSlot.Item.TemplateID}|{r},{g},{b},{a}", item.ItemID);

            world.Send(player, P.ServerMessage("Created custom: " + item.Name));
            window.Close(player, world);
        }
    }
}
