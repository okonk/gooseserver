using System.Text.Json.Nodes;
using Goose.Testing;

namespace Goose.Tests.Arena;

public sealed class ArenaBossTests : IDisposable
{
    private readonly ArenaTestKit kit;
    private readonly Map map;
    private int nextLogin = 1;

    public ArenaBossTests()
    {
        kit = new ArenaTestKit();
        map = kit.ArenaMap;
        kit.SetConfig(j => j["bossMechanics"] = JsonNode.Parse("""
            {
                "animation": 900001,
                "animationFile": 30022,
                "gapSeconds": [8, 12],
                "slam": { "fromRound": 1, "damage": 3, "radius": 5, "windup": 2 },
                "pull": { "fromRound": 1, "range": 8, "windup": 2.5 },
                "charge": { "fromRound": 1, "damage": 2.5, "length": 8, "windup": 1.5 },
                "mark": { "fromRound": 1, "damage": 4, "size": 3, "windup": 3 },
                "safeZone": { "fromRound": 1, "damage": 6, "blockWidth": 15, "blockHeight": 8, "maxDistance": 20, "windup": 6 },
                "swap": { "fromRound": 1, "everySeconds": [45, 60], "windup": 1.5, "lockSeconds": 10, "emote": [1081, 8] },
                "summon": { "fromRound": 1, "atHp": [0.66, 0.33], "base": 2, "perPlayers": 3 }
            }
            """));
    }

    public void Dispose() => kit.Dispose();

    private object? Call(string method, params object?[] args) => ArenaTestKit.Call(kit.Host, method, args);

    private NPC Boss(int x, int y, int round = 25)
    {
        var template = kit.World.NPCHandler.GetNPCTemplate(ArenaTestKit.BandBossId)!;
        return kit.Fixture.SpawnNpc(map, x, y, template, false, new PropertiesDictionary
        {
            ["arena.run"] = "r1",
            ["arena.round"] = round,
            ["arena.tier"] = 2,
            ["arena.alive"] = 2,
        });
    }

    private TestWorldFixture.CapturingPlayer Player(int x, int y)
    {
        int id = 70000 + nextLogin++;
        var player = kit.Fixture.CommandPlayerOn(map, x, y, "P" + id);
        player.PlayerID = id;
        player.ExperienceSold = 3_000_000_000;
        player.MaxStats.HP = 1_000_000_000;
        player.CurrentHP = 1_000_000_000;
        kit.Fixture.AddOnlinePlayer(player);
        map.Players.Add(player);
        map.SetCharacter(player, x, y);
        return player;
    }

    private static void Place(Map map, Player player, int x, int y)
    {
        map.SetCharacter(null, player.MapX, player.MapY);
        player.MapX = x;
        player.MapY = y;
        map.SetCharacter(player, x, y);
    }

    private List<(int X, int Y)> Tiles(object telegraph) => (List<(int X, int Y)>)Call("BossTiles", telegraph)!;

    private static bool Hurt(Player p) => p.CurrentHP < 1_000_000_000;

    [Fact]
    public void Slam_marks_a_ring_and_hits_everyone_inside_it()
    {
        var boss = Boss(40, 40);
        var inside = Player(43, 40);
        var outside = Player(47, 40);

        var slam = Call("BossStart", "slam", boss, kit.World)!;

        Assert.All(Tiles(slam), t =>
        {
            double d = Math.Sqrt((t.X - 40) * (t.X - 40) + (t.Y - 40) * (t.Y - 40));
            Assert.InRange(d, 4.0001, 5);
        });
        Assert.Contains((45, 40), Tiles(slam));
        Call("BossResolve", slam);
        Assert.True(Hurt(inside));
        Assert.False(Hurt(outside));
    }

    [Fact]
    public void Pull_drags_nearby_players_next_to_the_boss_then_slams()
    {
        var boss = Boss(40, 40);
        var near = Player(46, 40);
        var far = Player(55, 40);

        var slam = Call("BossStart", "pull", boss, kit.World)!;

        Assert.True(Math.Max(Math.Abs(near.MapX - 40), Math.Abs(near.MapY - 40)) <= 1);
        Assert.Equal((55, 40), (far.MapX, far.MapY));
        Call("BossResolve", slam);
        Assert.True(Hurt(near));
        Assert.False(Hurt(far));
    }

    [Fact]
    public void Charge_hits_the_line_towards_the_target_and_moves_the_boss_to_its_end()
    {
        var boss = Boss(40, 40);
        var target = Player(44, 41);
        var onLine = Player(42, 40);
        boss.AddAggro(target, 1, kit.World);

        var charge = Call("BossStart", "charge", boss, kit.World)!;

        Assert.Equal(Enumerable.Range(41, 8).Select(x => (x, 40)).ToList(), Tiles(charge));
        Call("BossResolve", charge);
        Assert.True(Hurt(onLine));
        Assert.False(Hurt(target));
        Assert.Equal((48, 40), (boss.MapX, boss.MapY));
    }

