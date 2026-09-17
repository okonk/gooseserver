namespace Goose
{
    public class RecipeWindow : Window
    {
        private readonly string text;
        private readonly Action<Player, GameWorld>? onBack;

        public RecipeWindow(Combination combination, Player player, GameWorld world, Action<Player, GameWorld>? onBack = null)
        {
            var lines = new List<string>
            {
                "Place the following items in a combine bag:",
                "",
            };
            foreach (var (itemId, count) in combination.RequiredHash)
            {
                var item = world.ItemHandler.GetTemplate(itemId);
                lines.Add($"{(item?.Name ?? "Unknown item")} ({count})");
            }
            this.text = string.Join("\\n", lines);
            this.onBack = onBack;

            this.ID = ++player.LastWindowID;
            this.Title = combination.Name;
            this.Buttons = onBack is null ? "0,1,0,0,0" : "0,1,1,0,0";
            this.Frame = WindowFrames.Quest;
            this.Type = WindowTypes.Recipe;

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public override void Populate(Player player, GameWorld world)
        {
            int lineNo = 1;
            foreach (var line in this.text.Split(new string[] { "\\n" }, StringSplitOptions.None))
                world.Send(player, P.WindowTextLine(this.ID, lineNo++, line));
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    player.Windows.Remove(this);
                    break;
                case ButtonTypes.Back:
                    this.Close(player, world);
                    this.onBack?.Invoke(player, world);
                    break;
            }
        }
    }
}
