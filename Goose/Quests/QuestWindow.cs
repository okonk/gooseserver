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
            QuestNoSpellbookSpace,
            QuestNotRightLevel,
            QuestProgress,
            QuestScriptCannotComplete,
        }

        private Quest quest;
        private QuestWindowState state;
        private string scriptCannotCompleteMessage = null!;

        public QuestWindow(NPC npc, Player player, Quest quest, GameWorld world)
        {
            this.ID = ++player.LastWindowID;
            this.Title = quest.Name;
            this.Buttons = "0,1,0,1,0";
            this.Frame = WindowFrames.Quest;
            this.Type = WindowTypes.Quest;
            this.NPC = npc;
            this.quest = quest;

            if (!QuestStateResolver.MeetsMinimumGates(quest, player))
                this.state = QuestWindowState.QuestNotRightLevel;
            else
                this.state = QuestWindowState.QuestDescription;

            player.Windows.Add(this);
            player.TalkedTo(npc, world);

            this.SendCreate(player, world);
        }

        /// <summary>
        /// Called when a player left clicks a quest npc
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <param name="world"></param>
        public static void Handle(NPC npc, Player player, GameWorld world)
        {
            foreach (var w in player.Windows.Where(w => (w.Type == WindowTypes.Quest || w.Type == WindowTypes.OptionList) && w.NPC == npc).ToList())
                w.Close(player, world);

            var quests = GetAvailableQuests(npc, player);
            if (quests.Count == 0) return;

            if (quests.Count == 1)
            {
                StartQuest(quests[0], player, world);
                new QuestWindow(npc, player, quests[0], world);
            }
            else
            {
                player.TalkedTo(npc, world);
                new OptionListWindow(player, world, npc.Name,
                    quests.Select(q => q.Name).ToList(),
                    (line, p, w) =>
                    {
                        var quest = quests[line];
                        QuestWindow.StartQuest(quest, p, w);
                        new QuestWindow(npc, p, quest, w);
                    },
                    npc,
                    openingLine: "Welcome, adventurer!");
            }
        }

        internal static List<Quest> GetActiveQuests(Player player)
        {
            return player.QuestsStarted
                .Where(q => !(player.QuestsCompleted.Any(c => c.Id == q.Id) && !q.Repeatable))
                .ToList();
        }

        internal static string? FindGrantingNpc(GameWorld world, int questId)
        {
            foreach (var template in world.NPCHandler.GetTemplates())
            {
                if (template.Quests.Any(q => q.Id == questId))
                    return template.Name;
            }

            return null;
        }

        internal static List<Quest> GetAvailableQuests(NPC npc, Player player)
        {
            var available = new List<Quest>();

            foreach (var quest in npc.Quests)
            {
                if (player.QuestsCompleted.Any(q => q.Id == quest.Id) && !quest.Repeatable)
                    continue;

                if (quest.PrerequisiteQuests.Any(prereq => !player.QuestsCompleted.Any(q => q.Id == prereq)))
                    continue;

                if ((quest.MaxLevel > 0 && player.Level > quest.MaxLevel) || (quest.MaxExperience > 0 && player.Experience + player.ExperienceSold > quest.MaxExperience))
                    continue;

                if (!player.Class.CanUse(quest.ClassRestrictions))
                    continue;

                available.Add(quest);
            }

            return available;
        }

        internal static void StartQuest(Quest quest, Player player, GameWorld world)
        {
            if (player.QuestsStarted.Any(q => q.Id == quest.Id))
                return;

            player.QuestsStarted.Add(quest);

            foreach (var requirement in quest.Requirements)
            {
                if (requirement.Type == RequirementType.Kill || requirement.Type == RequirementType.TalkToNPC)
                {
                    if (!player.QuestProgress.Any(q => q.Requirement.Id == requirement.Id))
                    {
                        player.QuestProgress.Add(new QuestProgress() { Requirement = requirement, Value = 0 });
                    }
                }
            }

            world.QuestHandler.RefreshIcons(player, world);
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

        /// <summary>The window text for the current state. Internal so tests can read exactly what
        /// the production control flow in Clicked selected, without exposing the private state or
        /// the blocking-message helper.</summary>
        internal string GetCurrentText(Player player, GameWorld world)
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
                case QuestWindowState.QuestNotRightLevel:
                    text = "You don't meet the level or experience \\nrequirements for this quest.";
                    if (quest.MinLevel > 0)
                        text += $"\\nLevel {quest.MinLevel} required.";
                    if (quest.MinExperience > 0)
                        text += $"\\n{Utils.FormatNumber(quest.MinExperience)} experience required.";
                    text += "\\nCome back to me when you're stronger.";
                    break;
                case QuestWindowState.QuestProgress:
                    text = GetQuestProgressText(player, world);
                    break;
                case QuestWindowState.QuestScriptCannotComplete:
                    text = this.scriptCannotCompleteMessage;
                    break;
            }

            return text;
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
                    if (this.state == QuestWindowState.QuestDescription)
                    {
                        this.Buttons = "0,1,0,0,0";

                        // player has opened the quest window twice, and already completed it in one
                        if (!quest.Repeatable && player.QuestsCompleted.Any(q => q.Id == quest.Id))
                        {
                            this.Close(player, world);
                            return;
                        }

                        if (this.PlayerMeetsRequirements(player, world))
                        {
                            if (!this.PlayerHasEnoughInventorySpaceForReward(player, world))
                            {
                                this.state = QuestWindowState.QuestNoInventorySpace;
                            }
                            else if (!this.PlayerHasEnoughSpellbookSpaceForReward(player))
                            {
                                this.state = QuestWindowState.QuestNoSpellbookSpace;
                            }
                            else if (this.GetScriptCannotCompleteMessage(player, world) is string cannotComplete)
                            {
                                this.scriptCannotCompleteMessage = cannotComplete;
                                this.state = QuestWindowState.QuestScriptCannotComplete;
                            }
                            else
                            {
                                this.state = QuestWindowState.QuestPass;
                                this.CompleteQuest(this.NPC!, player, world);
                            }
                        }
                        else
                        {
                            this.state = QuestWindowState.QuestFail;

                            if (this.quest.ShowProgress)
                                this.Buttons = "0,1,0,1,0";
                        }
                    }
                    else if (this.state == QuestWindowState.QuestFail && this.quest.ShowProgress)
                    {
                        this.Buttons = "0,1,0,0,0";
                        this.state = QuestWindowState.QuestProgress;
                    }

                    this.SendCreate(player, world);
                    break;
                default:
                    player.Windows.Remove(this);
                    break;
            }
        }

        public string GetQuestProgressText(Player player, GameWorld world)
            => GetQuestProgressText(this.quest, player, world);

        public static string GetQuestProgressText(Quest quest, Player player, GameWorld world)
        {
            string text = "Requirements\\n\\n";

            foreach (var requirement in quest.Requirements.OrderBy(r => r.Type))
            {
                switch (requirement.Type)
                {
                    case RequirementType.Gold:
                        text += $"{requirement.Value:N0} gp\\n";
                        break;
                    case RequirementType.Item:
                        var item = world.ItemHandler.GetTemplate((int)requirement.Value);
                        text += $"{(item?.Name ?? "Unknown item")} ({requirement.Value2})\\n";
                        break;
                    case RequirementType.TalkToNPC:
                        var talkNPC = world.NPCHandler.GetNPCTemplate((int)requirement.Value);
                        long talkNPCProgress = player.QuestProgress.FirstOrDefault(p => p.Requirement.Id == requirement.Id)?.Value ?? 0;
                        text += $"Talk to {(talkNPC?.Name ?? "Unknown NPC")} ({talkNPCProgress}/{requirement.Value2})\\n";
                        break;
                    case RequirementType.Kill:
                        var killNpc = world.NPCHandler.GetNPCTemplate((int)requirement.Value);
                        long killNpcProgress = player.QuestProgress.FirstOrDefault(p => p.Requirement.Id == requirement.Id)?.Value ?? 0;
                        text += $"Kill {(killNpc?.Name ?? "Unknown NPC")} ({killNpcProgress:N0}/{requirement.Value2:N0})\\n";
                        break;
                    case RequirementType.ExperienceBanked:
                        text += $"{requirement.Value:N0} xp banked\\n";
                        break;
                    case RequirementType.ExperienceSold:
                        text += $"{requirement.Value:N0} xp sold\\n";
                        break;
                    case RequirementType.NothingEquipped:
                        text += "Have no items equipped\\n";
                        break;
                    case RequirementType.Script:
                        var scriptText = requirement.Script!.Object.GetProgressText(requirement, player, world);
                        if (!string.IsNullOrEmpty(scriptText))
                            text += scriptText + "\\n";
                        break;
                }
            }

            return text;
        }

        public bool PlayerMeetsRequirements(Player player, GameWorld world)
            => QuestStateResolver.MeetsRequirements(this.quest, player, world);

        private bool PlayerHasEnoughSpellbookSpaceForReward(Player player)
        {
            var spellRewards = quest.Rewards.Where(r => r.Type == RewardType.LearnSpell);
            var freeSlots = player.Spellbook.GetNumberOfFreeSlots();

            return freeSlots >= spellRewards.Count();
        }

        private bool PlayerHasEnoughInventorySpaceForReward(Player player, GameWorld world)
        {
            // assume 1 slot per reward item
            var requiredSlots = quest.Rewards.Count(r => r.Type == RewardType.Item);

            // A script reporting 0 or less cannot loosen the gate: AddItem returns false when it
            // finds no room and GiveRewards ignores the result, so the reward would be dropped
            // after the quest was already marked complete.
            foreach (var reward in quest.Rewards.Where(r => r.Type == RewardType.Script))
            {
                var required = reward.Script?.Object.GetRequiredInventorySpace(reward, player, world) ?? 0;
                if (required > 0) requiredSlots += required;
            }

            return player.Inventory.GetNumberOfFreeSlots() >= requiredSlots;
        }

        /// <summary>The first blocking message from a Script reward, or null if every scripted reward
        /// allows completion. Called before CompleteQuest so a block leaves player state untouched.</summary>
        private string? GetScriptCannotCompleteMessage(Player player, GameWorld world)
        {
            foreach (var reward in this.quest.Rewards.Where(r => r.Type == RewardType.Script))
            {
                var message = reward.Script!.Object.CanComplete(reward, player, world);
                if (!string.IsNullOrEmpty(message)) return message;
            }

            return null;
        }

        public void CompleteQuest(NPC npc, Player player, GameWorld world)
        {
            if (!player.QuestsCompleted.Any(q => q.Id == quest.Id))
            {
                player.QuestsCompleted.Add(quest);
            }

            // A completed repeatable quest is done until the player re-accepts it at the NPC.
            // Its progress is dropped too: kills while inactive would still credit it.
            if (quest.Repeatable)
            {
                player.QuestsStarted.RemoveAll(q => q.Id == quest.Id);
            }

            player.QuestProgress.RemoveAll(p => p.Requirement.Quest.Id == quest.Id);

            this.TakeRequirements(player, world);
            this.GiveRewards(npc, player, world);

            world.QuestHandler.RefreshIcons(player, world);
        }

        private void GiveRewards(NPC npc, Player player, GameWorld world)
        {
            string prefix = "[Quest Reward]: ";
            foreach (var reward in quest.Rewards)
            {
                string? rewardMessage = null;
                switch (reward.Type)
                {
                    case RewardType.Gold:
                        player.AddGold(reward.LongValue, world);
                        rewardMessage = $"{prefix}{reward.LongValue} gold";
                        break;
                    case RewardType.Item:
                        int id = (int)reward.LongValue;
                        int stack = (int)reward.LongValue2;

                        ItemTemplate? template = world.ItemHandler.GetTemplate(id);
                        if (template is null) continue;

                        Item item = new Item();
                        if (!item.LoadFromTemplate(template)) continue;
                        world.ItemHandler.RollTitleAndSurname(item, world);
                        world.ItemHandler.AddAndAssignId(item, world);

                        player.Inventory.AddItem(item, stack, world);

                        rewardMessage = $"{prefix}Item: {item.Name} ({stack})";
                        break;
                    case RewardType.Title:
                        player.Title = reward.StringValue;
                        rewardMessage = $"{prefix}Title: {reward.StringValue}";
                        break;
                    case RewardType.Surname:
                        player.Surname = reward.StringValue;
                        rewardMessage = $"{prefix}Surname: {reward.StringValue}";
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

                        Map? map = world.MapHandler.GetMap(mapId);
                        if (map is null)
                            continue;

                        player.WarpTo(world, map, x, y);
                        rewardMessage = $"{prefix}Teleport to {map.Name}";
                        break;
                    case RewardType.Experience:
                        player.AddExperience(reward.LongValue, world, Player.ExperienceMessage.None);
                        rewardMessage = $"{prefix}{reward.LongValue} experience";
                        break;
                    case RewardType.FaceGraphic:
                        player.FaceID = (int)reward.LongValue;
                        player.SendCHPString(world);
                        rewardMessage = $"{prefix}changed face";
                        break;
                    case RewardType.BodyGraphic:
                        player.BodyID = (int)reward.LongValue;
                        player.SendCHPString(world);
                        rewardMessage = $"{prefix}changed body";
                        break;
                    case RewardType.HairGraphic:
                        player.HairID = (int)reward.LongValue;
                        player.SendCHPString(world);
                        rewardMessage = $"{prefix}changed hair";
                        break;
                    case RewardType.HairColour:
                        int[] rgba = reward.StringValue.Split(',').Select(s => int.Parse(s)).ToArray();
                        player.HairR = rgba[0];
                        player.HairG = rgba[1];
                        player.HairB = rgba[2];
                        player.HairA = rgba[3];
                        player.SendCHPString(world);
                        rewardMessage = $"{prefix}changed hair colour";
                        break;
                    case RewardType.BodyColour:
                        int[] brgba = reward.StringValue.Split(',').Select(s => int.Parse(s)).ToArray();
                        player.BodyR = brgba[0];
                        player.BodyG = brgba[1];
                        player.BodyB = brgba[2];
                        player.BodyA = brgba[3];
                        player.SendCHPString(world);
                        rewardMessage = $"{prefix}changed body colour";
                        break;
                    case RewardType.ClassChange:
                        player.ChangeClass((int)reward.LongValue, 5, world);
                        rewardMessage = $"{prefix}Class changed to {player.Class.ClassName}";
                        break;
                    case RewardType.HP:
                        this.AddPlayerStats(new AttributeSet() { HP = reward.LongValue }, player, world);
                        rewardMessage = $"{prefix}{reward.LongValue} HP";
                        break;
                    case RewardType.MP:
                        this.AddPlayerStats(new AttributeSet() { MP = reward.LongValue }, player, world);
                        rewardMessage = $"{prefix}{reward.LongValue} MP";
                        break;
                    case RewardType.AC:
                        this.AddPlayerStats(new AttributeSet() { AC = (int)reward.LongValue }, player, world);
                        rewardMessage = $"{prefix}{reward.LongValue} AC";
                        break;
                    case RewardType.Stamina:
                        this.AddPlayerStats(new AttributeSet() { Stamina = (int)reward.LongValue }, player, world);
                        rewardMessage = $"{prefix}{reward.LongValue} Stamina";
                        break;
                    case RewardType.Strength:
                        this.AddPlayerStats(new AttributeSet() { Strength = (int)reward.LongValue }, player, world);
                        rewardMessage = $"{prefix}{reward.LongValue} Strength";
                        break;
                    case RewardType.Dexterity:
                        this.AddPlayerStats(new AttributeSet() { Dexterity = (int)reward.LongValue }, player, world);
                        rewardMessage = $"{prefix}{reward.LongValue} Dexterity";
                        break;
                    case RewardType.Intelligence:
                        this.AddPlayerStats(new AttributeSet() { Intelligence = (int)reward.LongValue }, player, world);
                        rewardMessage = $"{prefix}{reward.LongValue} Intelligence";
                        break;
                    case RewardType.SpellBuff:
                        var spellEffect = world.SpellHandler.GetSpellEffect((int)reward.LongValue);
                        if (spellEffect is null)
                            continue;

                        spellEffect.Cast(npc, player, world);
                        rewardMessage = $"{prefix}Spell Effect: {spellEffect.Name}";
                        break;
                    case RewardType.LearnSpell:
                        player.LearnSpell((int)reward.LongValue, world);
                        break;
                    case RewardType.Script:
                        reward.Script!.Object.GiveReward(reward, npc, player, world);
                        break;
                }

                if (rewardMessage is not null)
                    world.Send(player, P.ServerMessage(rewardMessage));
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
                            var progress = player.QuestProgress.FirstOrDefault(p => p.Requirement.Quest.Id == quest.Id
                                && p.Requirement.Type == requirement.Type
                                && p.Requirement.Value == requirement.Value
                                && p.Requirement.Value2 == requirement.Value2);
                            if (progress is not null)
                            {
                                progress.Value = Math.Max(0, progress.Value - requirement.Value2);
                            }
                            break;
                        case RequirementType.ExperienceBanked:
                            player.AddExperience(-requirement.Value, world, Player.ExperienceMessage.None);
                            break;
                        case RequirementType.ExperienceSold:
                            break;
                        case RequirementType.Script:
                            requirement.Script!.Object.OnTakeRequirement(requirement, player, world);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
