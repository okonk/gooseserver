using Goose.CommunityGoals;

namespace Goose.Scripting
{
    public interface ICommunityRewardScript
    {
        string? CanGive(CommunityRewardContext ctx);
        int GetRequiredInventorySpace(CommunityRewardContext ctx);
        void Give(CommunityRewardContext ctx);
    }
}
