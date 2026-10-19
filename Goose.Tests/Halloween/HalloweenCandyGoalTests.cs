using System.Text.Json.Nodes;
using Goose;
using Goose.CommunityGoals;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenCandyGoalTests
{
    [Fact]
    public void Crossing_first_threshold_sets_tier1_and_applies_xp_bonus()
    {
        using var kit = Kit(out var listener);
        var script = kit.Compile<ICommunityGoalScript>("CommunityGoal", "HalloweenCandyGoal.csx").Object;

        Contribute(script, kit, 6000);

        Assert.True(Tier(kit, 1));
        Assert.False(Tier(kit, 2));
        Assert.Equal(1.5, kit.World.ExperienceModifier);
        Assert.Contains(listener.Sent, s => s.Contains("[Event]") && s.Contains("50%"));
    }

    [Fact]
    public void Below_threshold_unlocks_nothing()
    {
        using var kit = Kit(out _);
        var script = kit.Compile<ICommunityGoalScript>("CommunityGoal", "HalloweenCandyGoal.csx").Object;

        Contribute(script, kit, 5999);

        Assert.False(Tier(kit, 1));
        Assert.Equal(1.0, kit.World.ExperienceModifier);
    }

    [Fact]
    public void Single_contribution_crossing_two_thresholds_sets_both()
    {
        using var kit = Kit(out _);
        var script = kit.Compile<ICommunityGoalScript>("CommunityGoal", "HalloweenCandyGoal.csx").Object;

        Contribute(script, kit, 20000);

        Assert.True(Tier(kit, 1));
        Assert.True(Tier(kit, 2));
        Assert.False(Tier(kit, 3));
    }

    [Fact]
    public void Already_set_tier_is_not_reannounced()
    {
        using var kit = Kit(out var listener);
        kit.World.CommunityGoals.SetFlag("halloween.tier1", 1, null);
        var script = kit.Compile<ICommunityGoalScript>("CommunityGoal", "HalloweenCandyGoal.csx").Object;

        Contribute(script, kit, 7000);

        Assert.DoesNotContain(listener.Sent, s => s.Contains("[Event]"));
    }

    [Fact]
    public void Summary_names_the_next_unlock()
    {
        using var kit = Kit(out _);
        kit.World.CommunityGoals.SetFlag("halloween.tier1", 1, null);
        var script = kit.Compile<ICommunityGoalScript>("CommunityGoal", "HalloweenCandyGoal.csx").Object;
        var goal = Goal();
        var run = new CommunityGoalRun { GoalId = 2, Run = 1, StartedAt = DateTime.UtcNow };
        run.Totals["candy"] = 7000;

        var text = script.GetSummaryText(goal, run, kit.World);

        Assert.Contains("7,000", text);
        Assert.Contains("Next unlock at 20,000", text);
    }

    [Fact]
    public void Descending_tiers_disable_the_config()
    {
        using var kit = Kit(out _);
        kit.SetConfig(json => json["goal"] = new JsonObject { ["tiers"] = new JsonArray(6000, 5000) });
        var script = kit.Compile<ICommunityGoalScript>("CommunityGoal", "HalloweenCandyGoal.csx").Object;

        Contribute(script, kit, 10000);

        Assert.False(Tier(kit, 1));
    }

    private static HalloweenTestKit Kit(out TestWorldFixture.CapturingPlayer listener)
    {
        var kit = new HalloweenTestKit();
        kit.SetConfig(json => json["goal"] = new JsonObject { ["tiers"] = new JsonArray(6000, 20000, 45000) });
        listener = kit.Fixture.CommandPlayerOn(kit.Fixture.AddBaseMap(38, "Grove", 40, 40), 1, 1);
        kit.Fixture.AddOnlinePlayer(listener);
        return kit;
    }

    private static CommunityGoal Goal()
    {
        const string props = "{\"requirements\":[{\"key\":\"candy\",\"type\":\"Item\",\"item\":90001,\"label\":\"Candy\"}]}";
        return new CommunityGoal
        {
            Id = 2, Name = "The Candy Count", PropertiesJson = props,
            Definition = CommunityGoalDefinitionParser.Parse(props),
        };
    }

    private static void Contribute(ICommunityGoalScript script, HalloweenTestKit kit, long total)
    {
        var run = new CommunityGoalRun { GoalId = 2, Run = 1, StartedAt = DateTime.UtcNow };
        run.Totals["candy"] = total;
        script.OnContributed(Goal(), run, null, "candy", total, kit.World);
    }

    private static bool Tier(HalloweenTestKit kit, int n) => kit.World.CommunityGoals.GetFlag("halloween.tier" + n) != 0;
}
