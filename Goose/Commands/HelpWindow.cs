namespace Goose.Commands
{
    public class HelpWindow : Window
    {
        public override string Title => "Command Help";

        public override string Buttons
            => $"0,1,{(pageNumber == 0 ? 0 : 1)},{(pageNumber == pages.Count - 1 ? 0 : 1)},0";

        private readonly List<List<string>> pages;
        private int pageNumber = 0;

        public HelpWindow(GameWorld world, Player player, List<List<string>> pages)
        {
            this.ID = ++player.LastWindowID;
            this.Frame = WindowFrames.Quest;
            this.Type = WindowTypes.Help;
            this.pages = pages;

            this.SendCreate(player, world);
        }

        public static void Open(GameWorld world, Player player, List<List<string>> pages)
        {
            player.Windows.Add(new HelpWindow(world, player, pages));
        }

        public override void Populate(Player player, GameWorld world)
        {
            var lineNumber = 1;
            foreach (var line in pages[pageNumber])
                world.Send(player, P.WindowTextLine(this.ID, lineNumber++, line));
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    player.Windows.Remove(this);
                    break;
                case ButtonTypes.Next:
                    if (pageNumber < pages.Count - 1)
                    {
                        pageNumber++;
                        this.SendCreate(player, world);
                    }
                    break;
                case ButtonTypes.Back:
                    if (pageNumber > 0)
                    {
                        pageNumber--;
                        this.SendCreate(player, world);
                    }
                    break;
                default:
                    player.Windows.Remove(this);
                    break;
            }
        }
    }
}
