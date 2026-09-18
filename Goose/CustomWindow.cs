namespace Goose
{
    public class CustomWindow : Window
    {
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
    }
}
