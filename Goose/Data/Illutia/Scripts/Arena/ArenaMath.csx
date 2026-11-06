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
        double mult = tier switch
        {
            MobTier.Elite => c.EliteHpMult,
            MobTier.Boss => c.BossHpMult * (1 + c.BossHpPerPlayer * (alive - 1)),
            _ => 1,
        };
        double hp = c.AnchorMobHp * Math.Pow(c.HpGrowth, round - c.AnchorRound) * mult;
        if (hp >= long.MaxValue) return long.MaxValue;
        if (hp <= 0) return 0;
        return (long)hp;
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

    public static Dictionary<int, long> SplitBounty(long bounty, IReadOnlyDictionary<int, long> damageByPlayer)
    {
        var result = new Dictionary<int, long>();
        if (bounty <= 0) return result;
        long total = 0;
        foreach (var dmg in damageByPlayer.Values) total += dmg;
        if (total <= 0) return result;
        var shares = new List<(int Id, long Floor, double Frac)>();
        long given = 0;
        foreach (var (id, dmg) in damageByPlayer)
        {
            double exact = (double)bounty * dmg / total;
            long floor = (long)Math.Floor(exact);
            shares.Add((id, floor, exact - floor));
            given += floor;
        }
        shares.Sort((x, y) => x.Frac != y.Frac ? y.Frac.CompareTo(x.Frac) : x.Id.CompareTo(y.Id));
        for (int i = 0; i < shares.Count; i++)
        {
            long share = shares[i].Floor + (i < bounty - given ? 1 : 0);
            if (share > 0) result[shares[i].Id] = share;
        }
        return result;
    }

    public static DateTime NextStartUtc(DateTime nowUtc, ArenaConfig c)
    {
        DateTime anchor = nowUtc.Date + TimeSpan.Parse(c.AnchorUtc);
        TimeSpan interval = TimeSpan.FromHours(c.IntervalHours);
        while (anchor <= nowUtc) anchor += interval;
        return anchor;
    }
}
