#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
using System;
using System.Globalization;
using Goose;
using Goose.CommunityGoals;
using Goose.Scripting;

public class HalloweenCandyGoal : BaseCommunityGoalScript
{
    public override void OnContributed(CommunityGoal goal, CommunityGoalRun run, Player? player, string key, long amount, GameWorld world)
    {
        run.Totals.TryGetValue("candy", out var total);
        CheckTiers(world, total);
    }

    public void CheckTiers(GameWorld world, long total)
    {
        var cfg = HalloweenConfig.Get(world);
        var tiers = cfg.Goal.Tiers;
        for (int i = 0; i < tiers.Length && i < 3; i++)
        {
            int tier = i + 1;
            if (total < tiers[i] || Halloween.Tier(world, tier)) continue;
            Halloween.SetTier(world, tier, true);
            if (tier == 1) Halloween.ApplyXpBonus(world);
            world.SendToAll(P.ServerMessage("[Event] " + Format(tiers[i]) + " candy collected! " + Unlock(tier, cfg)));
        }
    }

    public override string GetSummaryText(CommunityGoal goal, CommunityGoalRun run, GameWorld world)
    {
        var text = base.GetSummaryText(goal, run, world);
        var tiers = HalloweenConfig.Get(world).Goal.Tiers;
        for (int i = 0; i < tiers.Length && i < 3; i++)
            if (!Halloween.Tier(world, i + 1))
                return text + ". Next unlock at " + Format(tiers[i]);
        return text + ". All tiers unlocked";
    }

    private static string Unlock(int tier, HalloweenConfig cfg) => tier switch
    {
        1 => "The spirits grant +" + (int)Math.Round(cfg.XpBonus * 100) + "% experience for the rest of the event!",
        2 => "The basement door creaks open...",
        _ => "The party spills into town!",
    };

    private static string Format(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}

return typeof(HalloweenCandyGoal);
