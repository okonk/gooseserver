#load "ArenaConfig.csx"
using System;
using System.Collections.Generic;

public enum MobTier { Normal, Elite, Boss }

public static class ArenaMath
{
    public static double PowerFraction(long xpSold, ArenaConfig c)
        => Math.Clamp((double)xpSold, (double)c.MinXpSold, (double)c.TopXpSold) / c.TopXpSold;

    public static long EffectiveDamage(long raw, double s, ArenaConfig c)
    {
        if (raw <= 0) return raw;
        double m = Math.Pow(s, -c.A);
        if (raw > long.MaxValue / m) return long.MaxValue;
        return (long)(raw * m);
    }

    public static long MobHp(int round, MobTier tier, int alive, ArenaConfig c)
    {
        double hp = tier switch
        {
            MobTier.Elite => c.AnchorMobHp * HpCurve(round, c) * c.EliteHpMult,
            MobTier.Boss => c.AnchorMobHp * BossHpCurve(round, c) * c.BossHpMult
                * (1 + c.BossHpPerPlayer * Math.Pow(Math.Max(0, alive - 1), c.BossHpPlayerCurve)),
            _ => c.AnchorMobHp * HpCurve(round, c),
        };
        if (hp >= long.MaxValue) return long.MaxValue;
        if (hp <= 0) return 0;
        return (long)hp;
    }

    public static double HpCurve(int round, ArenaConfig c)
        => Curve(round, c.AnchorRound, c.HpGrowth, c.HpSoftRound, c.HpGrowthLate);

    public static double BossHpCurve(int round, ArenaConfig c)
        => c.BossHpGrowth > 0
            ? Curve(round, c.AnchorRound, c.BossHpGrowth, c.BossHpSoftRound, c.BossHpGrowthLate)
            : HpCurve(round, c);

    private static double Curve(int round, int anchor, double growth, int soft, double late)
    {
        if (soft <= 0) return Math.Pow(growth, round - anchor);
        int early = Math.Min(round, soft) - Math.Min(anchor, soft);
        int after = Math.Max(round, soft) - Math.Max(anchor, soft);
        return Math.Pow(growth, early) * Math.Pow(late, after);
    }

    public static long MobXp(int round, MobTier tier, ArenaConfig c)
    {
        double mult = tier switch
        {
            MobTier.Elite => c.EliteXpMult,
            MobTier.Boss => c.BossXpMult,
            _ => 1,
        };
        double xp = c.XpAnchor * Math.Pow(c.XpGrowth, round - c.AnchorRound) * mult;
        if (xp >= long.MaxValue) return long.MaxValue;
        return (long)Math.Max(0, xp);
    }

    public static double MobWeaponDamage(int round, MobTier tier, double s, ArenaConfig c)
    {
        double mult = tier switch
        {
            MobTier.Elite => c.EliteDmgMult,
            MobTier.Boss => c.BossDmgMult,
            _ => 1,
        };
        return c.AnchorMobWeaponDamage * Math.Pow(c.DmgGrowth, round - c.AnchorRound) * mult * Math.Pow(s, c.B);
    }

    public static int WaveCount(int round, int alive, ArenaConfig c)
    {
        int count = c.BaseCount + c.PerPlayer * alive;
        return IsBossRound(round, c) ? (int)Math.Ceiling(count * c.BossRoundAddFraction) : count;
    }

    public static bool IsBossRound(int round, ArenaConfig c)
        => round % c.BossEvery == 0;

    public static double EliteChance(int round, ArenaConfig c)
        => round < c.EliteFromRound
            ? 0
            : Math.Min(c.EliteChance + c.EliteChanceGrowth * (round - c.EliteFromRound), c.EliteChanceMax);

    public static ArenaBand BandFor(int round, ArenaConfig c)
        => c.Bands[Math.Min((round - 1) / c.BandSize, c.Bands.Count - 1)];

    public static long RoundPayout(int round, ArenaConfig c)
        => c.RoundBase + c.RoundStep * (long)round;

    public static long Bounty(MobTier tier, ArenaConfig c)
        => c.MobBounty * (tier switch
        {
            MobTier.Elite => c.EliteBountyMult,
            MobTier.Boss => c.BossBountyMult,
            _ => 1,
        });

    public static long MilestoneBonus(int roundsCleared, ArenaConfig c)
    {
        int bestRound = -1;
        long best = 0;
        foreach (var m in c.Milestones)
            if (m.Key <= roundsCleared && m.Key > bestRound)
            {
                bestRound = m.Key;
                best = m.Value;
            }
        return best;
    }

    public static List<int> KillCredit(IReadOnlyDictionary<int, long> damageByPlayer, double minShare)
    {
        var result = new List<int>();
        double total = 0;
        foreach (var dmg in damageByPlayer.Values) total += Math.Max(0, dmg);
        if (total <= 0) return result;
        foreach (var (id, dmg) in damageByPlayer)
            if (dmg > 0 && dmg / total >= minShare)
                result.Add(id);
        result.Sort();
        return result;
    }

    public static DateTime NextStartUtc(DateTime nowUtc, ArenaConfig c)
    {
        DateTime anchor = nowUtc.Date + TimeSpan.Parse(c.AnchorUtc);
        TimeSpan interval = TimeSpan.FromHours(c.IntervalHours);
        if (interval <= TimeSpan.Zero) return anchor;
        while (anchor <= nowUtc) anchor += interval;
        return anchor;
    }
}
