namespace Goose.CommunityGoals
{
    public readonly record struct ClaimResult(bool Success, string? Refusal)
    {
        public static ClaimResult Ok() => new(true, null);
        public static ClaimResult Refuse(string refusal) => new(false, refusal);
    }

    public record PendingPrize(CommunityGoal Goal, CommunityGoalRun Run, int BracketIndex);
}
