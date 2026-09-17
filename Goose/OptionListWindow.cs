namespace Goose
{
    public class OptionListWindow : Window
    {
        private readonly List<string> lines;
        private readonly Action<int, Player, GameWorld> onLineClicked;
        private int page;

        public OptionListWindow(Player player, GameWorld world, string title, List<string> lines, Action<int, Player, GameWorld> onLineClicked, NPC? npc = null)
        {
            this.ID = ++player.LastWindowID;
            this.Title = title;
            this.Frame = WindowFrames.OptionList;
            this.Type = WindowTypes.OptionList;
            this.NPC = npc;
            this.lines = lines;
            this.onLineClicked = onLineClicked;
            this.Buttons = this.GetPagingButtons();

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        private int PageCount => (this.lines.Count + LineClickCount - 1) / LineClickCount;

        private string GetPagingButtons()
        {
            // combine,close,back,next,ok
            return $"0,1,{(this.page > 0 ? 1 : 0)},{(this.page < this.PageCount - 1 ? 1 : 0)},0";
        }

        public override void Populate(Player player, GameWorld world)
        {
            int lineNo = 1;
            foreach (var line in this.lines.Skip(this.page * LineClickCount).Take(LineClickCount))
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
                    if (this.page > 0)
                    {
                        this.page--;
                        this.Buttons = this.GetPagingButtons();
                        this.SendCreate(player, world);
                    }
                    break;
                case ButtonTypes.Next:
                    if (this.page < this.PageCount - 1)
                    {
                        this.page++;
                        this.Buttons = this.GetPagingButtons();
                        this.SendCreate(player, world);
                    }
                    break;
            }
        }

        public override void LineClicked(int line, int npcid, Player player, GameWorld world)
        {
            int index = this.page * LineClickCount + line;
            if (index < 0 || index >= this.lines.Count)
                return;

            this.Close(player, world);
            this.onLineClicked(index, player, world);
        }
    }
}
