using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;

namespace Goose.Tests.Arena;

public sealed class ArenaRoundTests : IDisposable
{
    private readonly ArenaTestKit kit = new();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public void Dispose() => kit.Dispose();

    private static object? Run(ArenaTestKit kit) => ArenaTestKit.Call(kit.Host, "Run", kit.World);
    private static object? LastRun(ArenaTestKit kit) => ArenaTestKit.Call(kit.Host, "LastRun", kit.World);
    private static void SaveRun(ArenaTestKit kit, object run) => ArenaTestKit.Call(kit.Host, "SaveRun", kit.World, run);
    private static void StartRun(ArenaTestKit kit, DateTime now) => ArenaTestKit.Call(kit.Host, "StartRun", kit.World, now);
    private static void Tick(ArenaTestKit kit, DateTime now) => ArenaTestKit.Call(kit.Host, "Tick", kit.World, now);
    private static void Eliminate(ArenaTestKit kit, int id, string reason) => ArenaTestKit.Call(kit.Host, "Eliminate", kit.World, id, reason);

    private static object? Prop(object? obj, string name) => ArenaTestKit.GetProp(obj, name);
    private static void SetProp(object obj, string name, object? value) => obj.GetType().GetProperty(name)!.SetValue(obj, value);
    private static System.Collections.IDictionary Participants(object run) => (System.Collections.IDictionary)Prop(run, "Participants")!;

    private static Map ArenaMap(ArenaTestKit kit) => kit.World.MapHandler.Maps[ArenaTestKit.ArenaMapId];
    private static List<NPC> Mobs(ArenaTestKit kit) => ArenaMap(kit).NPCs.ToList();

    private static void EnterRound1(ArenaTestKit kit, params Player[] players)
    {
        StartRun(kit, Now);
        foreach (var p in players) kit.Enter(ArenaMap(kit), p);
        Tick(kit, Now.AddSeconds(10));
    }

    private static void KillAll(ArenaTestKit kit, Player killer)
    {
        foreach (var npc in Mobs(kit)) npc.Attacked(killer, long.MaxValue / 4, kit.World);
    }

