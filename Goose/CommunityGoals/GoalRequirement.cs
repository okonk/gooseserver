namespace Goose.CommunityGoals
{
    public enum RequirementKind
    {
        Item,
        Gold,
        Experience,
        Kill,
        Custom
    }

    public sealed class GoalRequirement
    {
        public string Key { get; init; } = "";
        public RequirementKind Kind { get; init; }
        public long? Target { get; init; }
        public int? ItemTemplateId { get; init; }
        public int? NpcTemplateId { get; init; }
        public string? Label { get; init; }
    }

    public static class Requirement
    {
        public static GoalRequirement Item(string key, int itemId, long? target = null)
            => new() { Key = key, Kind = RequirementKind.Item, ItemTemplateId = itemId, Target = target };

        public static GoalRequirement Gold(string key, long? target = null)
            => new() { Key = key, Kind = RequirementKind.Gold, Target = target };

        public static GoalRequirement Experience(string key, long? target = null)
            => new() { Key = key, Kind = RequirementKind.Experience, Target = target };

        public static GoalRequirement Kill(string key, int npcId, long? target = null)
            => new() { Key = key, Kind = RequirementKind.Kill, NpcTemplateId = npcId, Target = target };

        public static GoalRequirement Custom(string key, string? label = null, long? target = null)
            => new() { Key = key, Kind = RequirementKind.Custom, Label = label, Target = target };
    }
}
