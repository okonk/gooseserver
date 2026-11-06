using System.Globalization;
using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;

namespace Goose.Tests.Arena;

public sealed class ArenaControllerTests : IDisposable
{
    private readonly ArenaTestKit kit = new();

    public void Dispose() => kit.Dispose();

    private static object? Call(object obj, string method, params object?[] args) => ArenaTestKit.Call(obj, method, args);
    private static object? Run(ArenaTestKit kit) => Call(kit.Host, "Run", kit.World);
    private static object? LastRun(ArenaTestKit kit) => Call(kit.Host, "LastRun", kit.World);
    private static object? Prop(object? obj, string name) => ArenaTestKit.GetProp(obj, name);
    private static System.Collections.IDictionary Participants(object run) => (System.Collections.IDictionary)Prop(run, "Participants")!;

    [Fact]
    public void Announces_15_10_5_once_each()
    {
        var controller = kit.RegisterController();
        controller.Object.OnLoaded(kit.World);
        var gm = kit.LobbyPlayer(70001, "GM", gm: true);

        var t0 = DateTime.UtcNow.Date.AddHours(2).AddMinutes(44);
        for (var t = t0; t <= t0.AddMinutes(16); t = t.AddSeconds(30))
            Call(controller.Object, "Tick", kit.World, t);

        Assert.Equal(1, gm.Sent.Count(s => s.Contains("opens in 15 minutes")));
        Assert.Equal(1, gm.Sent.Count(s => s.Contains("opens in 10 minutes")));
        Assert.Equal(1, gm.Sent.Count(s => s.Contains("opens in 5 minutes")));
        Assert.Equal(3, gm.Sent.Count(s => s.Contains("opens in")));
    }

    [Fact]
    public void Starts_on_anchor_and_skips_when_run_active()
    {
        var controller = kit.RegisterController();
        var p1 = kit.LobbyPlayer(70001, "P1");
        p1.ExperienceSold = 3_000_000_000;
        var world = kit.World;

        var t0 = DateTime.UtcNow.Date.AddHours(2).AddMinutes(59).AddSeconds(30);
        Call(controller.Object, "Tick", world, t0);
        Assert.Null(Run(kit));

        Call(controller.Object, "Tick", world, t0.AddSeconds(60));
        var run = Run(kit)!;
        Assert.NotNull(run);
        Assert.Equal(0, (int)Prop(run, "Phase")!);
        Assert.NotNull(Participants(run)[70001]);

        Call(kit.Host, "EndRun", world, "test");
        Call(kit.Host, "StartRun", world, t0.AddHours(2));
        var runId = (string)Prop(Run(kit)!, "RunId")!;

        Call(controller.Object, "Tick", world, t0.AddHours(2).AddSeconds(30));
        Call(controller.Object, "Tick", world, t0.AddHours(3).AddSeconds(30));

        Assert.Equal(runId, (string)Prop(Run(kit)!, "RunId")!);

        var lastTick = world.WorldState.Get<string>("arena.lastTickUtc")!;
        Assert.True(DateTime.TryParse(lastTick, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var t));
        Assert.Equal(t0.AddHours(3).AddSeconds(30), t);
    }

    [Fact]
    public void Restart_recovery_ends_stale_run_and_pays_bonuses()
    {
        var controller = kit.RegisterController();
        var p1 = kit.LobbyPlayer(70001, "P1");
        p1.ExperienceSold = 3_000_000_000;
        var world = kit.World;

        var now = DateTime.UtcNow;
        Call(kit.Host, "StartRun", world, now);
        Call(kit.Host, "Tick", world, now.AddSeconds(11));
        Assert.Equal(6, kit.ArenaMap.NPCs.Count);
        kit.Enter(kit.ArenaMap, p1);

        Call(kit.Host, "Pay", world, 70001, 6L, "test");
        var run = Run(kit)!;
        ArenaTestKit.SetProp(Participants(run)[70001]!, "RoundsCleared", 5);
        Call(kit.Host, "SaveRun", world, run);

        var template = world.NPCHandler.GetNPCTemplate(ArenaTestKit.BandNormalIds[0])!;
        world.NPCHandler.SpawnNPC(world, ArenaTestKit.ArenaMapId, 20, 20, template, false,
            new PropertiesDictionary { ["arena.run"] = "stale-run-id" });

        Call(controller.Object, "Recover", world);

        Assert.Null(Run(kit));
        var last = LastRun(kit)!;
        var results = (System.Collections.IList)Prop(last, "Results")!;
        Assert.Single(results);
        var result = results[0]!;
        Assert.Equal("P1", (string)Prop(result, "Name")!);
        Assert.Equal(5, (int)Prop(result, "RoundsCleared")!);
        Assert.Equal(26, (long)Prop(result, "Coins")!);
        Assert.Equal(26, kit.Wallet(p1));
        Assert.Empty(kit.ArenaMap.NPCs);
        Assert.Equal(ArenaTestKit.LobbyMapId, p1.MapID);
    }

