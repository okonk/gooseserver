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
        public string ScriptPath { get; init; } = "";
        public string PropertiesJson { get; init; } = "";
        public Script<ICommunityGoalScript>? Script { get; set; }
        public CommunityGoalDefinition? Definition { get; set; }
    }
}
