namespace Goose.CommunityGoals
{
    public sealed class Contribution
    {
        public int PlayerId { get; init; }
        public Dictionary<string, long> Amounts { get; } = new();
        public DateTime LastAt { get; set; }
        public int? ClaimedBracket { get; set; }
        public DateTime? ClaimedAt { get; set; }
    }
}
