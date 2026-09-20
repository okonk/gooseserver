namespace Goose
{
    public class OptionListWindow : Window
    {
        private readonly List<string> lines;
        private readonly List<(int Sheet, int Graphic)>? lineGraphics;
        private readonly Action<int, Player, GameWorld> onLineClicked;
        private int page;

        public OptionListWindow(Player player, GameWorld world, string title, List<string> lines, Action<int, Player, GameWorld> onLineClicked, NPC? npc = null, int startPage = 0, List<(int Sheet, int Graphic)>? lineGraphics = null)
        {
            this.ID = ++player.LastWindowID;
            this.Title = title;
            this.Frame = WindowFrames.OptionList;
            this.Type = WindowTypes.OptionList;
            this.NPC = npc;
            this.lines = lines;
            this.lineGraphics = lineGraphics;
            this.onLineClicked = onLineClicked;
            this.page = Math.Min(Math.Max(startPage, 0), Math.Max(this.PageCount - 1, 0));
            this.Buttons = this.GetPagingButtons();

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public int Page { get => this.page; }

        private int PageCount => (this.lines.Count + LineClickCount - 1) / LineClickCount;

        private string GetPagingButtons()
        {
            // combine,close,back,next,ok
            return $"0,1,{(this.page > 0 ? 1 : 0)},{(this.page < this.PageCount - 1 ? 1 : 0)},0";
        }

        public override void Populate(Player player, GameWorld world)
        {
            int lineNo = 1;
            int firstIndex = this.page * LineClickCount;
            foreach (var line in this.lines.Skip(firstIndex).Take(LineClickCount))
            {
                int absolute = firstIndex + lineNo - 1;
                int sheet = 0, graphic = 0;
                if (this.lineGraphics is not null && absolute < this.lineGraphics.Count)
                {
                    sheet = this.lineGraphics[absolute].Sheet;
                    graphic = this.lineGraphics[absolute].Graphic;
                }
                world.Send(player, P.WindowLine(this.ID, lineNo++, line, sheet, graphic, false, 0, 0, 0));
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
