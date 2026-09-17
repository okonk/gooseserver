namespace Goose.Quests
{
    class QuestInfoWindow : Window
    {
        enum QuestInfoState
        {
            QuestDescription,
            QuestRequirements,
        }

        private readonly Quest quest;
        private QuestInfoState state = QuestInfoState.QuestDescription;

        public QuestInfoWindow(Quest quest, Player player, GameWorld world)
        {
            this.ID = ++player.LastWindowID;
            this.Title = quest.Name;
            this.Buttons = "0,1,0,1,0";
            this.Frame = WindowFrames.Quest;
            this.Type = WindowTypes.QuestInfo;
            this.quest = quest;

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public override void Populate(Player player, GameWorld world)
        {
            var lines = this.GetCurrentText(player, world).Split(new string[] { "\\n" }, StringSplitOptions.None);

            int lineNo = 1;
            foreach (var line in lines)
            {
                world.Send(player, P.WindowTextLine(this.ID, lineNo, line));

                lineNo++;
            }
        }

        private string GetCurrentText(Player player, GameWorld world)
        {
            return this.state switch
            {
                QuestInfoState.QuestDescription => this.quest.Description,
                QuestInfoState.QuestRequirements => QuestWindow.GetQuestProgressText(this.quest, player, world),
                _ => "",
            };
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
                    if (this.state == QuestInfoState.QuestDescription)
                    {
                        this.state = QuestInfoState.QuestRequirements;
                        this.Buttons = "0,1,0,0,0";
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
