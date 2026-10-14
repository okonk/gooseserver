using System.Text.Json.Nodes;

namespace Goose.CommunityGoals
{
    public sealed class RewardSpec
    {
        public string Type { get; init; } = "";
        public JsonObject Params { get; init; } = new();
    }
}
