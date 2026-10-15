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
        public IReadOnlyList<long>? Targets { get; init; }
        public double? Growth { get; init; }
        public int? ItemTemplateId { get; init; }
        public int? NpcTemplateId { get; init; }
        public string? Label { get; init; }

        public bool HasTarget => Target.HasValue || Targets is { Count: > 0 };

        public long? TargetAt(int level)
        {
            var steps = Targets is { Count: > 0 } ? Targets : Target is long single ? [single] : null;
            if (steps is null)
                return null;
            if (level < steps.Count)
                return steps[level];
            var last = steps[^1];
            if (Growth is not double growth)
                return last;
            var scaled = last * Math.Pow(growth, level - steps.Count + 1);
            return scaled >= long.MaxValue ? long.MaxValue : (long)scaled;
        }
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
