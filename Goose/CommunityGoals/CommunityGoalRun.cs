using System.Text.Json.Nodes;

namespace Goose.CommunityGoals
{
    public enum RunStatus
    {
        Active,
        Completed,
        Failed
    }

    public sealed class CommunityGoalRun
    {
        public int GoalId { get; init; }
        public int Run { get; init; }
        public RunStatus Status { get; set; } = RunStatus.Active;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Dictionary<string, long> Totals { get; } = new();
        public JsonObject? Results { get; set; }
        public Dictionary<int, Contribution> Contributions { get; } = new();
    }
}
