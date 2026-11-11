using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Arena;

public sealed class ArenaMobTests : IDisposable
{
    private readonly ArenaTestKit kit;
    private readonly Map map;
    private readonly Script<INPCScript> mobScript;

    public ArenaMobTests()
    {
        kit = new ArenaTestKit();
        // The fixture's default MaxAC is 0, which makes MitigateMelee's absorb factor NaN and zeroes all melee damage.
        kit.Fixture.Settings.MaxAC = 255;
        map = kit.Fixture.AddBaseMap(2, "Mob Test", 40, 40);
        mobScript = kit.Compile<INPCScript>("Arena", "ArenaMob.csx");
    }

    public void Dispose() => kit.Dispose();

    private NPC SpawnMob(int id, int x, int y, int round = 20, int tier = 0, int alive = 4, bool tagged = true)
    {
        var properties = new PropertiesDictionary();
        if (tagged)
        {
            properties["arena.run"] = "r1";
            properties["arena.round"] = round;
            properties["arena.tier"] = tier;
            properties["arena.alive"] = alive;
        }
        var template = kit.Fixture.AddNpcTemplate(id, "Arena Mob " + id, t => t.Script = mobScript);
        return kit.Fixture.SpawnNpc(map, x, y, template, false, properties);
    }

    private Player Player(int id, int x, int y, long xpSold)
    {
        var player = kit.Fixture.CommandPlayerOn(map, x, y, "P" + id);
        player.PlayerID = id;
        player.ExperienceSold = xpSold;
        player.MaxStats.HP = 1_000_000_000;
        player.CurrentHP = 1_000_000_000;
        kit.Fixture.AddOnlinePlayer(player);
        map.Players.Add(player);
        return player;
    }

    [Fact]
    public void Spawn_sets_hp_from_round_and_tier()
    {
        var normal = SpawnMob(5001, 10, 10);
        var elite = SpawnMob(5002, 12, 10, tier: 1);
        var boss = SpawnMob(5003, 14, 10, tier: 2, alive: 5);

        Assert.Equal(12_000_000, normal.MaxHP);
        Assert.Equal(60_000_000, elite.MaxHP);
        Assert.Equal(960_000_000, boss.MaxHP);
    }

    [Fact]
    public void Spawn_sets_xp_from_round_and_tier_when_configured()
    {
        kit.SetConfig(j =>
        {
            j["xpAnchor"] = 1000;
            j["xpGrowth"] = 2;
            j["eliteXpMult"] = 3;
            j["bossXpMult"] = 10;
        });

        Assert.Equal(1000, SpawnMob(5004, 10, 10, round: 20).Experience);
        Assert.Equal(6000, SpawnMob(5005, 12, 10, round: 21, tier: 1).Experience);
        Assert.Equal(40000, SpawnMob(5006, 14, 10, round: 22, tier: 2).Experience);
    }

    [Fact]
    public void Spawn_keeps_template_xp_without_xp_anchor()
    {
        var template = kit.Fixture.AddNpcTemplate(5007, "Arena Mob 5007", t => { t.Script = mobScript; t.Experience = 909; });
        var npc = kit.Fixture.SpawnNpc(map, 10, 10, template, false, new PropertiesDictionary
        {
            ["arena.run"] = "r1",
            ["arena.round"] = 20,
        });

        Assert.Equal(909, npc.Experience);
    }

    [Fact]
    public void Elite_gets_title_and_tint()
    {
        var elite = SpawnMob(5011, 10, 10, tier: 1);

        Assert.Equal("Elite", elite.Title);
        Assert.Equal(255, elite.BodyR);
        Assert.Equal(160, elite.BodyG);
        Assert.Equal(0, elite.BodyB);
        Assert.Equal(255, elite.BodyA);

        var normal = SpawnMob(5012, 12, 10);
        Assert.Null(normal.Title);
        Assert.Equal(0, normal.BodyR);
        Assert.Equal(0, normal.BodyG);
        Assert.Equal(0, normal.BodyB);
        Assert.Equal(0, normal.BodyA);
    }

    [Fact]
    public void Untagged_spawn_keeps_template_hp()
    {
        var npc = SpawnMob(5021, 10, 10, tagged: false);

        Assert.Null(npc.TemporaryMaxHP);
        Assert.Equal(1000, npc.MaxHP);
    }

    [Fact]
    public void Low_xp_player_damage_is_lifted_and_logged()
    {
        var npc = SpawnMob(5031, 10, 10);
        var player = Player(70001, 12, 10, 20_000_000);
        long expected = (long)(35_000 * Math.Pow(20_000_000.0 / 3_000_000_000.0, -0.75));

        long before = npc.CurrentHP;
        npc.Attacked(player, 35_000, kit.World);
        long loss = before - npc.CurrentHP;

        Assert.InRange(loss, (long)(expected * 0.99), (long)(expected * 1.01));
        Assert.Equal(expected, npc.Properties.GetProperty<long>("arena.dmg.70001", 0));

        npc.Attacked(player, 35_000, kit.World);
        Assert.Equal(2 * expected, npc.Properties.GetProperty<long>("arena.dmg.70001", 0));
    }

    [Fact]
    public void Top_xp_player_damage_is_unchanged()
    {
        var npc = SpawnMob(5041, 10, 10);
        var player = Player(70002, 12, 10, 3_000_000_000);

        long before = npc.CurrentHP;
        npc.Attacked(player, 35_000, kit.World);

        Assert.Equal(35_000, before - npc.CurrentHP);
        Assert.Equal(35_000, npc.Properties.GetProperty<long>("arena.dmg.70002", 0));
    }

    [Fact]
    public void Mob_hits_low_xp_player_for_less_than_top_xp_player()
    {
        var low = Player(70003, 12, 10, 300_000_000);
        var top = Player(70004, 14, 10, 3_000_000_000);

        long lowBefore = low.CurrentHP;
        SpawnMob(5051, 13, 10).Attack(low, kit.World);
        long lowLoss = lowBefore - low.CurrentHP;

        long topBefore = top.CurrentHP;
        SpawnMob(5052, 13, 10).Attack(top, kit.World);
        long topLoss = topBefore - top.CurrentHP;

        double expected = Math.Pow(0.1, 0.75);
        Assert.InRange((double)lowLoss / topLoss, expected * 0.99, expected * 1.01);
    }

    [Fact]
    public void Melee_base_damage_passes_through_for_untagged_and_non_player()
    {
        var untagged = SpawnMob(5071, 10, 10, tagged: false);
        var player = Player(70005, 12, 10, 3_000_000_000);
        Assert.Equal(123.5, mobScript.Object.MeleeBaseDamage(untagged, player, 123.5, kit.World));

        var tagged = SpawnMob(5072, 14, 10);
        var other = SpawnMob(5073, 16, 10);
        Assert.Equal(456.75, mobScript.Object.MeleeBaseDamage(tagged, other, 456.75, kit.World));
    }

    [Fact]
    public void Damage_from_non_player_is_not_scaled()
    {
        var npc = SpawnMob(5061, 10, 10);
        var other = SpawnMob(5062, 12, 10);

        long result = (long)ArenaTestKit.Call(mobScript.Object, "InterceptDamageTaken", npc, other, 1_000L, kit.World)!;

        Assert.Equal(1_000, result);
        Assert.False(npc.Properties.Keys.Any(k => k.StartsWith("arena.dmg.")));
    }
}
