using Goose.Scripting;

namespace Goose.CommunityGoals
{
    public sealed class CommunityGoal
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public int NpcTemplateId { get; init; }
        public List<int> PrerequisiteGoals { get; } = [];
        // Raw prerequisite_goals column; parsed per-goal so a malformed value skips only that goal.
        public string? PrerequisiteGoalsRaw { get; init; }
        public string ScriptPath { get; init; } = "";
        public string PropertiesJson { get; set; } = "";
        public Script<ICommunityGoalScript>? Script { get; set; }
        // The script object that produced Definition; swapped only when a (re)definition succeeds.
        public ICommunityGoalScript? ResolvedScript { get; set; }
        public CommunityGoalDefinition? Definition { get; set; }
    }
}
