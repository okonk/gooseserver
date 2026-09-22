namespace Goose.Quests
{
    public enum QuestIconState
    {
        Ready,
        Available,
        None
    }

    internal static class QuestStateResolver
    {
        internal static bool IsActive(Quest quest, Player player)
        {
            if (!player.QuestsStarted.Any(q => q.Id == quest.Id))
                return false;

            // A completed non-repeatable quest stays in QuestsStarted historically; it is done.
            return !(player.QuestsCompleted.Any(q => q.Id == quest.Id) && !quest.Repeatable);
        }

        internal static bool MeetsMinimumGates(Quest quest, Player player)
        {
            return player.Level >= quest.MinLevel
                && player.Experience + player.ExperienceSold >= quest.MinExperience;
        }

        internal static bool MeetsStartRequirements(Quest quest, Player player)
        {
            return MeetsMinimumGates(quest, player)
                && (quest.MaxLevel <= 0 || player.Level <= quest.MaxLevel)
                && (quest.MaxExperience <= 0 || player.Experience + player.ExperienceSold <= quest.MaxExperience)
                && player.Class.CanUse(quest.ClassRestrictions);
        }

        internal static bool IsAvailable(Quest quest, Player player)
        {
            if (player.QuestsCompleted.Any(q => q.Id == quest.Id) && !quest.Repeatable)
                return false;

            return !IsActive(quest, player)
                && MeetsStartRequirements(quest, player)
                && quest.PrerequisiteQuests.All(prereq => player.QuestsCompleted.Any(q => q.Id == prereq));
        }

        internal static bool MeetsRequirements(Quest quest, Player player, GameWorld world)
        {
            foreach (var requirement in quest.Requirements)
            {
                if (!MeetsRequirement(requirement, player, world))
                    return false;
            }

            return true;
        }

        internal static QuestIconState Resolve(NPC npc, Player player, GameWorld world)
        {
            foreach (var quest in npc.Quests)
            {
                if (IsActive(quest, player) && MeetsRequirements(quest, player, world))
                    return QuestIconState.Ready;
            }

            foreach (var quest in npc.Quests)
            {
                if (IsAvailable(quest, player))
                    return QuestIconState.Available;
            }

            return QuestIconState.None;
        }

        private static bool MeetsRequirement(QuestRequirement requirement, Player player, GameWorld world)
        {
            switch (requirement.Type)
            {
                case RequirementType.Gold:
                    return player.Gold >= requirement.Value;
                case RequirementType.Item:
                    return player.Inventory.HasItem((int)requirement.Value, requirement.Value2);
                case RequirementType.TalkToNPC:
                case RequirementType.Kill:
                    return player.QuestProgress.Any(p => p.Requirement.Id == requirement.Id && p.Value >= p.Requirement.Value2);
                case RequirementType.ExperienceBanked:
                    return player.Experience >= requirement.Value;
                case RequirementType.ExperienceSold:
                    return player.ExperienceSold >= requirement.Value;
                case RequirementType.NothingEquipped:
                    foreach (Inventory.EquipSlots slot in Enum.GetValues(typeof(Inventory.EquipSlots)))
                    {
                        if (player.Inventory.GetEquippedSlot(slot) is not null)
                            return false;
                    }

                    return true;
                case RequirementType.Script:
                    return requirement.Script!.Object.IsMet(requirement, player, world);
                default:
                    return false;
            }
        }
    }
}
