using System.Linq;
using Goose;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenDamageRuleTests
{
    private const int CapEffect = 70001;
    private const int CurveEffect = 70002;

    [Fact]
    public void Cap_limits_each_hit_to_100k()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.SetConfig(cfg =>
        {
            cfg["effects"]!["weeperCap"] = CapEffect;
            cfg["weeper"]!["capPerHit"] = 100_000;
        });
        var cap = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCap.csx");
        var (boss, player) = SpawnBoss(kit, map, CapEffect, cap);

        Assert.Equal(100_000, Hit(kit, boss, player, 3_000_000));
        Assert.Equal(50_000, Hit(kit, boss, player, 50_000));
    }

    [Fact]
    public void Curve_examples_match_design()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.SetConfig(cfg => cfg["effects"]!["groundskeeperCurve"] = CurveEffect);
        var curve = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCurve.csx");
        var (boss, player) = SpawnBoss(kit, map, CurveEffect, curve);

        AssertDamage(kit, boss, player, 5_000, 5_000);
        AssertDamage(kit, boss, player, 20_000, 42_000);
        AssertDamage(kit, boss, player, 40_000, 60_000);
        AssertDamage(kit, boss, player, 80_000, 80_000);
        AssertDamage(kit, boss, player, 300_000, 134_000);
        AssertDamage(kit, boss, player, 800_000, 195_000);
        AssertDamage(kit, boss, player, 3_000_000, 324_000);
    }

    [Fact]
    public void Curve_is_continuous_at_floor()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.SetConfig(cfg => cfg["effects"]!["groundskeeperCurve"] = CurveEffect);
        var curve = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCurve.csx");
        var (boss, player) = SpawnBoss(kit, map, CurveEffect, curve);

        Assert.Equal(10_000, Hit(kit, boss, player, 10_000));
        long justAbove = Hit(kit, boss, player, 10_001);
        Assert.True(justAbove > 10_000);
        Assert.InRange(justAbove, 10_000, 11_000);
    }

    [Fact]
    public void Heals_pass_through()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.SetConfig(cfg =>
        {
            cfg["effects"]!["weeperCap"] = CapEffect;
            cfg["effects"]!["groundskeeperCurve"] = CurveEffect;
            cfg["weeper"]!["capPerHit"] = 100_000;
        });
        var cap = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCap.csx");
        var curve = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCurve.csx");
        var (capBoss, player) = SpawnBoss(kit, map, CapEffect, cap);
        var (curveBoss, _) = SpawnBoss(kit, map, CurveEffect, curve);

        Assert.Equal(-50, (long)HalloweenTestKit.Call(cap.Object, "InterceptDamage",
            capBoss.Buffs.First(), player, -50, -50, kit.World));
        Assert.Equal(-50, (long)HalloweenTestKit.Call(curve.Object, "InterceptDamage",
            curveBoss.Buffs.First(), player, -50, -50, kit.World));
    }

    [Fact]
    public void Bloody_ghosts_alive_reduce_curve_damage()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.SetConfig(cfg => cfg["effects"]!["groundskeeperCurve"] = CurveEffect);
        var curve = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCurve.csx");
        var (boss, player) = SpawnBoss(kit, map, CurveEffect, curve);

        AssertDamage(kit, boss, player, 80_000, 80_000);

        var add = kit.Fixture.SpawnNpc(map, 6, 6,
            kit.Fixture.AddNpcTemplate(93002, "Bloody Add"));
        add.Properties["gkAdd"] = "bloody";

        AssertDamage(kit, boss, player, 80_000, 20_000);
    }

    [Fact]
    public void Disabled_config_passes_damage_through()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var cap = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCap.csx");
        var curve = kit.Compile<ISpellEffectScript>("Spell", "HalloweenDamageCurve.csx");
        var (capBoss, player) = SpawnBoss(kit, map, CapEffect, cap);
        var (curveBoss, _) = SpawnBoss(kit, map, CurveEffect, curve);

        Assert.Equal(3_000_000, Hit(kit, capBoss, player, 3_000_000));
        Assert.Equal(3_000_000, Hit(kit, curveBoss, player, 3_000_000));
    }

    private static (NPC Boss, Player Player) SpawnBoss(HalloweenTestKit kit, Map map,
        int effectId, Script<ISpellEffectScript> script)
    {
        var effect = kit.Fixture.AddBaseSpellEffect(effectId, "Boss Rule", e => e.Script = script);
        var boss = kit.Fixture.SpawnNpc(map, 5, 5,
            kit.Fixture.AddNpcTemplate(93001, "Boss", t => t.BaseStats.HP = 10_000_000));
        boss.AddBuff(new Buff { Caster = boss, Target = boss, SpellEffect = effect }, kit.World);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);
        return (boss, player);
    }

    private static long Hit(HalloweenTestKit kit, NPC boss, Player player, long raw)
    {
        long before = boss.CurrentHP;
        boss.Attacked(player, raw, kit.World);
        return before - boss.CurrentHP;
    }

    private static void AssertDamage(HalloweenTestKit kit, NPC boss, Player player, long raw, long expected)
    {
        long dealt = Hit(kit, boss, player, raw);
        Assert.InRange(dealt, expected - 1_000, expected + 1_000);
    }
}
