namespace Goose
{
    public class TextInputWindow : Window
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly string prompt;
        private readonly string buttonText;
        private readonly string defaultText;
        private readonly int maxLength;
        private readonly Action<string, Player, GameWorld> onConfirm;

        public TextInputWindow(Player player, GameWorld world, string title, string prompt, string buttonText,
            string defaultText, int maxLength, Action<string, Player, GameWorld> onConfirm)
        {
            if (maxLength < 1)
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "maxLength must be >= 1");
            if (string.IsNullOrEmpty(prompt))
                throw new ArgumentException("prompt must not be empty; the client's GetRemaining throws on an empty trailing field", nameof(prompt));

            this.ID = ++player.LastWindowID;
            this.Title = title;
            this.Frame = WindowFrames.TextInput;
            this.Type = WindowTypes.TextInput;
            this.prompt = prompt;
            this.buttonText = buttonText;
            this.defaultText = defaultText;
            this.maxLength = maxLength;
            this.onConfirm = onConfirm;
            this.Buttons = "0,1,0,0,0";

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public override void Populate(Player player, GameWorld world)
        {
            world.Send(player, P.TextInputInfo(this.ID, this.maxLength, this.buttonText, this.defaultText, this.prompt));
        }

        // Deliberately does not close (unlike AmountWindow): the caller closes on success.
        public void Submit(string text, Player player, GameWorld world)
        {
            var clean = new string(text.Where(c => !char.IsControl(c)).ToArray()).Trim();
            if (clean.Length == 0 || clean.Length > this.maxLength)
            {
                log.Debug("Player {player} submitted invalid text (length {length}, max {max}) for window {id}",
                    player.Name, clean.Length, this.maxLength, this.ID);
                return;
            }

            this.onConfirm(clean, player, world);
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
