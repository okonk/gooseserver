using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Quests
{
    class QuestWindow : Window
    {
        enum QuestWindowState
        {
            QuestDescription,
            QuestFail,
            QuestPass,
        }

        private Quest quest;
        private QuestWindowState state;

        public QuestWindow(NPC npc, Player player, Quest quest, GameWorld world)
        {
            this.ID = ++player.LastWindowID;
            this.Title = quest.Name;
            this.Buttons = "0,1,0,1,0";
            this.Frame = WindowFrames.Quest;
            this.Type = WindowTypes.Quest;
            this.NPC = npc;
            this.quest = quest;
            this.state = QuestWindowState.QuestDescription;

            player.Windows.Add(this);
            player.TalkedTo(npc, world);

            this.SendCreate(player, world);
        }

        public void SendCreate(Player player, GameWorld world)
        {
            world.Send(player, this.MKWString());
            this.Populate(player, world);
            world.Send(player, "ENW" + this.ID);
        }

        public string MKWString()
        {
            return string.Format("MKW{0},{1},{2},{3},{4},{5},{6}",
                this.ID, (int)this.Frame, this.Title, this.Buttons, this.NPC.LoginID, 0, 0);
        }

        /// <summary>
        /// Called when a player left clicks a quest npc
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <param name="world"></param>
        public static void Handle(NPC npc, Player player, GameWorld world)
        {
            player.Windows.RemoveAll(w => w.Type == WindowTypes.Quest && w.NPC == npc);

            foreach (var quest in npc.Quests)
            {
                if (player.QuestsCompleted.Any(q => q.Quest.Id == quest.Id))
                    continue;

                var questWindow = new QuestWindow(npc, player, quest, world);
            }
        }

        public override void Populate(Player player, GameWorld world)
        {
            var text = ((state == QuestWindowState.QuestDescription) ? quest.Description : (state == QuestWindowState.QuestFail ? quest.FailText : quest.PassText));
            var lines = text.Split(new string[] { "\\n" }, StringSplitOptions.None);

            int lineNo = 1;
            foreach (var line in lines)
            {
                world.Send(player, string.Format("WNF{0},{1},{2}|0|0|0|0|*", this.ID, lineNo, line));

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
                case ButtonTypes.ShowNext:
                    if (this.state == QuestWindowState.QuestDescription)
                    {
                        this.Buttons = "0,1,0,0,0";

                        // player has opened the quest window twice, and already completed it in one
                        if (player.QuestsCompleted.Any(q => q.Quest.Id == quest.Id))
                        {
                            player.Windows.Remove(this);
                            return;
                        }

                        if (this.PlayerMeetsRequirements(player))
                        {
                            this.state = QuestWindowState.QuestPass;
                            this.CompleteQuest(player, world);
                        }
                        else
                        {
                            this.state = QuestWindowState.QuestFail;
                        }

                        this.SendCreate(player, world);
                    }
                    break;
                default:
                    player.Windows.Remove(this);
                    break;
            }
        }

        public bool PlayerMeetsRequirements(Player player)
        {
            foreach (var requirement in this.quest.Requirements)
            {
                switch (requirement.Type)
                {
                    case RequirementType.Gold:
                        if (player.Gold < requirement.Value)
                            return false;
                        break;
                    case RequirementType.Item:
                        if (!player.Inventory.HasItem((int)requirement.Value, requirement.Value2))
                            return false;
                        break;
                    case RequirementType.TalkToNPC:
                    case RequirementType.Kill:
                        if (!player.QuestProgress.Any(p => p.Requirement.Id == requirement.Id && p.Value >= p.Requirement.Value2))
                            return false;
                        break;
                    case RequirementType.ExperienceBanked:
                        if (player.Experience < requirement.Value)
                            return false;
                        break;
                    case RequirementType.ExperienceSold:
                        if (player.ExperienceSold < requirement.Value)
                            return false;
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        public void CompleteQuest(Player player, GameWorld world)
        {

        }
    }
}
