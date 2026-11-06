using System;
using System.Collections.Generic;
using Goose;
using Goose.Scripting;
using Xunit;

namespace Goose.Tests.Arena;

[Collection("NLog")]
public sealed class ArenaMathTests : IDisposable
{
    private const int Normal = 0, Elite = 1, Boss = 2;
    private readonly ArenaTestKit kit = new();

    public void Dispose() => kit.Dispose();

    private static object Cfg(ArenaTestKit kit) => (object)ArenaTestKit.Call(kit.Host, "Config", kit.World)!;

    private static double PowerFraction(ArenaTestKit kit, long xp)
        => (double)ArenaTestKit.Call(kit.Host, "PowerFraction", xp, Cfg(kit))!;

    private static long EffectiveDamage(ArenaTestKit kit, long raw, double s)
        => (long)ArenaTestKit.Call(kit.Host, "EffectiveDamage", raw, s, Cfg(kit))!;

    private static long MobHp(ArenaTestKit kit, int round, int tier, int alive)
        => (long)ArenaTestKit.Call(kit.Host, "MobHp", round, tier, alive, Cfg(kit))!;

    private static double MobWeaponDamage(ArenaTestKit kit, int round, int tier, double s)
        => (double)ArenaTestKit.Call(kit.Host, "MobWeaponDamage", round, tier, s, Cfg(kit))!;

    private static int WaveCount(ArenaTestKit kit, int round, int alive)
        => (int)ArenaTestKit.Call(kit.Host, "WaveCount", round, alive, Cfg(kit))!;

    private static bool IsBossRound(ArenaTestKit kit, int round)
        => (bool)ArenaTestKit.Call(kit.Host, "IsBossRound", round, Cfg(kit))!;

    private static double EliteChance(ArenaTestKit kit, int round)
        => (double)ArenaTestKit.Call(kit.Host, "EliteChance", round, Cfg(kit))!;

    private static int BandBoss(ArenaTestKit kit, int round)
    {
        var band = ArenaTestKit.Call(kit.Host, "BandFor", round, Cfg(kit))!;
        return (int)band.GetType().GetProperty("Boss")!.GetValue(band)!;
    }

    private static long RoundPayout(ArenaTestKit kit, int round)
        => (long)ArenaTestKit.Call(kit.Host, "RoundPayout", round, Cfg(kit))!;

    private static long Bounty(ArenaTestKit kit, int tier)
        => (long)ArenaTestKit.Call(kit.Host, "Bounty", tier, Cfg(kit))!;

    private static long MilestoneBonus(ArenaTestKit kit, int rounds)
        => (long)ArenaTestKit.Call(kit.Host, "MilestoneBonus", rounds, Cfg(kit))!;

    private static Dictionary<int, long> SplitBounty(ArenaTestKit kit, long bounty, Dictionary<int, long> dmg)
        => (Dictionary<int, long>)ArenaTestKit.Call(kit.Host, "SplitBounty", bounty, dmg)!;

    private static DateTime NextStartUtc(ArenaTestKit kit, DateTime now)
        => (DateTime)ArenaTestKit.Call(kit.Host, "NextStartUtc", now, Cfg(kit))!;

    private static void Approx(double actual, double expected)
        => Assert.InRange(actual, expected * 0.99, expected * 1.01);

    [Fact]
    public void PowerFraction_clamps_both_ends()
    {
        Assert.Equal(1.0, PowerFraction(kit, 3_000_000_000L), 10);
        Assert.Equal(1.0, PowerFraction(kit, 100_000_000_000L), 10);
        Assert.Equal(0.1, PowerFraction(kit, 300_000_000L), 10);
    }

    [Fact]
    public void Zero_xp_sold_counts_as_min()
    {
        double min = 4_000_000.0 / 3_000_000_000.0;
        Assert.Equal(min, PowerFraction(kit, 0), 10);
        Assert.Equal(min, PowerFraction(kit, -5), 10);
        Assert.Equal(min, PowerFraction(kit, 4_000_000), 10);
    }

    [Fact]
    public void Effective_damage_matches_design_table()
    {
        Assert.Equal(5_000_000L, EffectiveDamage(kit, 5_000_000, 1.0));
        Approx(EffectiveDamage(kit, 500_000, 0.1), 2_811_707);
        Approx(EffectiveDamage(kit, 35_000, 20_000_000.0 / 3_000_000_000.0), 1_500_500);
    }

    [Fact]
    public void MobHp_at_anchor_round_is_anchor()
    {
        Assert.Equal(12_000_000L, MobHp(kit, 20, Normal, 1));
        Assert.Equal(60_000_000L, MobHp(kit, 20, Elite, 1));
    }

    [Fact]
    public void MobHp_round_1_is_about_2M()
    {
        Approx(MobHp(kit, 1, Normal, 1), 1_963_400);
    }

