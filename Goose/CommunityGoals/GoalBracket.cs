namespace Goose.CommunityGoals
{
    public sealed class GoalBracket
    {
        public int? Rank { get; init; }
        public double? MinScore { get; init; }
        public string? Measure { get; init; }
        public IReadOnlyList<RewardSpec> Rewards { get; init; } = [];
    }

    public static class Bracket
    {
        public static GoalBracket Rank(int rank, IReadOnlyList<RewardSpec> rewards)
            => new() { Rank = rank, Rewards = rewards };

        public static GoalBracket MinScore(double minScore, IReadOnlyList<RewardSpec> rewards, string? measure = null)
            => new() { MinScore = minScore, Rewards = rewards, Measure = measure };
    }
}
