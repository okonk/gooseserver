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
            QuestAlreadyClaimed,
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

            // At a TalkToNPC destination the description is the sender's text, so go straight to the turn-in.
            if (this.state == QuestWindowState.QuestDescription &&
                quest.Requirements.Any(r => r.Type == RequirementType.TalkToNPC && r.Value == npc.NPCTemplate.NPCTemplateID) &&
                (quest.Repeatable || !player.QuestsCompleted.Any(q => q.Id == quest.Id)) &&
                this.PlayerMeetsRequirements(player, world))
            {
                this.ResolveTurnIn(player, world);
            }

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

            var quests = GetAvailableQuests(npc, player, world);
            if (quests.Count == 0)
            {
                player.TalkedTo(npc, world);
                return;
            }

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

        internal static List<Quest> GetAvailableQuests(NPC npc, Player player, GameWorld world)
        {
            var available = new List<Quest>();

            foreach (var quest in npc.Quests)
            {
                if (player.QuestsCompleted.Any(q => q.Id == quest.Id) && !quest.Repeatable)
                    continue;

                if (world.QuestHandler.IsClaimedFor(quest, player) && !player.QuestsStarted.Any(q => q.Id == quest.Id))
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

            if (world.QuestHandler.IsClaimedFor(quest, player))
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
                case QuestWindowState.QuestAlreadyClaimed:
                    world.QuestHandler.TryGetClaim(quest.Id, out var claim);
                    var claimer = claim is not null ? world.PlayerHandler.GetPlayerName(claim.PlayerId) : null;
                    text = $"{claimer ?? "Someone"} has already completed this quest.\\nIt can only be completed once.";
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

                        this.ResolveTurnIn(player, world);
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

        private void ResolveTurnIn(Player player, GameWorld world)
        {
            this.Buttons = "0,1,0,0,0";

            if (this.PlayerMeetsRequirements(player, world))
            {
                if (world.QuestHandler.IsClaimedFor(quest, player))
                {
                    player.QuestsStarted.RemoveAll(q => q.Id == quest.Id);
                    player.QuestProgress.RemoveAll(p => p.Requirement.Quest.Id == quest.Id);
                    world.QuestHandler.RefreshIcons(player, world);
                    this.state = QuestWindowState.QuestAlreadyClaimed;
                }
                else if (!this.PlayerHasEnoughInventorySpaceForReward(player, world))
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

        public string GetQuestProgressText(Player player, GameWorld world)
            => GetQuestProgressText(this.quest, player, world);

        public static string GetQuestProgressText(Quest quest, Player player, GameWorld world)
        {
            string text = "Requirements\\n\\n";

            // Item rows render as one line per family with the summed requirement, so duplicate
            // rows cannot double-report progress; unresolved authored templates keep their own
            // fail-closed Unknown item line instead of merging into a valid family.
            var itemFamilies = new Dictionary<int, (string Name, long Required)>();
            foreach (var requirement in quest.Requirements.Where(r => r.Type == RequirementType.Item))
            {
                var template = world.ItemHandler.GetTemplate((int)requirement.Value);
                if (template is null) continue;

                var existing = itemFamilies.TryGetValue(template.BaseTemplateID, out var entry) ? entry : (Name: template.Name, Required: 0L);
                itemFamilies[template.BaseTemplateID] = (existing.Name, existing.Required + requirement.Value2);
            }

            var renderedFamilies = new HashSet<int>();

            foreach (var requirement in quest.Requirements.OrderBy(r => r.Type))
            {
                switch (requirement.Type)
                {
                    case RequirementType.Gold:
                        text += $"{requirement.Value:N0} gp\\n";
                        break;
                    case RequirementType.Item:
                        var item = world.ItemHandler.GetTemplate((int)requirement.Value);
                        if (item is null)
                        {
                            text += $"Unknown item (0/{requirement.Value2})\\n";
                            break;
                        }

                        if (!renderedFamilies.Add(item.BaseTemplateID)) continue;
                        var family = itemFamilies[item.BaseTemplateID];
                        long familyCount = player.Inventory.GetInventoryItemFamilyCount(item.BaseTemplateID);
                        text += $"{family.Name} ({familyCount}/{family.Required})\\n";
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
            => player.Inventory.GetNumberOfFreeSlots() >= QuestRewardGranter.RequiredInventorySlots(quest.Rewards, player, world);

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
            // Every family is preflighted against its full kept + consumed requirement before any
            // mutation: a short family aborts completion with quest, rewards, and inventory
            // untouched, and a pass guarantees kept rows still remain after the removal.
            var totals = GetItemFamilyTotalQuantities(quest, world);
            foreach (var (family, quantity) in totals)
            {
                if (player.Inventory.GetInventoryItemFamilyCount(family) < quantity)
                    return;
            }

            if (!player.QuestsCompleted.Any(q => q.Id == quest.Id))
            {
                player.QuestsCompleted.Add(quest);
            }

            if (quest.OnlyOnePlayerCanComplete && !world.QuestHandler.IsClaimed(quest.Id))
            {
                world.QuestHandler.Claim(quest, player, world);
                world.SendToAll(P.ServerMessage($"[World First] {quest.Name} has been completed by {player.Name}!"));
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
            foreach (var reward in quest.Rewards)
                QuestRewardGranter.Give(reward, npc, player, world);
        }

        internal static Dictionary<int, long> GetItemFamilyTotalQuantities(Quest quest, GameWorld world)
        {
            var quantities = new Dictionary<int, long>();
            foreach (var requirement in quest.Requirements)
            {
                if (requirement.Type != RequirementType.Item) continue;

                var template = world.ItemHandler.GetTemplate((int)requirement.Value);
                if (template is null) continue;

                quantities.TryGetValue(template.BaseTemplateID, out long total);
                quantities[template.BaseTemplateID] = total + requirement.Value2;
            }

            return quantities;
        }

        private static Dictionary<int, long> GetRemovableItemFamilyQuantities(Quest quest, GameWorld world)
        {
            var quantities = new Dictionary<int, long>();
            foreach (var requirement in quest.Requirements)
            {
                if (requirement.Type != RequirementType.Item || requirement.KeepRequirement) continue;

                var template = world.ItemHandler.GetTemplate((int)requirement.Value);
                if (template is null) continue;

                quantities.TryGetValue(template.BaseTemplateID, out long total);
                quantities[template.BaseTemplateID] = total + requirement.Value2;
            }

            return quantities;
        }

        /// <summary>
        /// Removes required items/gold/etc from player, if the requirement specifies
        /// </summary>
        /// <param name="player"></param>
        /// <param name="world"></param>
        private void TakeRequirements(Player player, GameWorld world)
        {
            var removable = GetRemovableItemFamilyQuantities(quest, world);

            foreach (var requirement in quest.Requirements)
            {
                if (!requirement.KeepRequirement)
                {
                    switch (requirement.Type)
                    {
                        case RequirementType.Gold:
                            player.RemoveGold(requirement.Value, world);
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

            foreach (var (family, quantity) in removable)
            {
                player.Inventory.TryRemoveInventoryItemFamily(family, quantity, world);
            }
        }
    }
}
