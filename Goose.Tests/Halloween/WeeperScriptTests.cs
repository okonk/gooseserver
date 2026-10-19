using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class WeeperScriptTests : IDisposable
{
    private static readonly long Now = 100_000;

    private readonly HalloweenTestKit kit;
    private readonly Map map;
    private readonly NPCTemplate template;
    private readonly object weeper;

    public WeeperScriptTests()
    {
        kit = new HalloweenTestKit();
        var script = kit.Compile<INPCScript>("NPC", "Weeper.csx");
        weeper = script.Object;
        template = kit.Fixture.AddNpcTemplate(92001, "The Weeper", t =>
        {
            t.Script = script;
            t.AttackSpeed = 2;
            t.AttackRange = 1;
            t.AggroRange = 6;
            t.MoveSpeed = 1.2;
            t.BaseStats.HP = 12_000_000;
        });
        map = kit.Fixture.AddBaseMap(48, "House", 60, 60);
    }

    public void Dispose() => kit.Dispose();

    [Fact]
    public void Spawn_adds_cap_buff_and_announces()
    {
        kit.Fixture.AddBaseSpellEffect(777, "Weeper Cap");
        kit.SetConfig(cfg => cfg["effects"]!["weeperCap"] = 777);
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([0.5]));

        var player = PlayerOn(40, 36, "A");
        var npc = Spawn(38, 36);

        Assert.Equal(1, npc.Buffs.Count);
        Assert.Equal(777, npc.Buffs[0].SpellEffect.ID);
        Assert.Contains(player.Sent, s => s.Contains("A low weeping echoes through the house…"));
        Assert.True(npc.Properties.TryGetProperty<string>("weeperLoop", out _));
        Assert.Equal(0, LoopIndex(npc));
    }

    [Fact]
    public void Patrol_reaches_waypoints_around_walls()
    {
        int[][] loop = [[10, 5], [10, 15], [20, 15], [20, 5], [10, 5]];
        Loops(("a", loop));
        for (int x = 1; x <= map.Width; x++)
            if (x != 5) map.SetTile(x, 10, new BlockedTile());

        var npc = Spawn(10, 5);
        var trail = new List<(int X, int Y)>();
        for (int i = 0; i < 60 && !(npc.MapX == 10 && npc.MapY == 15 && LoopIndex(npc) == 2); i++)
        {
            Call("MoveStep", npc, Now, kit.World);
            trail.Add((npc.MapX, npc.MapY));
        }

        Assert.Equal(10, npc.MapX);
        Assert.Equal(15, npc.MapY);
        Assert.Equal(2, LoopIndex(npc));
        Assert.Contains((5, 10), trail);
    }

    [Fact]
    public void Next_loop_differs_from_previous()
    {
        int[][] square = [[10, 10], [14, 10], [14, 14], [10, 14], [10, 10]];
        Loops(("a", square), ("b", square), ("c", square));
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([0.0, 0.0]));

        var npc = Spawn(10, 10);
        Assert.Equal("a", LoopName(npc));

        npc.Properties["weeperLastLoop"] = "a";
        Call("OnSpawnEvent", npc, kit.World);
        Assert.Equal("b", LoopName(npc));
        Assert.Equal(0, LoopIndex(npc));

        for (int i = 0; i < 60 && LoopName(npc) == "b"; i++)
            Call("MoveStep", npc, Now, kit.World);

        Assert.Equal("a", LoopName(npc));
        Assert.Equal("b", (string)npc.Properties["weeperLastLoop"]);
        Assert.Equal(0, LoopIndex(npc));
    }

    [Fact]
    public void Swing_takes_8_percent_and_16_for_stragglers()
    {
        kit.World.Settings.MaxDodgeChance = 0;
        var npc = Spawn(10, 10);
        var target = PlayerOn(11, 10, "T");
        npc.AddAggro(target, 100, kit.World);
        SetState(npc, "NextLungeAt", Now + kit.World.TimerFrequency * 15);

        Call("AttackStep", npc, Now, kit.World);
        Assert.Equal(8_400, target.CurrentHP);

        PlayerOn(12, 11, "Ally");
        target.CurrentHP = 10_000;
        Call("AttackStep", npc, Now, kit.World);
        Assert.Equal(9_200, target.CurrentHP);
    }

    [Fact]
    public void Swing_adds_bleed_stack()
    {
        kit.World.Settings.MaxDodgeChance = 0;
        var npc = Spawn(10, 10);
        var target = PlayerOn(11, 10, "T");
        npc.AddAggro(target, 100, kit.World);
        SetState(npc, "NextLungeAt", Now + kit.World.TimerFrequency * 15);

        Call("AttackStep", npc, Now, kit.World);
        Assert.Equal(1, BleedStacks(npc, target));

        Call("AttackStep", npc, Now + kit.World.TimerFrequency, kit.World);
        Assert.Equal(2, BleedStacks(npc, target));
    }

    [Fact]
    public void Lunge_moves_next_to_another_player_and_switches_target_without_changing_damage_credit()
    {
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([0.0]));
        var npc = Spawn(10, 10);
        var a = PlayerOn(11, 10, "A");
        var b = PlayerOn(15, 10, "B");
        npc.AddAggro(a, 100, kit.World);

        Call("AttackStep", npc, Now, kit.World);

        Assert.Equal(b, npc.AggroTarget);
        Assert.True(Math.Abs(npc.MapX - b.MapX) <= 1 && Math.Abs(npc.MapY - b.MapY) <= 1);
        Assert.Equal(100, npc.AggroTargetToValue[a].Damage);
        Assert.Equal(0, npc.AggroTargetToValue[b].Damage);
        Assert.Equal(101, npc.AggroTargetToValue[b].Taunt);
        Assert.Contains(b.Sent, s => s.Contains("The Weeper lunges at B!"));
        Assert.Equal(Now + kit.World.TimerFrequency * 15, StateLong(npc, "NextLungeAt"));
    }

    [Theory]
    [InlineData(Direction.Up, 15, 11)]
    [InlineData(Direction.Down, 15, 9)]
    [InlineData(Direction.Left, 16, 10)]
    [InlineData(Direction.Right, 14, 10)]
    public void Lunge_lands_directly_behind_the_player(Direction facing, int x, int y)
    {
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([0.0]));
        var npc = Spawn(10, 10);
        var a = PlayerOn(11, 10, "A");
        var b = PlayerOn(15, 10, "B");
        b.Facing = facing;
        npc.AddAggro(a, 100, kit.World);

        Call("AttackStep", npc, Now, kit.World);

        Assert.Equal((x, y), (npc.MapX, npc.MapY));
    }

    [Fact]
    public void Lunge_falls_back_to_the_side_when_behind_is_blocked()
    {
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([0.0]));
        map.SetTile(15, 11, new BlockedTile());
        var npc = Spawn(10, 10);
        var a = PlayerOn(11, 10, "A");
        var b = PlayerOn(15, 10, "B");
        b.Facing = Direction.Up;
        npc.AddAggro(a, 100, kit.World);

        Call("AttackStep", npc, Now, kit.World);

        Assert.Equal(1, Math.Abs(npc.MapX - b.MapX));
        Assert.Equal(b.MapY, npc.MapY);
    }

    [Fact]
    public void Patrol_counts_a_taken_waypoint_as_reached_once_beside_it()
    {
        int[][] loop = [[10, 5], [10, 8], [14, 8]];
        Loops(("a", loop));
        var npc = Spawn(10, 5);
        Spawn(10, 8);

        Call("MoveStep", npc, Now, kit.World);
        Call("MoveStep", npc, Now, kit.World);
        Call("MoveStep", npc, Now, kit.World);
        Assert.Equal((10, 7), (npc.MapX, npc.MapY));
        Assert.Equal(1, LoopIndex(npc));

        Call("MoveStep", npc, Now, kit.World);
        Assert.Equal(2, LoopIndex(npc));
        Assert.Equal((10, 7), (npc.MapX, npc.MapY));
    }

    [Fact]
    public void Patrol_skips_a_waypoint_it_cannot_get_near()
    {
        int[][] loop = [[10, 5], [10, 15], [14, 5]];
        Loops(("a", loop));
        for (int x = 1; x <= map.Width; x++)
            if (x != 5) map.SetTile(x, 10, new BlockedTile());
        var npc = Spawn(10, 5);
        Spawn(5, 10);

        for (int i = 0; i < 20 && LoopIndex(npc) < 2; i++)
            Call("MoveStep", npc, Now, kit.World);

        Assert.Equal(2, LoopIndex(npc));
        Assert.True(npc.MapY < 10);
    }

    [Fact]
    public void Leash_drops_aggro_after_10s_with_nobody_near()
    {
        var npc = Spawn(10, 10);
        var target = PlayerOn(40, 10, "T");
        npc.AddAggro(target, 100, kit.World);

        Call("MoveStep", npc, Now, kit.World);
        Assert.Equal(target, npc.AggroTarget);

        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 9, kit.World);
        Assert.Equal(target, npc.AggroTarget);

        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 10, kit.World);
        Assert.Null(npc.AggroTarget);
        Assert.Empty(npc.AggroTargetToValue);
        Assert.Equal(Now + kit.World.TimerFrequency * 10, StateLong(npc, "LastCombatAt"));
    }

    [Fact]
    public void Leash_drops_aggro_past_40_tiles_off_route()
    {
        var npc = Spawn(10, 10);
        var target = PlayerOn(17, 10, "T");
        npc.AddAggro(target, 100, kit.World);

        Call("MoveStep", npc, Now, kit.World);
        npc.MoveTo(kit.World, 51, 10);
        target.MapX = 59;

        Call("MoveStep", npc, Now + kit.World.TimerFrequency, kit.World);

        Assert.Null(npc.AggroTarget);
        Assert.Empty(npc.AggroTargetToValue);
    }

    [Fact]
    public void Heals_to_full_after_30s_out_of_combat_not_before()
    {
        int[][] small = [[10, 10], [12, 10], [12, 12], [10, 12], [10, 10]];
        Loops(("a", small));
        var npc = Spawn(10, 10);
        var watcher = PlayerOn(11, 10, "Watcher");
        npc.CurrentHP = 5_000;
        SetState(npc, "LastCombatAt", Now);

        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 29, kit.World);
        Assert.Equal(5_000, npc.CurrentHP);

        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 30, kit.World);
        Assert.Equal(npc.MaxHP, npc.CurrentHP);
        Assert.Contains(watcher.Sent, s => s.StartsWith("VPU" + npc.LoginID + ",100,"));
    }

    [Fact]
    public void Returns_to_nearest_waypoint_after_leash()
    {
        int[][] big = [[10, 10], [30, 10], [30, 30], [10, 30], [10, 10]];
        Loops(("a", big));
        var npc = Spawn(10, 10);
        Call("MoveStep", npc, Now, kit.World);
        Assert.Equal(1, LoopIndex(npc));

        var target = PlayerOn(50, 10, "T");
        npc.AddAggro(target, 100, kit.World);

        Call("MoveStep", npc, Now, kit.World);
        npc.MoveTo(kit.World, 15, 28);
        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 10, kit.World);

        Assert.Null(npc.AggroTarget);
        Assert.Equal(3, LoopIndex(npc));
        Assert.Equal(6, Math.Abs(npc.MapX - 10) + Math.Abs(npc.MapY - 30));
    }

    [Fact]
    public void Footsteps_reach_nearby_idle_players_with_direction_not_fighters()
    {
        var npc = Spawn(10, 10);
        var idle = PlayerOn(10, 5, "Idle");
        var fighter = PlayerOn(11, 10, "F");
        var far = PlayerOn(30, 10, "Far");
        npc.AddAggro(fighter, 100, kit.World);
        SetState(npc, "NextFootstepsAt", Now);

        Call("FootstepsCheck", npc, Now, kit.World);

        Assert.Contains(idle.Sent, s => s.Contains("Something is dragging its feet… to the south."));
        Assert.DoesNotContain(fighter.Sent, s => s.Contains("dragging its feet"));
        Assert.DoesNotContain(far.Sent, s => s.Contains("dragging its feet"));
        Assert.Equal(Now + kit.World.TimerFrequency * 8, StateLong(npc, "NextFootstepsAt"));

        Call("FootstepsCheck", npc, Now + kit.World.TimerFrequency * 7, kit.World);
        Assert.Single(idle.Sent, s => s.Contains("dragging its feet"));
    }

    [Fact]
    public void Route_exit_point_is_re_recorded_when_combat_ends_without_leash()
    {
        var npc = Spawn(10, 10);
        var a = PlayerOn(11, 10, "A");
        npc.AddAggro(a, 100, kit.World);

        Call("MoveStep", npc, Now, kit.World);
        Assert.Equal(10, StateInt(npc, "LeftRouteAtX"));

        npc.RemoveAggro(a);
        map.RemovePlayer(a, kit.World);
        Call("MoveStep", npc, Now + kit.World.TimerFrequency, kit.World);
        npc.MoveTo(kit.World, 51, 10);

        var b = PlayerOn(52, 10, "B");
        npc.AddAggro(b, 100, kit.World);

        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 2, kit.World);

        Assert.Equal(b, npc.AggroTarget);
        Assert.Equal(51, StateInt(npc, "LeftRouteAtX"));
        Assert.Equal(10, StateInt(npc, "LeftRouteAtY"));
    }

    [Fact]
    public void Leash_window_restarts_when_aggro_is_cleared_without_leash()
    {
        var npc = Spawn(10, 10);
        var a = PlayerOn(40, 10, "A");
        npc.AddAggro(a, 100, kit.World);

        Call("MoveStep", npc, Now, kit.World);
        Assert.Equal(Now, StateLong(npc, "LeashStartedAt"));

        npc.RemoveAggro(a);
        map.RemovePlayer(a, kit.World);
        Call("MoveStep", npc, Now + kit.World.TimerFrequency, kit.World);
        Assert.Equal(0, StateLong(npc, "LeashStartedAt"));

        var b = PlayerOn(40, 12, "B");
        npc.AddAggro(b, 100, kit.World);
        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 5, kit.World);
        Assert.Equal(Now + kit.World.TimerFrequency * 5, StateLong(npc, "LeashStartedAt"));

        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 10, kit.World);
        Assert.Equal(b, npc.AggroTarget);

        Call("MoveStep", npc, Now + kit.World.TimerFrequency * 15, kit.World);
        Assert.Null(npc.AggroTarget);
    }

    private NPC Spawn(int x, int y) => kit.Fixture.SpawnNpc(map, x, y, template);

    private TestWorldFixture.CapturingPlayer PlayerOn(int x, int y, string name)
    {
        var player = kit.Fixture.CommandPlayerOn(map, x, y, name);
        player.PlayerID = x * 100 + y;
        player.MaxStats.HP = 10_000;
        player.CurrentHP = 10_000;
        map.AddPlayer(player, kit.World);
        return player;
    }

    private void Loops(params (string Name, int[][] Points)[] loops)
    {
        kit.SetConfig(cfg =>
        {
            var json = new JsonObject();
            foreach (var (name, points) in loops)
            {
                var array = new JsonArray();
                foreach (var point in points)
                    array.Add(new JsonArray(point[0], point[1]));
                json[name] = array;
            }
            cfg["weeper"]!["loops"] = json;
        });
    }

    private object? Call(string method, params object?[] args)
        => weeper.GetType().GetMethod(method)!.Invoke(weeper, args);

    private string LoopName(NPC npc) => (string)npc.Properties["weeperLoop"];

    private int LoopIndex(NPC npc) => (int)npc.Properties["weeperWaypoint"];

    private long StateLong(NPC npc, string field)
        => (long)npc.ScriptStore!.GetType().GetProperty(field)!.GetValue(npc.ScriptStore)!;

    private int StateInt(NPC npc, string field)
        => (int)npc.ScriptStore!.GetType().GetProperty(field)!.GetValue(npc.ScriptStore)!;

    private void SetState(NPC npc, string field, long value)
        => npc.ScriptStore!.GetType().GetProperty(field)!.SetValue(npc.ScriptStore, value);

    private int BleedStacks(NPC npc, Player target)
    {
        var bleeds = (IDictionary)npc.ScriptStore!.GetType().GetProperty("Bleeds")!.GetValue(npc.ScriptStore)!;
        var entry = bleeds[target.PlayerID]!;
        return (int)entry.GetType().GetProperty("Stacks")!.GetValue(entry)!;
    }

    private static void SetWorldRandom(GameWorld world, Random rng)
        => typeof(GameWorld).GetField("rng", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(world, rng);
}