    [Fact]
    public void Reload_does_not_trigger_recovery()
    {
        var controller = kit.RegisterController();
        controller.Object.OnLoaded(kit.World);
        var p1 = kit.LobbyPlayer(70001, "P1");
        p1.ExperienceSold = 3_000_000_000;
        var world = kit.World;

        var now = DateTime.UtcNow;
        Call(kit.Host, "StartRun", world, now);
        var runId = (string)Prop(Run(kit)!, "RunId")!;
        var old = controller.Object;

        controller.LoadScript();

        Assert.NotSame(old, controller.Object);
        Call(controller.Object, "Tick", world, now.AddSeconds(1));
        var run = Run(kit)!;
        Assert.NotNull(run);
        Assert.Equal(runId, (string)Prop(run, "RunId")!);
        Assert.Equal(0, (int)Prop(run, "Phase")!);
    }

    [Fact]
    public void Preview_prints_values_matching_math()
    {
        var controller = kit.RegisterController();
        controller.Object.OnLoaded(kit.World);
        var gm = kit.LobbyPlayer(70001, "GM", gm: true);
        gm.ExperienceSold = 1_500_000_000;
        gm.MaxStats.AC = 100;

        Assert.True(kit.Fixture.RunCommand(gm, "/arena preview 5 2500000"));

        var cfg = Call(kit.Host, "Config", kit.World)!;
        var power = (double)Call(kit.Host, "PowerFraction", 1_500_000_000L, cfg)!;
        var hpN = (long)Call(kit.Host, "MobHp", 5, 0, 1, cfg)!;
        var hpE = (long)Call(kit.Host, "MobHp", 5, 1, 1, cfg)!;
        var hpB = (long)Call(kit.Host, "MobHp", 5, 2, 1, cfg)!;
        var eff = (long)Call(kit.Host, "EffectiveDamage", 2_500_000L, power, cfg)!;

        var template = kit.World.NPCHandler.GetNPCTemplate(ArenaTestKit.BandNormalIds[0])!;
        var npc = new NPC();
        npc.NPCTemplate = template;
        npc.Level = template.Level;
        npc.MaxStats = new AttributeSet();

        var inv = CultureInfo.InvariantCulture;
        Assert.Contains(gm.Sent, m => m.Contains("[Arena] Preview round 5 for GM:"));
        Assert.Contains(gm.Sent, m => m.Contains("Power s = " + power.ToString("0.####", inv)));
        Assert.Contains(gm.Sent, m => m.Contains("normal " + hpN + ", elite " + hpE + ", boss " + hpB));
        Assert.Contains(gm.Sent, m => m.Contains("Your effective hit (raw 2500000) = " + eff));
        var names = new[] { "Normal", "Elite", "Boss" };
        for (var tier = 0; tier < 3; tier++)
        {
            var before = (double)Call(kit.Host, "MobWeaponDamage", 5, tier, power, cfg)!;
            var after = npc.MitigateMelee(gm, before, kit.World);
            Assert.Contains(gm.Sent, m => m.Contains(names[tier] + " weapon: " +
                before.ToString("0.##", inv) + " -> " + after.ToString("0.##", inv)));
            if (tier == 0)
                Assert.True(after < before);
        }
    }

    [Fact]
    public void Commands_require_gm()
    {
        kit.SetConfig(j => j["intervalHours"] = 100000);
        var controller = kit.RegisterController();
        controller.Object.OnLoaded(kit.World);
        var gm = kit.LobbyPlayer(70001, "GM", gm: true);
        var p2 = kit.LobbyPlayer(70002, "P2");
        p2.ExperienceSold = 3_000_000_000;

        kit.Fixture.RunCommand(p2, "/arena start");
        Assert.Null(Run(kit));
        Assert.DoesNotContain(p2.Sent, s => s.Contains("Arena run started"));

        Assert.True(kit.Fixture.RunCommand(gm, "/arena start"));
        var run = Run(kit)!;
        Assert.NotNull(run);
        Assert.NotNull(Participants(run)[70002]);
        Assert.Contains(gm.Sent, s => s.Contains("Arena run started."));
        var runId = (string)Prop(run, "RunId")!;

        kit.Fixture.RunCommand(p2, "/arena stop");
        Assert.Equal(runId, (string)Prop(Run(kit)!, "RunId")!);

        kit.Fixture.RunCommand(gm, "/arena stop");
        Assert.Null(Run(kit));
        Assert.NotNull(LastRun(kit));
        Assert.Contains(gm.Sent, s => s.Contains("Arena run stopped."));
    }

    [Fact]
    public void Status_with_broken_config_does_not_hang()
    {
        kit.SetConfig(j => j["a"] = 3.5);
        var controller = kit.RegisterController();
        controller.Object.OnLoaded(kit.World);
        var gm = kit.LobbyPlayer(70001, "GM", gm: true);

        Assert.True(kit.Fixture.RunCommand(gm, "/arena status"));
        Assert.Contains(gm.Sent, s => s.Contains("The arena is disabled."));
    }

    [Fact]
    public void Empty_bands_config_is_disabled()
    {
        kit.SetConfig(j => j["bands"] = new JsonArray());
        var controller = kit.RegisterController();
        controller.Object.OnLoaded(kit.World);
        var gm = kit.LobbyPlayer(70001, "GM", gm: true);

        Assert.False((bool)Call(kit.Host, "Enabled", kit.World)!);
        Assert.True(kit.Fixture.RunCommand(gm, "/arena status"));
        Assert.Contains(gm.Sent, s => s.Contains("The arena is disabled."));
    }
}
