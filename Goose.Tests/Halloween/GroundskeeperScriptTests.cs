using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class GroundskeeperScriptTests : IDisposable
{
    private static readonly long Now = 100_000;

    private readonly HalloweenTestKit kit;
    private readonly Map map;
    private readonly NPCTemplate template;
    private readonly object keeper;

    public GroundskeeperScriptTests()
    {
        kit = new HalloweenTestKit();
        var script = kit.Compile<INPCScript>("NPC", "Groundskeeper.csx");
        keeper = script.Object;
        template = kit.Fixture.AddNpcTemplate(92002, "The Groundskeeper", t =>
        {
            t.Script = script;
            t.AttackSpeed = 2.5;
            t.AttackRange = 1;
            t.AggroRange = 50;
            t.BaseStats.HP = 100_000;
        });
        map = kit.Fixture.AddBaseMap(39, "Basement", 60, 60);
    }

    public void Dispose() => kit.Dispose();

    [Fact]
    public void Spawn_scales_hp_by_players_present()
    {
        kit.Fixture.AddBaseSpellEffect(777, "Groundskeeper Curve");
        kit.SetConfig(cfg => cfg["effects"]!["groundskeeperCurve"] = 777);
        var player = PlayerOn(20, 20, "A");
        kit.Fixture.AddOnlinePlayer(player);
        PlayerOn(21, 20, "B");

        var npc = Spawn(30, 30);

        Assert.Equal(16_000_000, npc.MaxHP);
        Assert.Equal(npc.MaxHP, npc.CurrentHP);
        Assert.Single(npc.Buffs, b => b.SpellEffect.ID == 777);
        Assert.Contains(player.Sent, s => s.Contains("[Event] The Groundskeeper has risen in the basement!"));
    }

    [Fact]
    public void Crossing_75_runs_phase_once_with_rescale_and_adds()
    {
        Adds(("placeSettings", kit.WailingGhostId, 3));
        var npc = Spawn(30, 30);
        Assert.Equal(8_000_000, npc.MaxHP);
        npc.CurrentHP = (long)(0.74 * npc.MaxHP);

        var player = PlayerOn(31, 30, "A");
        PlayerOn(32, 30, "B");
        npc.AddAggro(player, 100, kit.World);

        Call("AttackStep", npc, Now, kit.World);

        Assert.Equal(1, Phase(npc));
        Assert.Equal(16_000_000, npc.MaxHP);
        Assert.Equal((long)(0.74 * 16_000_000), npc.CurrentHP);
        Assert.Equal(3, AddsOn(map, "placeSettings").Count);

        Call("AttackStep", npc, Now + kit.World.TimerFrequency, kit.World);

        Assert.Equal(1, Phase(npc));
        Assert.Equal(3, AddsOn(map, "placeSettings").Count);
    }

    [Fact]
    public void Burst_past_two_thresholds_runs_both_phases_in_order()
    {
        Adds(("placeSettings", kit.WailingGhostId, 2), ("ghosts", kit.FlickeringGhostId, 3));
        var npc = Spawn(30, 30);
        npc.CurrentHP = (long)(0.40 * npc.MaxHP);

        Call("AttackStep", npc, Now, kit.World);

        Assert.Equal(2, Phase(npc));
        Assert.Equal(2, AddsOn(map, "placeSettings").Count);
        Assert.Equal(3, AddsOn(map, "ghosts").Count);
        Assert.Empty(AddsOn(map, "bloody"));
        Assert.Equal((long)(0.40 * npc.MaxHP), npc.CurrentHP);
    }

    [Fact]
    public void One_kind_can_mix_add_templates()
    {
        kit.SetConfig(cfg => cfg["groundskeeper"]!["adds"] = new JsonObject
        {
            ["ghosts"] = new JsonArray(new JsonArray(kit.WailingGhostId, 2), new JsonArray(kit.FlickeringGhostId, 1)),
        });
        var npc = Spawn(30, 30);
        npc.CurrentHP = (long)(0.40 * npc.MaxHP);

        Call("AttackStep", npc, Now, kit.World);

        var ghosts = AddsOn(map, "ghosts");
        Assert.Equal(2, ghosts.Count(g => g.NPCTemplateID == kit.WailingGhostId));
        Assert.Equal(1, ghosts.Count(g => g.NPCTemplateID == kit.FlickeringGhostId));
    }

    [Fact]
    public void Old_single_pair_adds_shape_is_rejected()
    {
        var npc = Spawn(30, 30);
        kit.SetConfig(cfg => cfg["groundskeeper"]!["adds"] = new JsonObject
        {
            ["ghosts"] = new JsonArray(kit.WailingGhostId, 2),
        });
        npc.CurrentHP = (long)(0.40 * npc.MaxHP);

        Call("AttackStep", npc, Now, kit.World);

        Assert.Empty(AddsOn(map, "ghosts"));
    }

    [Fact]
    public void Ghost_adds_get_snare_buff()
    {
        kit.Fixture.AddBaseSpellEffect(778, "Ghost Snare");
        kit.SetConfig(cfg =>
        {
            cfg["effects"]!["ghostSnareOnHit"] = 778;
            cfg["groundskeeper"]!["adds"] = new JsonObject { ["ghosts"] = new JsonArray(new JsonArray(kit.FlickeringGhostId, 2)) };
        });
        var npc = Spawn(30, 30);
        npc.CurrentHP = (long)(0.40 * npc.MaxHP);

        Call("AttackStep", npc, Now, kit.World);

        var ghosts = AddsOn(map, "ghosts");
        Assert.Equal(2, ghosts.Count);
        Assert.All(ghosts, g => Assert.Contains(g.Buffs, b => b.SpellEffect.ID == 778));
    }

    [Fact]
    public void Bloody_adds_tagged_and_reduce_damage_while_alive()
    {
        const int CurveEffect = 779;
        var curve = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCurve.csx");
        kit.SetConfig(cfg =>
        {
            cfg["effects"]!["groundskeeperCurve"] = CurveEffect;
            cfg["groundskeeper"]!["adds"] = new JsonObject { ["bloody"] = new JsonArray(new JsonArray(kit.BloodyGhostId, 1)) };
        });
        kit.Fixture.AddBaseSpellEffect(CurveEffect, "Groundskeeper Curve", e => e.Script = curve);
        var attacker = kit.Fixture.CommandPlayerOn(map, 1, 1);
        var npc = Spawn(30, 30);

        Assert.Equal(80_000, Hit(npc, attacker, 80_000));

        npc.CurrentHP = (long)(0.20 * npc.MaxHP);
        Call("AttackStep", npc, Now, kit.World);

        var bloody = Assert.Single(AddsOn(map, "bloody"));
        Assert.Equal(20_000, Hit(npc, attacker, 80_000));

        bloody.Despawn(kit.World);
        Assert.Equal(80_000, Hit(npc, attacker, 80_000));
    }

    [Fact]
    public void Lights_out_sends_dark_packet_then_restores()
    {
        var player = PlayerOn(30, 30, "A");
        var npc = Spawn(31, 30);
        npc.CurrentHP = (long)(0.70 * npc.MaxHP);

        Call("AttackStep", npc, Now, kit.World);

        Assert.Contains(player.Sent, s => s.StartsWith("AMB950,950,"));

        foreach (var timer in PendingScriptTimers(kit.World))
            timer.Ready(kit.World);

        Assert.Contains(player.Sent, s => s.StartsWith("AMB0,0,"));
    }

    [Fact]
    public void Big_hit_is_warned_then_lands_for_20_percent()
    {
        kit.World.Settings.MaxDodgeChance = 0;
        var npc = Spawn(30, 30);
        var target = PlayerOn(31, 30, "T");
        PlayerOn(32, 30, "Ally");
        npc.AddAggro(target, 100, kit.World);

        for (int i = 0; i < 3; i++)
        {
            long hp = target.CurrentHP;
            Call("AttackStep", npc, Now + kit.World.TimerFrequency * i, kit.World);
            Assert.Equal(1_000, hp - target.CurrentHP);
        }

        long before = target.CurrentHP;
        Call("AttackStep", npc, Now + kit.World.TimerFrequency * 3, kit.World);
        Assert.Equal(0, before - target.CurrentHP);
        Assert.Contains(target.Sent, s => s.Contains("The Groundskeeper revs his chainsaw!"));

        var bigHit = PendingScriptTimers(kit.World).OrderBy(t => t.Ticks).First();
        bigHit.Ready(kit.World);

        Assert.Equal(before - 2_000, target.CurrentHP);
    }

    [Fact]
    public void Enrage_speeds_up_swings()
    {
        var npc = Spawn(30, 30);
        Assert.Equal(2.5, npc.AttackSpeed);
        npc.CurrentHP = (long)(0.05 * npc.MaxHP);

        Call("AttackStep", npc, Now, kit.World);

        Assert.Equal(1.5, npc.AttackSpeed);
        Assert.Equal(4, Phase(npc));
    }

    [Fact]
    public void Despawns_with_adds_when_basement_empty_for_60s()
    {
        var gm = PlayerOn(30, 30, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        var player = PlayerOn(31, 30, "A");
        var npc = Spawn(32, 30);
        var add = kit.Fixture.SpawnNpc(map, 33, 30, AddTemplate(), shouldRespawn: false,
            new PropertiesDictionary { ["gkAdd"] = "ghosts" });

        Call("EmptyCheck", npc, Now, kit.World);
        Assert.Equal(Now, LastPlayerSeenAt(npc));

        map.RemovePlayer(player, kit.World);

        Call("EmptyCheck", npc, Now + kit.World.TimerFrequency * 59, kit.World);
        Assert.Equal(NPC.States.Alive, npc.State);
        Assert.Equal(NPC.States.Alive, add.State);

        Call("EmptyCheck", npc, Now + kit.World.TimerFrequency * 60, kit.World);
        Assert.Equal(NPC.States.Dead, npc.State);
        Assert.Equal(NPC.States.Dead, add.State);
        Assert.Contains(gm.Sent, s => s.Contains("The Groundskeeper returns to the dark."));
    }

    [Fact]
    public void Death_despawns_adds()
    {
        var killer = PlayerOn(31, 30, "A");
        kit.Fixture.AddOnlinePlayer(killer);
        var npc = Spawn(30, 30);
        var add1 = kit.Fixture.SpawnNpc(map, 33, 30, AddTemplate(), shouldRespawn: false,
            new PropertiesDictionary { ["gkAdd"] = "ghosts" });
        var add2 = kit.Fixture.SpawnNpc(map, 34, 30, AddTemplate(), shouldRespawn: false,
            new PropertiesDictionary { ["gkAdd"] = "bloody" });

        npc.Attacked(killer, npc.MaxHP, kit.World);

        Assert.Equal(NPC.States.Dead, add1.State);
        Assert.Equal(NPC.States.Dead, add2.State);
        Assert.Contains(killer.Sent, s => s.Contains("[Event] The Groundskeeper has fallen!"));
    }

    private NPC Spawn(int x, int y) => kit.Fixture.SpawnNpc(map, x, y, template);

    private NPCTemplate AddTemplate() => kit.Fixture.AddNpcTemplate(92003, "Ghost Add");

    private void Adds(params (string Kind, int TemplateId, int Count)[] kinds)
    {
        kit.SetConfig(cfg =>
        {
            var adds = new JsonObject();
            foreach (var (kind, id, count) in kinds)
                adds[kind] = new JsonArray(new JsonArray(id, count));
            cfg["groundskeeper"]!["adds"] = adds;
        });
    }

    private List<NPC> AddsOn(Map map, string kind) =>
        map.NPCs.Where(n => n.State == NPC.States.Alive &&
            n.Properties.TryGetProperty<string>("gkAdd", out var k) && k == kind).ToList();

    private int Phase(NPC npc) => (int)npc.Properties["gkPhase"];

    private long LastPlayerSeenAt(NPC npc)
        => (long)npc.ScriptStore!.GetType().GetProperty("LastPlayerSeenAt")!.GetValue(npc.ScriptStore)!;

    private long Hit(NPC npc, Player attacker, long raw)
    {
        long before = npc.CurrentHP;
        npc.Attacked(attacker, raw, kit.World);
        return before - npc.CurrentHP;
    }

    private TestWorldFixture.CapturingPlayer PlayerOn(int x, int y, string name)
    {
        var player = kit.Fixture.CommandPlayerOn(map, x, y, name);
        player.PlayerID = x * 100 + y;
        player.MaxStats.HP = 10_000;
        player.CurrentHP = 10_000;
        map.AddPlayer(player, kit.World);
        return player;
    }

    private object? Call(string method, params object?[] args)
        => keeper.GetType().GetMethod(method)!.Invoke(keeper, args);

    private static List<ScriptTimerEvent> PendingScriptTimers(GameWorld world)
    {
        var queue = (PriorityQueue<Event, long>)typeof(Goose.EventHandler)
            .GetField("events", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(world.EventHandler)!;
        var pending = new List<Event>();
        while (queue.TryDequeue(out Event? ev, out _)) pending.Add(ev!);
        return pending.OfType<ScriptTimerEvent>().ToList();
    }
}
