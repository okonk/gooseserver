namespace Goose.Quests
{
    class AbandonConfirmWindow : Window
    {
        private readonly Quest quest;

        public AbandonConfirmWindow(Quest quest, Player player, GameWorld world)
        {
            this.ID = ++player.LastWindowID;
            this.Title = "Abandon Quest";
            this.Buttons = "0,1,0,1,0";
            this.Frame = WindowFrames.GenericInfo;
            this.Type = WindowTypes.Generic;
            this.quest = quest;

            player.Windows.Add(this);
            this.SendCreate(player, world);
        }

        public override void Populate(Player player, GameWorld world)
        {
            var lines = $"Are you sure you want to abandon \\n\"{this.quest.Name}\"?\\n\\nAny progress on it will be lost.\\nClick next to confirm.".Split(new string[] { "\\n" }, StringSplitOptions.None);

            int lineNo = 1;
            foreach (var line in lines)
            {
                world.Send(player, P.WindowTextLine(this.ID, lineNo, line));

                lineNo++;
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
                case ButtonTypes.Next:
                    // The quest may have completed or been re-accepted while this window was open.
                    if (player.QuestsStarted.Any(q => q.Id == this.quest.Id))
                    {
                        player.QuestsStarted.RemoveAll(q => q.Id == this.quest.Id);
                        player.QuestProgress.RemoveAll(p => p.Requirement.Quest.Id == this.quest.Id);
                        world.Send(player, P.ServerMessage($"Abandoned quest: {this.quest.Name}"));
                    }
                    this.Close(player, world);
                    break;
                default:
                    player.Windows.Remove(this);
                    break;
            }
        }
    }
}
