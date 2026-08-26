using System.Text;
using Goose.Quests;

namespace Goose.Scripting
{
    /// <summary>Hooks for RequirementType.Script and RewardType.Script rows. One interface covers
    /// both roles so a single script file can implement a paired requirement + reward behaviour;
    /// a script only interested in one role inherits the other's no-ops from BaseQuestScript.
    ///
    /// IMPORTANT: ScriptHandler caches ONE instance per file path, shared by every row pointing at
    /// that file (ScriptHandler.cs:20-30). ScriptParams is per-ROW. Deserialize
    /// requirement.ScriptParams / reward.ScriptParams inside each call — never cache it in a field
    /// between calls, or a second row using the same script will read the first row's params.</summary>
    public interface IQuestScript
    {
        // Requirement role
        bool IsMet(QuestRequirement requirement, Player player, GameWorld world);
        string GetProgressText(QuestRequirement requirement, Player player, GameWorld world);
        void OnTakeRequirement(QuestRequirement requirement, Player player, GameWorld world);

        // Reward role
        /// <summary>null or empty to allow completion; otherwise the message shown to the player
        /// instead of completing the quest. Supports \n the same way quest Description does.</summary>
        string? CanComplete(QuestReward reward, Player player, GameWorld world);
        void GiveReward(QuestReward reward, NPC npc, Player player, GameWorld world);
    }
}
