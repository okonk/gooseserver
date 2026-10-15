using System.Text.Json.Nodes;

namespace Goose.CommunityGoals
{
    public sealed class CommunityRewardContext
    {
        public CommunityGoal Goal { get; init; } = null!;
        public CommunityGoalRun Run { get; init; } = null!;
        public Player? Player { get; init; }
        public NPC? Npc { get; init; }
        public GoalBracket? Bracket { get; init; }
        public JsonObject Params { get; init; } = new();
        public GameWorld World { get; init; } = null!;
    }
}
