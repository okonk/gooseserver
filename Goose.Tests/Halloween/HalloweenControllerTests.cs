using System.Reflection;
using Goose;
using Goose.CommunityGoals;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenControllerTests
{
    [Fact]
    public void Xp_bonus_reward_sets_world_modifier_to_base_plus_bonus()
    {
        using var kit = new HalloweenTestKit();
        var listener = kit.Fixture.CommandPlayerOn(kit.Fixture.AddBaseMap(38, "Grove", 40, 40), 1, 1);
        kit.Fixture.AddOnlinePlayer(listener);

        var reward = kit.Compile<ICommunityRewardScript>("CommunityGoal", "HalloweenXpBonus.csx");
        reward.Object.Give(RewardContext(kit));

        Assert.Equal(1.5, kit.World.ExperienceModifier);
        Assert.Contains(listener.Sent, s => s.Contains("[Event]") && s.Contains("50%"));
    }

    [Fact]
    public void Granting_xp_bonus_twice_does_not_stack()
    {
        using var kit = new HalloweenTestKit();
        var reward = kit.Compile<ICommunityRewardScript>("CommunityGoal", "HalloweenXpBonus.csx");

        reward.Object.Give(RewardContext(kit));
        reward.Object.Give(RewardContext(kit));

        Assert.Equal(1.5, kit.World.ExperienceModifier);
    }

    [Fact]
    public void Startup_applies_bonus_when_tier1_flag_is_set()
    {
        using var kit = new HalloweenTestKit();
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.World.CommunityGoals.SetFlag("halloween.tier1", 1, null);

        var controller = kit.Compile<IGlobalScript>("Global", "Halloween.csx");
        controller.Object.OnLoaded(kit.World);
        FirePendingScriptTimers(kit.World);

        Assert.Equal(1.5, kit.World.ExperienceModifier);
    }

    [Fact]
    public void Startup_leaves_base_without_tier1()
    {
        using var kit = new HalloweenTestKit();
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);

        var controller = kit.Compile<IGlobalScript>("Global", "Halloween.csx");
        controller.Object.OnLoaded(kit.World);
        FirePendingScriptTimers(kit.World);

        Assert.Equal(1.0, kit.World.ExperienceModifier);
    }

    [Fact]
    public void Off_evicts_players_from_house_and_basement_to_grove_but_not_gms()
    {
        using var kit = new HalloweenTestKit();
        var house = kit.Fixture.AddBaseMap(48, "House", 40, 40);
        var basement = kit.Fixture.AddBaseMap(39, "Basement", 40, 40);
        kit.Fixture.AddBaseMap(38, "Grove", 40, 40);
        var player = kit.Fixture.CommandPlayerOn(house, 5, 5);
        house.AddPlayer(player, kit.World);
        var basementPlayer = kit.Fixture.CommandPlayerOn(basement, 5, 5);
        basement.AddPlayer(basementPlayer, kit.World);
        var gm = kit.Fixture.CommandPlayerOn(house, 6, 6, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        house.AddPlayer(gm, kit.World);

        var controller = kit.Compile<IGlobalScript>("Global", "Halloween.csx");
        controller.Object.OnLoaded(kit.World);
        Assert.True(kit.Fixture.RunCommand(gm, "/halloween off"));

        Assert.Equal(38, player.MapID);
        Assert.Equal((38, 37), (player.MapX, player.MapY));
        Assert.Contains(player.Sent, s => s.Contains("The party is over. The door closes behind you."));
        Assert.Equal(38, basementPlayer.MapID);
        Assert.Equal(48, gm.MapID);
        Assert.DoesNotContain(gm.Sent, s => s.Contains("The party is over"));

        var latecomer = kit.Fixture.CommandPlayerOn(house, 7, 7, "Late");
        house.AddPlayer(latecomer, kit.World);
        FirePendingScriptTimers(kit.World);
        Assert.Equal(38, latecomer.MapID);
    }

    [Fact]
    public void Halloween_command_on_off_tier_status()
    {
        using var kit = new HalloweenTestKit();
        var house = kit.Fixture.AddBaseMap(48, "House", 40, 40);
        var basement = kit.Fixture.AddBaseMap(39, "Basement", 40, 40);
        var grove = kit.Fixture.AddBaseMap(38, "Grove", 40, 40);
        var gm = kit.Fixture.CommandPlayerOn(house, 1, 1, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        house.AddPlayer(gm, kit.World);

        var controller = kit.Compile<IGlobalScript>("Global", "Halloween.csx");
        controller.Object.OnLoaded(kit.World);

        Assert.True(kit.Fixture.RunCommand(gm, "/halloween on"));
        Assert.Equal(1, kit.World.CommunityGoals.GetFlag("halloween.active"));
        Assert.Contains(gm.Sent, s => s.Contains("Halloween event is on."));

        Assert.True(kit.Fixture.RunCommand(gm, "/halloween tier 1 1"));
        Assert.Equal(1, kit.World.CommunityGoals.GetFlag("halloween.tier1"));

        Assert.True(kit.Fixture.RunCommand(gm, "/halloween tier 4 1"));
        Assert.Equal(0, kit.World.CommunityGoals.GetFlag("halloween.tier4"));
        Assert.Contains(gm.Sent, s => s.Contains("Usage: /halloween tier"));

        Assert.True(kit.Fixture.RunCommand(gm, "/halloween tier 1 0"));
        Assert.Equal(0, kit.World.CommunityGoals.GetFlag("halloween.tier1"));

        gm.ScriptState["hw.cd.ghosts"] = 123;
        gm.ScriptState["hw.grace.stalker"] = 456;
        gm.ScriptState["unrelated"] = 789;
        Assert.True(kit.Fixture.RunCommand(gm, "/halloween resetcd"));
        Assert.Equal(0, gm.ScriptState.Keys.Count(k => k.StartsWith("hw.")));
        Assert.Equal(789, gm.ScriptState["unrelated"]);

        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.World.CommunityGoals.SetFlag("halloween.tier2", 1, null);
        Assert.True(kit.Fixture.RunCommand(gm, "/halloween status"));
        Assert.Contains(gm.Sent, s => s.Contains("Halloween: active"));
        Assert.Contains(gm.Sent, s => s.Contains("1=off") && s.Contains("2=on") && s.Contains("3=off"));
        Assert.Contains(gm.Sent, s => s.Contains("house (48): 0 ambush, 0 phantom, 0 stalker"));
        Assert.Contains(gm.Sent, s => s.Contains("basement (39):"));
        Assert.Contains(gm.Sent, s => s.Contains("grove (38):"));
        Assert.Contains(gm.Sent, s => s.Contains("Next chainsaw: not scheduled"));

        Assert.True(kit.Fixture.RunCommand(gm, "/halloween bogus"));
        Assert.Contains(gm.Sent, s => s.StartsWith("$7Usage: /halloween "));

        Assert.True(kit.Fixture.RunCommand(gm, "/halloween off"));
        Assert.Equal(0, kit.World.CommunityGoals.GetFlag("halloween.active"));
    }

    [Fact]
    public void Tick_calls_current_script_object_after_swap()
    {
        using var kit = new HalloweenTestKit();
        var script = kit.Compile<IGlobalScript>("Global", "Halloween.csx");
        var first = script.Object;
        first.OnLoaded(kit.World);

        script.CompileForSwap()();
        var second = script.Object;
        Assert.NotSame(first, second);

        FirePendingScriptTimers(kit.World);

        Assert.Equal(0L, (long)HalloweenTestKit.Call(first, "LastTickAt")!);
        Assert.NotEqual(0L, (long)HalloweenTestKit.Call(second, "LastTickAt")!);
    }

    private static CommunityRewardContext RewardContext(HalloweenTestKit kit) => new()
    {
        Goal = new CommunityGoal { Id = 1, Name = "Goal", NpcTemplateId = 0 },
        Run = new CommunityGoalRun { GoalId = 1, Run = 1, StartedAt = DateTime.UtcNow },
        World = kit.World,
    };

    private static void FirePendingScriptTimers(GameWorld world)
    {
        var queue = (PriorityQueue<Event, long>)typeof(Goose.EventHandler)
            .GetField("events", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(world.EventHandler)!;
        var pending = new List<Event>();
        while (queue.TryDequeue(out Event? ev, out _)) pending.Add(ev!);
        foreach (var ev in pending.OfType<ScriptTimerEvent>())
            ev.Ready(world);
    }
}
