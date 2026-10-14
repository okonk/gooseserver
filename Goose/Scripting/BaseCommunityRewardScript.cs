using Goose.CommunityGoals;

namespace Goose.Scripting
{
    public class BaseCommunityRewardScript : ICommunityRewardScript
    {
        public virtual string? CanGive(CommunityRewardContext ctx) => null;
        public virtual int GetRequiredInventorySpace(CommunityRewardContext ctx) => 0;
        public virtual void Give(CommunityRewardContext ctx) { }
    }
}
