#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
using System;
using Goose;
using Goose.CommunityGoals;
using Goose.Scripting;

public class HalloweenXpBonus : BaseCommunityRewardScript
{
    public override void Give(CommunityRewardContext ctx)
    {
        var world = ctx.World;
        Halloween.ApplyXpBonus(world);
        int percent = (int)Math.Round(HalloweenConfig.Get(world).XpBonus * 100);
        world.SendToAll(P.ServerMessage("[Event] The spirits of the grove grant +" + percent + "% experience!"));
    }
}

return typeof(HalloweenXpBonus);