    [Fact]
    public void Boss_hp_scales_with_alive_players()
    {
        Assert.Equal(480_000_000L, MobHp(kit, 20, Boss, 1));
        Assert.Equal(960_000_000L, MobHp(kit, 20, Boss, 5));
    }

    [Fact]
    public void Weapon_damage_scales_down_for_low_xp()
    {
        Approx(MobWeaponDamage(kit, 20, Normal, 0.1), 11_559);
        Approx(MobWeaponDamage(kit, 20, Normal, 1.0), 65_000);
    }

    [Fact]
    public void Effective_damage_never_overflows()
    {
        Assert.Equal(long.MaxValue, EffectiveDamage(kit, long.MaxValue, 4_000_000.0 / 3_000_000_000.0));
    }

    [Fact]
    public void Split_bounty_sums_exactly_and_is_proportional()
    {
        var even = SplitBounty(kit, 10, new Dictionary<int, long> { [1] = 1, [2] = 1, [3] = 1 });
        Assert.Equal(10, Sum(even));
        foreach (var share in even.Values)
            Assert.True(share is 3 or 4, $"share {share}");

        var odd = SplitBounty(kit, 7, new Dictionary<int, long> { [1] = 3, [2] = 2, [3] = 1 });
        Assert.Equal(7, Sum(odd));
        Assert.Equal(4, odd[1]);
        Assert.Equal(2, odd[2]);
        Assert.Equal(1, odd[3]);
    }

    [Fact]
    public void Split_bounty_with_no_damage_is_empty()
    {
        Assert.Empty(SplitBounty(kit, 10, new Dictionary<int, long>()));
        Assert.Empty(SplitBounty(kit, 10, new Dictionary<int, long> { [1] = 0, [2] = 0 }));
        Assert.Empty(SplitBounty(kit, 0, new Dictionary<int, long> { [1] = 5 }));
    }

    [Fact]
    public void Band_repeats_last_after_the_end()
    {
        kit.SetConfig(json =>
        {
            var bands = ((System.Text.Json.Nodes.JsonArray)json["bands"]!);
            bands.Add(new System.Text.Json.Nodes.JsonObject
            {
                ["normals"] = new System.Text.Json.Nodes.JsonArray(41004),
                ["boss"] = 41102,
            });
        });
        Assert.Equal(ArenaTestKit.BandBossId, BandBoss(kit, 1));
        Assert.Equal(ArenaTestKit.BandBossId, BandBoss(kit, 10));
        Assert.Equal(41102, BandBoss(kit, 11));
        Assert.Equal(41102, BandBoss(kit, 20));
        Assert.Equal(41102, BandBoss(kit, 25));
    }

    [Fact]
    public void Elite_chance_caps()
    {
        Assert.Equal(0.0, EliteChance(kit, 2), 10);
        Assert.Equal(0.05, EliteChance(kit, 3), 10);
        Assert.Equal(0.07, EliteChance(kit, 4), 10);
        Assert.Equal(0.30, EliteChance(kit, 100), 10);
    }

    [Fact]
    public void NextStart_is_strictly_after_now_and_aligned_to_anchor()
    {
        var day = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal(day.AddHours(3), NextStartUtc(kit, day));
        Assert.Equal(day.AddHours(3), NextStartUtc(kit, day.AddHours(2).AddMinutes(59)));
        Assert.Equal(day.AddHours(6), NextStartUtc(kit, day.AddHours(3)));
        Assert.Equal(day.AddHours(6), NextStartUtc(kit, day.AddHours(3).AddMinutes(1)));
    }

    [Fact]
    public void Milestone_takes_highest_reached()
    {
        Assert.Equal(0, MilestoneBonus(kit, 4));
        Assert.Equal(20, MilestoneBonus(kit, 5));
        Assert.Equal(60, MilestoneBonus(kit, 12));
        Assert.Equal(200, MilestoneBonus(kit, 20));
        Assert.Equal(200, MilestoneBonus(kit, 30));
    }

    [Fact]
    public void WaveCount_and_boss_rounds()
    {
        Assert.False(IsBossRound(kit, 4));
        Assert.True(IsBossRound(kit, 5));
        Assert.Equal(10, WaveCount(kit, 21, 3));
        Assert.Equal(5, WaveCount(kit, 20, 3));
    }

    [Fact]
    public void Payout_and_bounty()
    {
        Assert.Equal(6, RoundPayout(kit, 1));
        Assert.Equal(25, RoundPayout(kit, 20));
        Assert.Equal(1, Bounty(kit, Normal));
        Assert.Equal(5, Bounty(kit, Elite));
        Assert.Equal(25, Bounty(kit, Boss));
    }

    private static long Sum(Dictionary<int, long> d)
    {
        long sum = 0;
        foreach (var v in d.Values) sum += v;
        return sum;
    }
}
