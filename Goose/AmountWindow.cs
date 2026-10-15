namespace Goose
{
    public class AmountWindow : Window
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly string prompt;
        private readonly (int Sheet, int Graphic, int R, int G, int B, int A) icon;
        private readonly long min;
        private readonly long max;
        private readonly long defaultValue;
        private readonly Action<long, Player, GameWorld> onConfirm;

        public AmountWindow(Player player, GameWorld world, string title, string prompt,
            (int Sheet, int Graphic, int R, int G, int B, int A) icon,
            long min, long max, long defaultValue, Action<long, Player, GameWorld> onConfirm, NPC? npc = null)
        {
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(min), min, "min must be <= max");
            if (defaultValue < min || defaultValue > max)
                throw new ArgumentOutOfRangeException(nameof(defaultValue), defaultValue, "defaultValue must be within [min, max]");

            this.ID = ++player.LastWindowID;
            this.Title = title;
            this.Frame = WindowFrames.Amount;
            this.Type = WindowTypes.Amount;
            this.NPC = npc;
            this.prompt = prompt;
            this.icon = icon;
            this.min = min;
            this.max = max;
            this.defaultValue = defaultValue;
            this.onConfirm = onConfirm;
            this.Buttons = "0,1,0,0,0";

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public override void Populate(Player player, GameWorld world)
        {
            world.Send(player, P.AmountInfo(this.ID, this.icon, this.min, this.max, this.defaultValue, this.prompt));
        }

        public void Submit(long amount, Player player, GameWorld world)
        {
            if (amount < this.min || amount > this.max)
            {
                log.Debug("Player {player} submitted amount {amount} outside [{min},{max}] for window {id}",
                    player.Name, amount, this.min, this.max, this.ID);
                return;
            }

            this.Close(player, world);
            this.onConfirm(amount, player, world);
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
    }
}
