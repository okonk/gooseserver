using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenBossHelperTests
{
    private static readonly long Now = 1_000_000;

    private sealed class Ctx
    {
        public required HalloweenTestKit Kit { get; init; }
        public required Type Boss { get; init; }
        public required NPC Npc { get; init; }
        public TestWorldFixture.CapturingPlayer Target { get; set; } = null!;

        public GameWorld World => Kit.World;

        public object? Call(string method, params object?[] args)
            => Boss.GetMethod(method)!.Invoke(null, args);
    }

    [Fact]
    public void PercentHit_ignores_armour()
    {
        var ctx = Setup();
        var observer = KitPlayer(ctx, ctx.Target.Map, 14, 14, "Observer");
        ctx.Target.MaxStats.HP = 10_000;
        ctx.Target.CurrentHP = 10_000;
        ctx.Target.MaxStats.AC = 100_000;
        ctx.World.Settings.MaxDodgeChance = 0;

        ctx.Call("PercentHit", ctx.Npc, ctx.Target, 0.08, ctx.World);

        Assert.Equal(9_200, ctx.Target.CurrentHP);
        Assert.Contains(ctx.Target.Sent, s => s.Contains("ATT" + ctx.Npc.LoginID));
        Assert.Contains(observer.Sent, s => s.Contains("ATT" + ctx.Npc.LoginID));
    }

    [Fact]
    public void IsStraggler_true_alone_false_with_ally_within_5_ignores_gms()
    {
        var ctx = Setup();

        Assert.True((bool)ctx.Call("IsStraggler", ctx.Target, 5.0)!);

        var gm = KitPlayer(ctx, ctx.Target.Map, 13, 13, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        Assert.True((bool)ctx.Call("IsStraggler", ctx.Target, 5.0)!);

        KitPlayer(ctx, ctx.Target.Map, 15, 14, "Ally");
        Assert.False((bool)ctx.Call("IsStraggler", ctx.Target, 5.0)!);
    }

    [Fact]
    public void Bleed_stacks_to_five_and_refreshes_duration()
    {
        var ctx = Setup();
        ctx.Target.MaxStats.HP = 10_000;
        ctx.Target.CurrentHP = 10_000;

        for (int i = 0; i < 6; i++)
            ctx.Call("AddBleed", ctx.Npc, ctx.Target, Now + i, ctx.World);

        var entry = BleedEntry(ctx, ctx.Target.PlayerID);
        Assert.Equal(5, (int)entry.GetType().GetProperty("Stacks")!.GetValue(entry)!);
        Assert.Equal(Now + 5 + ctx.World.TimerFrequency * 10,
            (long)entry.GetType().GetProperty("ExpiresAt")!.GetValue(entry)!);

        ctx.Call("TickBleeds", ctx.Npc, Now + 100, ctx.World);
        Assert.Equal(9_500, ctx.Target.CurrentHP);
    }

    [Fact]
    public void Bleed_expires_after_10s_without_hits()
    {
        var ctx = Setup();
        ctx.Target.MaxStats.HP = 10_000;
        ctx.Target.CurrentHP = 10_000;

        ctx.Call("AddBleed", ctx.Npc, ctx.Target, Now, ctx.World);
        ctx.Call("TickBleeds", ctx.Npc, Now + ctx.World.TimerFrequency * 11, ctx.World);

        Assert.Equal(10_000, ctx.Target.CurrentHP);
        Assert.Empty(BleedEntries(ctx));
    }

    [Fact]
    public void Bleed_stops_when_target_leaves_map()
    {
        var ctx = Setup();
        ctx.Target.MaxStats.HP = 10_000;
        ctx.Target.CurrentHP = 10_000;
        var other = ctx.Kit.Fixture.AddBaseMap(99, "Elsewhere", 20, 20);

        ctx.Call("AddBleed", ctx.Npc, ctx.Target, Now, ctx.World);
        CompleteWarp(ctx, other, 5, 5);
        ctx.Call("TickBleeds", ctx.Npc, Now + 100, ctx.World);

        Assert.Equal(10_000, ctx.Target.CurrentHP);
        Assert.Empty(BleedEntries(ctx));
    }

    [Fact]
    public void Compass_names_eight_directions()
    {
        var ctx = Setup();

        Assert.Equal("south", (string)ctx.Call("Compass", 10, 10, 10, 12)!);
        Assert.Equal("east", (string)ctx.Call("Compass", 10, 10, 12, 10)!);
        Assert.Equal("north", (string)ctx.Call("Compass", 10, 10, 10, 8)!);
        Assert.Equal("west", (string)ctx.Call("Compass", 10, 10, 8, 10)!);
        Assert.Equal("south-east", (string)ctx.Call("Compass", 10, 10, 13, 11)!);
        Assert.Equal("south-west", (string)ctx.Call("Compass", 10, 10, 7, 12)!);
        Assert.Equal("north-east", (string)ctx.Call("Compass", 10, 10, 12, 9)!);
        Assert.Equal("north-west", (string)ctx.Call("Compass", 10, 10, 9, 7)!);
        Assert.Equal("north", (string)ctx.Call("Compass", 10, 10, 10, 10)!);
    }

    private static Ctx Setup()
    {
        var kit = new HalloweenTestKit();
        var asm = kit.Compile<IGlobalScript>("Global", "Halloween.csx").Object.GetType().Assembly;
        var type = asm.GetTypes().First(t => t.Name == "HalloweenBoss");
        var map = kit.Fixture.AddBaseMap(48, "House", 48, 48);
        var npc = kit.Fixture.SpawnNpc(map, 5, 5, kit.World.NPCHandler.GetNPCTemplate(kit.WailingGhostId)!);
        var ctx = new Ctx { Kit = kit, Boss = type, Npc = npc };
        ctx.Target = KitPlayer(ctx, map, 10, 10, "Target");
        return ctx;
    }

    private static TestWorldFixture.CapturingPlayer KitPlayer(Ctx ctx, Map map, int x, int y, string name)
    {
        var player = ctx.Kit.Fixture.CommandPlayerOn(map, x, y, name);
        player.PlayerID = x * 100 + y;
        map.AddPlayer(player, ctx.World);
        return player;
    }

    private static List<object> BleedEntries(Ctx ctx)
    {
        var bleeds = (System.Collections.IDictionary)ctx.Npc.ScriptStore!.GetType()
            .GetProperty("Bleeds")!.GetValue(ctx.Npc.ScriptStore)!;
        return bleeds.Keys.Cast<object>().Select(k => bleeds[k]!).ToList();
    }

    private static object BleedEntry(Ctx ctx, int playerId)
        => ((System.Collections.IDictionary)ctx.Npc.ScriptStore!.GetType()
            .GetProperty("Bleeds")!.GetValue(ctx.Npc.ScriptStore)!)[playerId]!;

    private static void CompleteWarp(Ctx ctx, Map map, int x, int y)
    {
        ctx.Target.WarpTo(ctx.World, map, x, y);
        var ev = new DoneLoadingMapEvent { Player = ctx.Target, Ticks = ctx.World.TimeNow };
        ctx.World.EventHandler.AddEvent(ev);
        ctx.World.EventHandler.Update(ctx.World);
        Assert.Equal(map, ctx.Target.Map);
    }
}