    private static int Chebyshev(int x1, int y1, int x2, int y2)
        => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));

    [Fact]
    public void Countdown_then_round_1_spawns_base_plus_per_player()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        var p2 = kit.LobbyPlayer(70002, "P2");
        var p3 = kit.LobbyPlayer(70003, "P3");
        StartRun(kit, Now);
        foreach (var p in new[] { p1, p2, p3 }) kit.Enter(ArenaMap(kit), p);

        Tick(kit, Now.AddSeconds(5));
        Assert.Equal(0, (int)Prop(Run(kit)!, "Phase")!);
        Assert.Empty(Mobs(kit));

        Tick(kit, Now.AddSeconds(10));
        var run = Run(kit)!;
        Assert.Equal(1, (int)Prop(run, "Round")!);
        Assert.Equal(1, (int)Prop(run, "Phase")!);
        Assert.Equal(Now.AddSeconds(190), (DateTime)Prop(run, "PhaseUntilUtc")!);
        var mobs = Mobs(kit);
        Assert.Equal(10, mobs.Count);
        var runId = (string)Prop(run, "RunId")!;
        Assert.All(mobs, m => Assert.Equal(runId, m.Properties.GetProperty<string>("arena.run")));
        Assert.All(mobs, m => Assert.Equal(1, m.Properties.GetProperty<int>("arena.round")));
        Assert.All(mobs, m => Assert.True(m.Properties.GetProperty<bool>("untameable", false)));
        foreach (var p in new[] { p1, p2, p3 })
            Assert.Contains(p.Sent, s => s.Contains("[Arena] Round 1!"));
    }

    [Fact]
    public void Players_and_mobs_are_only_placed_on_floor_connected_to_the_rect_centre()
    {
        for (int y = 1; y <= 100; y++)
            ArenaMap(kit).SetTile(30, y, new BlockedTile());
        var players = Enumerable.Range(0, 6).Select(i => kit.LobbyPlayer(70001 + i, "P" + i)).ToArray();

        StartRun(kit, Now);
        Assert.All(players, p => Assert.True(p.MapX > 30, $"player at {p.MapX},{p.MapY}"));
        Tick(kit, Now.AddSeconds(10));

        var mobs = Mobs(kit);
        Assert.NotEmpty(mobs);
        Assert.All(mobs, m => Assert.True(m.MapX > 30, $"mob at {m.MapX},{m.MapY}"));
    }

    [Fact]
    public void Spawns_respect_min_player_distance()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        var p2 = kit.LobbyPlayer(70002, "P2");
        EnterRound1(kit, p1, p2);

        foreach (var npc in Mobs(kit))
            foreach (var p in new[] { p1, p2 })
                Assert.True(Chebyshev(npc.MapX, npc.MapY, p.MapX, p.MapY) >= 3);
    }

    [Fact]
    public void Spawns_fall_back_when_crowded()
    {
        kit.SetConfig(j => j["arenaRect"] = new JsonArray(50, 50, 3, 3));
        var p1 = kit.LobbyPlayer(70001, "P1");
        var p2 = kit.LobbyPlayer(70002, "P2");
        StartRun(kit, Now);
        var map = ArenaMap(kit);
        p1.MapX = 51; p1.MapY = 51;
        p2.MapX = 52; p2.MapY = 52;
        map.SetCharacter(p1, 51, 51);
        map.SetCharacter(p2, 52, 52);
        kit.Enter(map, p1);
        kit.Enter(map, p2);

        Tick(kit, Now.AddSeconds(10));

        var run = Run(kit)!;
        Assert.Equal(1, (int)Prop(run, "Round")!);
        Assert.Equal(1, (int)Prop(run, "Phase")!);
        Assert.InRange(Mobs(kit).Count, 1, 7);
    }

    [Fact]
    public void Clear_pays_round_payout_and_breaks_then_next_round()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        EnterRound1(kit, p1);

        KillAll(kit, p1);
        Tick(kit, Now.AddSeconds(11));

        var run = Run(kit)!;
        Assert.Equal(1, (int)Prop(Participants(run)[70001], "RoundsCleared")!);
        Assert.Equal(6, (long)Prop(Participants(run)[70001], "Coins")!);
        Assert.Equal(6, kit.Wallet(p1));
        Assert.Equal(2, (int)Prop(run, "Phase")!);
        Assert.Equal(Now.AddSeconds(26), (DateTime)Prop(run, "PhaseUntilUtc")!);

        Tick(kit, Now.AddSeconds(26));
        var next = Run(kit)!;
        Assert.Equal(2, (int)Prop(next, "Round")!);
        Assert.Equal(1, (int)Prop(next, "Phase")!);
        Assert.Equal(6, Mobs(kit).Count);
    }

    [Fact]
    public void Clear_and_countdown_and_coins_go_to_notices()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        EnterRound1(kit, p1);
        KillAll(kit, p1);
        p1.Sent.Clear();

        Tick(kit, Now.AddSeconds(11));
        Assert.Contains(p1.Sent, s => s.StartsWith("NTF0,Round 1 cleared! Round 2 starts in 15 seconds."));
        Assert.Contains(p1.Sent, s => s.StartsWith("NTF0,+6 Arena Coins (round 1 clear)"));

        p1.Sent.Clear();
        for (int second = 12; second < 26; second++)
            Tick(kit, Now.AddSeconds(second));
        Tick(kit, Now.AddSeconds(25.5));
        var countdown = p1.Sent.Where(s => s.StartsWith("NTF0,Round 2 starts in")).Select(s => s.TrimEnd('\x01')).ToList();
        Assert.Equal(new[]
        {
            "NTF0,Round 2 starts in 10...", "NTF0,Round 2 starts in 5...", "NTF0,Round 2 starts in 3...",
            "NTF0,Round 2 starts in 2...", "NTF0,Round 2 starts in 1...",
        }, countdown.ToArray());
    }

    [Fact]
    public void Mobs_spawn_within_spawn_radius_of_a_participant()
    {
        kit.SetConfig(j => j["spawnRadius"] = 6);
        var p1 = kit.LobbyPlayer(70001, "P1");
        var p2 = kit.LobbyPlayer(70002, "P2");
        StartRun(kit, Now);
        foreach (var p in new[] { p1, p2 }) kit.Enter(ArenaMap(kit), p);
        p1.WarpTo(kit.World, ArenaMap(kit), 15, 15);
        p2.WarpTo(kit.World, ArenaMap(kit), 80, 80);

        Tick(kit, Now.AddSeconds(10));

        var mobs = Mobs(kit);
        Assert.NotEmpty(mobs);
        Assert.All(mobs, m => Assert.True(
            Chebyshev(m.MapX, m.MapY, 15, 15) <= 6 || Chebyshev(m.MapX, m.MapY, 80, 80) <= 6,
            $"mob at {m.MapX},{m.MapY}"));
    }

    [Fact]
    public void Boss_round_spawns_boss_and_fewer_adds()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        EnterRound1(kit, p1);
        KillAll(kit, p1);

        var run = Run(kit)!;
        SetProp(run, "Round", 4);
        SetProp(run, "Phase", 2);
        SetProp(run, "PhaseUntilUtc", Now);
        SaveRun(kit, run);

        Tick(kit, Now.AddSeconds(11));

        var next = Run(kit)!;
        Assert.Equal(5, (int)Prop(next, "Round")!);
        Assert.Equal(1, (int)Prop(next, "Phase")!);
        var mobs = Mobs(kit);
        Assert.Equal(3, mobs.Count);
        var boss = mobs.Single(m => m.Properties.GetProperty<int>("arena.tier") == 2);
        Assert.Equal(ArenaTestKit.BandBossId, boss.NPCTemplateID);
        var adds = mobs.Where(m => m != boss).ToList();
        Assert.Equal(2, adds.Count);
        foreach (var m in adds)
        {
            Assert.Contains(m.NPCTemplateID, ArenaTestKit.BandNormalIds);
            Assert.InRange(m.Properties.GetProperty<int>("arena.tier"), 0, 1);
        }
        Assert.Contains(p1.Sent, s => s.Contains("[Arena] Boss round!"));
    }

    [Fact]
    public void Timeout_warns_once_then_fails_run_and_warps_survivors_to_lobby()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        EnterRound1(kit, p1);
        var until = (DateTime)Prop(Run(kit)!, "PhaseUntilUtc")!;

        Tick(kit, until.AddSeconds(-20));
        Assert.Equal(1, p1.Sent.Count(s => s.Contains("enemies remain")));

        Tick(kit, until.AddSeconds(-10));
        Assert.Equal(1, p1.Sent.Count(s => s.Contains("enemies remain")));

        Tick(kit, until.AddSeconds(1));

        Assert.Null(Run(kit));
        var last = LastRun(kit);
        Assert.NotNull(last);
        Assert.Equal(0, (int)Prop(last!, "BestRound")!);
        Assert.Equal(ArenaTestKit.LobbyMapId, p1.MapID);
        Assert.Equal(10, p1.MapX);
        Assert.Equal(10, p1.MapY);
        Assert.Equal(0, kit.Wallet(p1));
    }

    [Fact]
    public void Straggler_recall_moves_last_mobs_near_a_participant()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        EnterRound1(kit, p1);
        var until = (DateTime)Prop(Run(kit)!, "PhaseUntilUtc")!;

        foreach (var npc in Mobs(kit).Take(4))
            npc.Attacked(p1, long.MaxValue / 4, kit.World);

        Tick(kit, until.AddSeconds(-20));

        var remaining = Mobs(kit);
        Assert.Equal(2, remaining.Count);
        foreach (var npc in remaining)
            Assert.True(Chebyshev(npc.MapX, npc.MapY, p1.MapX, p1.MapY) <= 2);
    }

    [Fact]
    public void Bounty_goes_to_the_killer_and_not_to_a_participant_under_the_kill_share()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        p1.ExperienceSold = 3_000_000_000;
        EnterRound1(kit, p1);

        foreach (var npc in Mobs(kit).Take(5))
            npc.Attacked(p1, long.MaxValue / 4, kit.World);

        kit.RegisterController();
        var p2 = kit.LobbyPlayer(70002, "P2", gm: true);
        p2.ExperienceSold = 3_000_000_000;
        kit.Enter(ArenaMap(kit), p2);

        var mob = Mobs(kit).Single();
        mob.Attacked(p1, 1_000_000, kit.World);
        mob.Attacked(p2, 500_000, kit.World);
        mob.Attacked(p1, long.MaxValue / 4, kit.World);

        Assert.Equal(1, kit.Wallet(p1));
        Assert.Contains(p1.Sent, s => s.Contains("+1 Arena Coins (kill bounty)"));
        Assert.Equal(1.0, (double)Prop(Participants(Run(kit)!)[70001], "Kills")!);
        Assert.Equal(0, kit.Wallet(p2));
        Assert.DoesNotContain(p2.Sent, s => s.Contains("Arena Coins"));
    }

    [Fact]
    public void Kill_credited_to_participants_with_at_least_five_percent_of_damage()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        var p2 = kit.LobbyPlayer(70002, "P2");
        p1.ExperienceSold = 3_000_000_000;
        p2.ExperienceSold = 3_000_000_000;
        EnterRound1(kit, p1, p2);
        kit.RegisterController();

        var mobs = Mobs(kit);
        mobs[0].Attacked(p2, mobs[0].CurrentHP / 50, kit.World);
        mobs[0].Attacked(p1, long.MaxValue / 4, kit.World);
        Assert.Equal(1.0, (double)Prop(Participants(Run(kit)!)[70001], "Kills")!);
        Assert.Equal(0.0, (double)Prop(Participants(Run(kit)!)[70002], "Kills")!);

        mobs[1].Attacked(p2, mobs[1].CurrentHP / 4, kit.World);
        mobs[1].Attacked(p1, long.MaxValue / 4, kit.World);
        Assert.Equal(2.0, (double)Prop(Participants(Run(kit)!)[70001], "Kills")!);
        Assert.Equal(1.0, (double)Prop(Participants(Run(kit)!)[70002], "Kills")!);
    }

    [Fact]
    public void Grouped_priest_gets_partial_kill_credit_without_enough_damage()
    {
        var killer = kit.LobbyPlayer(70001, "Killer");
        var priest = kit.LobbyPlayer(70002, "Priest");
        var loner = kit.LobbyPlayer(70003, "Loner");
        var friend = kit.LobbyPlayer(70004, "Friend");
        foreach (var p in new[] { killer, priest, loner, friend }) p.ExperienceSold = 3_000_000_000;
        priest.ClassID = 5;
        loner.ClassID = 5;
        friend.ClassID = 3;
        var group = new Group();
        foreach (var p in new Player[] { killer, priest, friend })
        {
            p.Group = group;
            group.Players.Add(p);
        }
        EnterRound1(kit, killer, priest, loner, friend);
        kit.RegisterController();

        Mobs(kit)[0].Attacked(killer, long.MaxValue / 4, kit.World);

        var parts = Participants(Run(kit)!);
        Assert.Equal(1.0, (double)Prop(parts[70001], "Kills")!);
        Assert.Equal(0.5, (double)Prop(parts[70002], "Kills")!);
        Assert.Equal(0.0, (double)Prop(parts[70003], "Kills")!);
        Assert.Equal(0.0, (double)Prop(parts[70004], "Kills")!);
    }

    [Fact]
    public void Missing_template_round_does_not_stall()
    {
        kit.SetConfig(j => j["bands"]![0]!["normals"] = new JsonArray(99999));
        var p1 = kit.LobbyPlayer(70001, "P1");
        EnterRound1(kit, p1);

        var run = Run(kit)!;
        Assert.Equal(1, (int)Prop(run, "Round")!);
        Assert.Equal(1, (int)Prop(run, "Phase")!);
        Assert.Empty(Mobs(kit));

        Tick(kit, Now.AddSeconds(11));
        Assert.Equal(2, (int)Prop(Run(kit)!, "Phase")!);
    }

    [Fact]
    public void Straggler_recall_skipped_when_more_than_straggler_count()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        EnterRound1(kit, p1);
        var until = (DateTime)Prop(Run(kit)!, "PhaseUntilUtc")!;

        foreach (var npc in Mobs(kit).Take(2))
            npc.Attacked(p1, long.MaxValue / 4, kit.World);

        var remaining = Mobs(kit);
        Assert.Equal(4, remaining.Count);
        var before = remaining.ToDictionary(n => n.LoginID, n => (n.MapX, n.MapY));

        Tick(kit, until.AddSeconds(-20));

        Assert.Equal(1, p1.Sent.Count(s => s.Contains("enemies remain")));
        foreach (var (loginId, pos) in before)
        {
            var npc = Mobs(kit).Single(n => n.LoginID == loginId);
            Assert.Equal(pos, (npc.MapX, npc.MapY));
        }
    }

    [Fact]
    public void Disabled_config_mid_run_ends_the_run()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        StartRun(kit, Now);
        kit.Enter(ArenaMap(kit), p1);

        kit.SetConfig(j => j["a"] = 3.5);
        Tick(kit, Now.AddSeconds(1));

        Assert.Null(Run(kit));
        Assert.NotNull(LastRun(kit));
    }

    [Fact]
    public void Mob_retargets_when_target_eliminated()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        var p2 = kit.LobbyPlayer(70002, "P2");
        EnterRound1(kit, p1, p2);

        var mobs = Mobs(kit);
        var mob = mobs.FirstOrDefault(m => m.AggroTarget == p1);
        if (mob is null)
        {
            mob = mobs[0];
            mob.AddAggro(p1, 100, kit.World);
        }
        mob.Attacked(p1, 100, kit.World);
        mob.Attacked(p2, 10, kit.World);

        Eliminate(kit, 70001, "test");
        Tick(kit, Now.AddSeconds(11));

        Assert.Same(p2, mob.AggroTarget);
    }

    [Fact]
    public void Killer_group_members_nearby_each_get_the_full_bounty()
    {
        var killer = kit.LobbyPlayer(70001, "Killer");
        var near = kit.LobbyPlayer(70002, "Near");
        var lowLevel = kit.LobbyPlayer(70003, "LowLevel");
        var far = kit.LobbyPlayer(70004, "Far");
        var stranger = kit.LobbyPlayer(70005, "Stranger");
        foreach (var p in new[] { killer, near, lowLevel, far, stranger })
        {
            p.ExperienceSold = 3_000_000_000;
            p.Level = 50;
        }
        lowLevel.Level = 40;
        var group = new Group();
        foreach (var p in new Player[] { killer, near, lowLevel, far })
        {
            p.Group = group;
            group.Players.Add(p);
        }
        EnterRound1(kit, killer, near, lowLevel, far, stranger);
        kit.RegisterController();

        var mob = Mobs(kit)[0];
        var map = ArenaMap(kit);
        foreach (var (p, dx) in new[] { (near, 2), (lowLevel, 2), (far, Map.RANGE_X + 1) })
        {
            map.SetCharacter(null, p.MapX, p.MapY);
            p.MapX = mob.MapX + dx;
            p.MapY = mob.MapY;
        }
        mob.Attacked(stranger, mob.CurrentHP / 50, kit.World);
        mob.Attacked(killer, long.MaxValue / 4, kit.World);

        Assert.Equal(1, kit.Wallet(killer));
        Assert.Equal(1, kit.Wallet(near));
        Assert.Equal(0, kit.Wallet(lowLevel));
        Assert.Equal(0, kit.Wallet(far));
        Assert.Equal(0, kit.Wallet(stranger));
        var parts = Participants(Run(kit)!);
        Assert.Equal(1.0, (double)Prop(parts[70001], "Kills")!);
        Assert.Equal(0.0, (double)Prop(parts[70002], "Kills")!);
    }

    [Fact]
    public void Eliminated_participant_gets_no_bounty_for_a_mob_they_damaged()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        var p2 = kit.LobbyPlayer(70002, "P2");
        p1.ExperienceSold = 3_000_000_000;
        p2.ExperienceSold = 3_000_000_000;
        EnterRound1(kit, p1, p2);
        kit.RegisterController();

        var mob = Mobs(kit)[0];
        mob.Attacked(p2, mob.CurrentHP / 2, kit.World);
        Eliminate(kit, 70002, "test");
        mob.Attacked(p1, long.MaxValue / 4, kit.World);

        Assert.Equal(0, kit.Wallet(p2));
        Assert.Equal(1, kit.Wallet(p1));
    }

    [Fact]
    public void Milestone_is_paid_on_elimination_not_when_the_run_ends()
    {
        var p1 = kit.LobbyPlayer(70001, "P1");
        var p2 = kit.LobbyPlayer(70002, "P2");
        EnterRound1(kit, p1, p2);
        var run = Run(kit)!;
        SetProp(Participants(run)[70001], "RoundsCleared", 5);
        SaveRun(kit, run);

        Eliminate(kit, 70001, "test");
        Assert.Equal(20, kit.Wallet(p1));

        Eliminate(kit, 70002, "test");
        Assert.Equal(20, kit.Wallet(p1));
        Assert.Equal(20L, (long)Prop(((System.Collections.IEnumerable)Prop(LastRun(kit)!, "Results")!).Cast<object>().First(r => (string)Prop(r, "Name")! == "P1"), "Coins")!);
    }
}
