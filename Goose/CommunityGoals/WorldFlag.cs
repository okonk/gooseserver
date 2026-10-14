namespace Goose.CommunityGoals
{
    public sealed class WorldFlag
    {
        public string Name { get; init; } = "";
        public int Value { get; set; }
        public DateTime SetAt { get; set; }
        public int? SourceGoalId { get; set; }
    }
}
