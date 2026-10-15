namespace Goose.CommunityGoals
{
    public enum RankingKind
    {
        Score,
        Lottery
    }

    public sealed class RepeatRule
    {
        public TimeSpan? Every { get; }

        internal RepeatRule(TimeSpan? every)
        {
            Every = every;
        }
    }

    public static class Repeat
    {
        public static readonly RepeatRule OnClose = new(null);

        public static RepeatRule Every(TimeSpan every) => new(every);
    }

    public sealed class CommunityGoalDefinition
    {
        public IReadOnlyList<GoalRequirement> Requirements { get; init; } = [];
        public IReadOnlyDictionary<string, double> ScoreWeights { get; init; } = new Dictionary<string, double>(StringComparer.Ordinal);
        public RankingKind Ranking { get; init; }
        public IReadOnlyDictionary<string, long>? EntryCost { get; init; }
        public TimeSpan? Duration { get; init; }
        public DateTime? ClosesAt { get; init; }
        public RepeatRule? Repeat { get; init; }
        public TimeSpan? ClaimWindow { get; init; }
        public IReadOnlyList<GoalBracket> Brackets { get; init; } = [];
        public IReadOnlyList<RewardSpec> CommunityRewards { get; init; } = [];
        public int? MaxCompletions { get; init; }

        public bool HasTargets => Requirements.Any(r => r.HasTarget);
        public bool Repeats => Repeat is not null;
        public TimeSpan? EffectiveClaimWindow => Repeats ? ClaimWindow ?? TimeSpan.FromDays(30) : null;
    }
}
