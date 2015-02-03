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
            QuestNoInventorySpace,
            QuestNoSpellbookSpace
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
            string text = "";
            switch (state)
            {
                case QuestWindowState.QuestDescription:
                    text = quest.Description;
                    break;
                case QuestWindowState.QuestFail:
                    text = quest.FailText;
                    break;
                case QuestWindowState.QuestPass:
                    text = quest.PassText;
                    break;
                case QuestWindowState.QuestNoInventorySpace:
                    text = "You don't have enough inventory space to accept \\nthe reward.\\nDelete an item and try again.";
                    break;
                case QuestWindowState.QuestNoSpellbookSpace:
                    text = "You don't have enough spellbook space to accept \\nthe reward.\\nDelete a spell and try again.";
                    break;
            }

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
                            if (!this.PlayerHasEnoughInventorySpaceForReward(player, world))
                            {
                                this.state = QuestWindowState.QuestNoInventorySpace;
                            }
                            else if (!this.PlayerHasEnoughSpellbookSpaceForReward(player))
                            {
                                this.state = QuestWindowState.QuestNoSpellbookSpace;
                            }
                            else
                            {
                                this.state = QuestWindowState.QuestPass;
                                this.CompleteQuest(this.NPC, player, world);
                            }
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

        private bool PlayerHasEnoughSpellbookSpaceForReward(Player player)
        {
            var spellRewards = quest.Rewards.Where(r => r.Type == RewardType.LearnSpell);
            var freeSlots = player.Spellbook.GetNumberOfFreeSlots();

            return freeSlots >= spellRewards.Count();
        }

        private bool PlayerHasEnoughInventorySpaceForReward(Player player, GameWorld world)
        {
            var itemRewards = quest.Rewards.Where(r => r.Type == RewardType.Item);
            // assume 1 slot per reward item
            var freeSlots = player.Inventory.GetNumberOfFreeSlots();

            return freeSlots >= itemRewards.Count();
        }

        public void CompleteQuest(NPC npc, Player player, GameWorld world)
        {
            player.QuestsCompleted.Add(new QuestCompleted()
            {
                Dirty = true,
                Quest = quest
            });

            var progressToRemove = player.QuestProgress.Where(p => p.Requirement.Quest.Id == quest.Id);
            foreach (var progress in progressToRemove)
            {
                progress.Remove = true;
            }

            this.TakeRequirements(player, world);
            this.GiveRewards(npc, player, world);
        }

        private void GiveRewards(NPC npc, Player player, GameWorld world)
        {
            string prefix = "$7[Quest Reward]: ";
            foreach (var reward in quest.Rewards)
            {
                string rewardMessage = null;
                switch (reward.Type)
                {
                    case RewardType.Gold:
                        player.AddGold(reward.LongValue, world);
                        rewardMessage = string.Format("{0}{1} gold", prefix, reward.LongValue);
                        break;
                    case RewardType.Item:
                        int id = (int)reward.LongValue;
                        int stack = (int)reward.LongValue2;

                        ItemTemplate template = world.ItemHandler.GetTemplate(id);
                        if (template == null) continue;

                        Item item = new Item();
                        item.LoadFromTemplate(template);

                        world.ItemHandler.AddItem(item);

                        player.Inventory.AddItem(item, stack, world);

                        rewardMessage = string.Format("{0}Item: {1} ({2})", prefix, item.Name, stack);
                        break;
                    case RewardType.Title:
                        player.Title = reward.StringValue;
                        rewardMessage = string.Format("{0}Title: {1}", prefix, reward.StringValue);
                        break;
                    case RewardType.Surname:
                        player.Surname = reward.StringValue;
                        rewardMessage = string.Format("{0}Surname: {1}", prefix, reward.StringValue);
                        break;
                    case RewardType.Teleport:
                        string[] m = reward.StringValue.Split(',');
                        int mapId = 0;
                        int x = 0;
                        int y = 0;
                        if (!int.TryParse(m[0], out mapId))
                            continue;
                        if (!int.TryParse(m[1], out x))
                            continue;
                        if (!int.TryParse(m[2], out y))
                            continue;

                        Map map = world.MapHandler.GetMap(mapId);
                        if (map == null)
                            continue;

                        player.WarpTo(world, map, x, y);
                        rewardMessage = string.Format("{0}Teleport to {1}", prefix, map.Name);
                        break;
                    case RewardType.Experience:
                        player.AddExperience(reward.LongValue, world, Player.ExperienceMessage.None);
                        rewardMessage = string.Format("{0}{1} experience", prefix, reward.LongValue);
                        break;
                    case RewardType.FaceGraphic:
                        player.FaceID = (int)reward.LongValue;
                        player.SendCHPString(world);
                        rewardMessage = string.Format("{0}changed face", prefix);
                        break;
                    case RewardType.BodyGraphic:
                        player.BodyID = (int)reward.LongValue;
                        player.SendCHPString(world);
                        rewardMessage = string.Format("{0}changed body", prefix);
                        break;
                    case RewardType.HairGraphic:
                        player.HairID = (int)reward.LongValue;
                        player.SendCHPString(world);
                        rewardMessage = string.Format("{0}changed hair", prefix);
                        break;
                    case RewardType.HairColour:
                        int[] rgba = reward.StringValue.Split(',').Select(s => int.Parse(s)).ToArray();
                        player.HairR = rgba[0];
                        player.HairG = rgba[1];
                        player.HairB = rgba[2];
                        player.HairA = rgba[3];
                        player.SendCHPString(world);
                        rewardMessage = string.Format("{0}changed hair colour", prefix);
                        break;
                    case RewardType.BodyColour:
                        int[] brgba = reward.StringValue.Split(',').Select(s => int.Parse(s)).ToArray();
                        player.BodyR = brgba[0];
                        player.BodyG = brgba[1];
                        player.BodyB = brgba[2];
                        player.BodyA = brgba[3];
                        player.SendCHPString(world);
                        rewardMessage = string.Format("{0}changed body colour", prefix);
                        break;
                    case RewardType.ClassChange:
                        player.ChangeClass((int)reward.LongValue, world);
                        rewardMessage = string.Format("{0}Class changed to {1}", prefix, player.Class.ClassName);
                        break;
                    case RewardType.HP:
                        this.AddPlayerStats(new AttributeSet() { HP = (int)reward.LongValue }, player, world);
                        rewardMessage = string.Format("{0}{1} HP", prefix, reward.LongValue);
                        break;
                    case RewardType.MP:
                        this.AddPlayerStats(new AttributeSet() { MP = (int)reward.LongValue }, player, world);
                        rewardMessage = string.Format("{0}{1} MP", prefix, reward.LongValue);
                        break;
                    case RewardType.AC:
                        this.AddPlayerStats(new AttributeSet() { AC = (int)reward.LongValue }, player, world);
                        rewardMessage = string.Format("{0}{1} AC", prefix, reward.LongValue);
                        break;
                    case RewardType.Stamina:
                        this.AddPlayerStats(new AttributeSet() { Stamina = (int)reward.LongValue }, player, world);
                        rewardMessage = string.Format("{0}{1} Stamina", prefix, reward.LongValue);
                        break;
                    case RewardType.Strength:
                        this.AddPlayerStats(new AttributeSet() { Strength = (int)reward.LongValue }, player, world);
                        rewardMessage = string.Format("{0}{1} Strength", prefix, reward.LongValue);
                        break;
                    case RewardType.Dexterity:
                        this.AddPlayerStats(new AttributeSet() { Dexterity = (int)reward.LongValue }, player, world);
                        rewardMessage = string.Format("{0}{1} Dexterity", prefix, reward.LongValue);
                        break;
                    case RewardType.Intelligence:
                        this.AddPlayerStats(new AttributeSet() { Intelligence = (int)reward.LongValue }, player, world);
                        rewardMessage = string.Format("{0}{1} Intelligence", prefix, reward.LongValue);
                        break;
                    case RewardType.SpellBuff:
                        var spellEffect = world.SpellHandler.GetSpellEffect((int)reward.LongValue);
                        if (spellEffect == null)
                            continue;

                        spellEffect.Cast(npc, player, world);
                        rewardMessage = string.Format("{0}Spell Effect: {1}", prefix, spellEffect.Name);
                        break;
                    case RewardType.LearnSpell:
                        player.LearnSpell((int)reward.LongValue, world);
                        break;
                }

                if (rewardMessage != null)
                    world.Send(player, rewardMessage);
            }
        }

        private void AddPlayerStats(AttributeSet stats, Player player, GameWorld world)
        {
            player.RemoveStats(player.BaseStats, world);
            player.BaseStats += stats;
            player.AddStats(player.BaseStats, world);
        }

        /// <summary>
        /// Removes required items/gold/etc from player, if the requirement specifies
        /// </summary>
        /// <param name="player"></param>
        /// <param name="world"></param>
        private void TakeRequirements(Player player, GameWorld world)
        {
            foreach (var requirement in quest.Requirements)
            {
                if (!requirement.KeepRequirement)
                {
                    switch (requirement.Type)
                    {
                        case RequirementType.Gold:
                            player.RemoveGold(requirement.Value, world);
                            break;
                        case RequirementType.Item:
                            player.Inventory.RemoveItem((int)requirement.Value, requirement.Value2, world);
                            break;
                        case RequirementType.TalkToNPC:
                        case RequirementType.Kill:
                            break;
                        case RequirementType.ExperienceBanked:
                            player.AddExperience(-requirement.Value, world, Player.ExperienceMessage.None);
                            break;
                        case RequirementType.ExperienceSold:
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