    [Fact]
    public void Mark_hits_the_three_by_three_tiles_where_the_player_stood()
    {
        var boss = Boss(40, 40);
        var marked = Player(50, 50);

        var mark = Call("BossStart", "mark", boss, kit.World)!;

        Assert.Equal(9, Tiles(mark).Count);
        Assert.Contains((49, 49), Tiles(mark));
        Assert.Contains((51, 51), Tiles(mark));
        var stayed = Player(51, 49);
        Place(map, marked, 53, 50);
        Call("BossResolve", mark);
        Assert.False(Hurt(marked));
        Assert.True(Hurt(stayed));
    }

    [Fact]
    public void Safe_zone_hits_everyone_outside_the_marked_block()
    {
        var boss = Boss(40, 40);
        var zone = Call("BossStart", "safeZone", boss, kit.World)!;
        var edge = Tiles(zone);
        int minX = edge.Min(t => t.X) + 1, minY = edge.Min(t => t.Y) + 1;
        int maxX = edge.Max(t => t.X) - 1, maxY = edge.Max(t => t.Y) - 1;
        Assert.Equal(15, maxX - minX + 1);
        Assert.Equal(8, maxY - minY + 1);

        var safe = Player(minX + 2, minY + 2);
        var caught = Player(maxX + 3 > 89 ? minX - 3 : maxX + 3, minY + 2);
        Call("BossResolve", zone);

        Assert.False(Hurt(safe));
        Assert.True(Hurt(caught));
    }

    [Fact]
    public void Swap_teleports_behind_the_top_damage_dealer_and_holds_them_despite_taunt()
    {
        var boss = Boss(40, 40);
        var tank = Player(41, 40);
        var dps = Player(60, 60);
        dps.Facing = Direction.Right;
        boss.Properties["arena.dmg." + dps.PlayerID] = 1000L;
        boss.Properties["arena.dmg." + tank.PlayerID] = 10L;
        boss.AddAggro(tank, 1_000_000, true, kit.World);

        var swap = Call("BossStart", "swap", boss, kit.World)!;

        Assert.Equal(new List<(int, int)> { (60, 60) }, Tiles(swap));
        Call("BossResolve", swap);
        Assert.Equal((59, 60), (boss.MapX, boss.MapY));
        Assert.Same(dps, boss.AggroTarget);

        boss.AddAggro(tank, 1_000_000, true, kit.World);
        Call("BossHold", boss, kit.World);
        Assert.Same(dps, boss.AggroTarget);
    }

    [Fact]
    public void Crossing_a_summon_threshold_calls_elite_guards_and_shields_the_boss_until_they_die()
    {
        var boss = Boss(40, 40);
        var player = Player(41, 40);
        long floor = (long)Math.Ceiling(boss.MaxHP * 0.66);

        boss.Attacked(player, boss.MaxHP / 2, kit.World);

        Assert.Equal(floor, boss.CurrentHP);
        var guards = map.NPCs.Where(n => n != boss && n.State == NPC.States.Alive).ToList();
        Assert.Equal(2, guards.Count);
        Assert.All(guards, g => Assert.Equal(1, g.Properties.GetProperty<int>("arena.tier", 0)));
        Assert.True((bool)Call("BossShielded", boss)!);

        boss.Attacked(player, 1000, kit.World);
        Assert.Equal(floor, boss.CurrentHP);

        foreach (var guard in guards) guard.Attacked(player, long.MaxValue / 4, kit.World);
        Assert.False((bool)Call("BossShielded", boss)!);
        boss.Attacked(player, 1000, kit.World);
        Assert.Equal(floor - 1000, boss.CurrentHP);
    }

    [Fact]
    public void Think_uses_only_mechanics_unlocked_for_the_round_and_blocks_while_winding_up()
    {
        kit.SetConfig(j =>
        {
            var m = j["bossMechanics"]!.AsObject();
            foreach (var key in new[] { "pull", "charge", "mark", "safeZone", "swap" })
                m[key]!["fromRound"] = 30;
        });
        var boss = Boss(40, 40, round: 25);
        Player(43, 40);
        boss.Properties["arena.boss.next"] = 0L;

        Assert.True((bool)Call("BossThink", boss, kit.World)!);
        Assert.Equal("slam", boss.Properties.GetProperty<string>("arena.boss.last", ""));
        Assert.True((bool)Call("BossBusy", boss, kit.World)!);
        Assert.False((bool)Call("BossThink", boss, kit.World)!);
    }

    [Fact]
    public void Low_rounds_get_no_mechanics()
    {
        kit.SetConfig(j =>
        {
            foreach (var (_, node) in j["bossMechanics"]!.AsObject())
                if (node is JsonObject o && o.ContainsKey("fromRound")) o["fromRound"] = 30;
        });
        var boss = Boss(40, 40, round: 25);
        boss.Properties["arena.boss.next"] = 0L;
        boss.Properties["arena.boss.swapAt"] = 0L;

        Assert.False((bool)Call("BossThink", boss, kit.World)!);
    }
}
