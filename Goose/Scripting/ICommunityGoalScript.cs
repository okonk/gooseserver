using Goose.CommunityGoals;

namespace Goose.Scripting
{
    public interface ICommunityGoalScript
    {
        CommunityGoalDefinition Define(CommunityGoal goal, GameWorld world);
        string? CanContribute(CommunityGoal goal, CommunityGoalRun run, Player player, string key, long amount, GameWorld world);
        void OnContributed(CommunityGoal goal, CommunityGoalRun run, Player? player, string key, long amount, GameWorld world);
        bool IsComplete(CommunityGoal goal, CommunityGoalRun run, GameWorld world);
        void OnCompleted(CommunityGoal goal, CommunityGoalRun run, GameWorld world);
        string GetSummaryText(CommunityGoal goal, CommunityGoalRun run, GameWorld world);
        GoalBracket? GetBracket(CommunityGoal goal, CommunityGoalRun run, Player player, GameWorld world);
    }
}
