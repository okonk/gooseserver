using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Goose;

public sealed class ArenaBand
{
    public int[] Normals { get; set; } = [];
    public int Boss { get; set; }
}

public sealed class ArenaMobSpell
{
    public double Damage { get; set; }
    public double Heal { get; set; }
    public double HealBelow { get; set; } = 0.8;
    public double Cooldown { get; set; } = 8;
    public int Range { get; set; }
}

public sealed class ArenaMayor
{
    public int TemplateId { get; set; }
    public double RoundChance { get; set; }
    public double ClearChance { get; set; }
    public Dictionary<string, string[]> Lines { get; set; } = new();
}

public sealed class ArenaCrowdShouts
{
    public double[] EverySeconds { get; set; } = [];
    public double BooChance { get; set; }
    public int NearestCrowd { get; set; } = 8;
    public string[] Cheers { get; set; } = [];
    public string[] Boos { get; set; } = [];
}

public sealed class ArenaBossMechanics
{
    public int Animation { get; set; } = 900001;
    public int AnimationFile { get; set; } = 30022;
    public double[] GapSeconds { get; set; } = [8, 12];
    public ArenaSlam? Slam { get; set; }
    public ArenaPull? Pull { get; set; }
    public ArenaCharge? Charge { get; set; }
    public ArenaMark? Mark { get; set; }
    public ArenaSafeZone? SafeZone { get; set; }
    public ArenaSwap? Swap { get; set; }
    public ArenaSummon? Summon { get; set; }
}

public sealed class ArenaSlam
{
    public int FromRound { get; set; }
    public double Damage { get; set; } = 3;
    public double Radius { get; set; } = 5;
    public double Windup { get; set; } = 2;
}

public sealed class ArenaPull
{
    public int FromRound { get; set; }
    public int Range { get; set; } = 8;
    public double Windup { get; set; } = 2.5;
}

public sealed class ArenaCharge
{
    public int FromRound { get; set; }
    public double Damage { get; set; } = 2.5;
    public int Length { get; set; } = 8;
    public double Windup { get; set; } = 1.5;
}

public sealed class ArenaMark
{
    public int FromRound { get; set; }
    public double Damage { get; set; } = 4;
    public int Size { get; set; } = 3;
    public double Windup { get; set; } = 3;
}

public sealed class ArenaSafeZone
{
    public int FromRound { get; set; }
    public double Damage { get; set; } = 6;
    public int BlockWidth { get; set; } = 15;
    public int BlockHeight { get; set; } = 8;
    public int MaxDistance { get; set; } = 20;
    public double Windup { get; set; } = 6;
}

public sealed class ArenaSwap
{
    public int FromRound { get; set; }
    public double[] EverySeconds { get; set; } = [45, 60];
    public double Windup { get; set; } = 1.5;
    public double LockSeconds { get; set; } = 10;
    public int[] Emote { get; set; } = [1081, 8];
}

public sealed class ArenaSummon
{
    public int FromRound { get; set; }
    public double[] AtHp { get; set; } = [0.66, 0.33];
    public int Base { get; set; } = 2;
    public int PerPlayers { get; set; } = 3;
}

public sealed class ArenaConfig
{
    public int LobbyMap { get; set; }
    public int[] LobbySpawn { get; set; } = [];
    public int ArenaMap { get; set; }
    public int[] ArenaRect { get; set; } = [];
    public double IntervalHours { get; set; }
    public string AnchorUtc { get; set; } = "00:00";
    public int[] AnnounceMinutes { get; set; } = [];
    public int CountdownSeconds { get; set; }
    public int BreakSeconds { get; set; }
    public int RoundSeconds { get; set; }
    public int TimeoutWarningSeconds { get; set; }
    public int StragglerCount { get; set; }
    public int MinPlayerDistance { get; set; }
    public int SpawnRadius { get; set; }
    public int BaseCount { get; set; }
    public int PerPlayer { get; set; }
    public int BandSize { get; set; }
    public List<ArenaBand> Bands { get; set; } = new();
    public int EliteFromRound { get; set; }
    public double EliteChance { get; set; }
    public double EliteChanceGrowth { get; set; }
    public double EliteChanceMax { get; set; }
    public double EliteHpMult { get; set; }
    public double EliteDmgMult { get; set; }
    public string EliteTitle { get; set; } = "";
    public int[] EliteTint { get; set; } = [];
    public int BossEvery { get; set; }
    public double BossRoundAddFraction { get; set; }
    public double BossHpMult { get; set; }
    public double BossHpPerPlayer { get; set; }
    public double BossHpPlayerCurve { get; set; } = 1;
    public double BossDmgMult { get; set; }
    public double BossHpGrowth { get; set; }
    public int BossHpSoftRound { get; set; }
    public double BossHpGrowthLate { get; set; }
    public long MinXpSold { get; set; }
    public long TopXpSold { get; set; }
    public double A { get; set; }
    public double B { get; set; }
    public int AnchorRound { get; set; }
    public long AnchorMobHp { get; set; }
    public long AnchorMobWeaponDamage { get; set; }
    public double HpGrowth { get; set; }
    public int HpSoftRound { get; set; }
    public double HpGrowthLate { get; set; }
    public double DmgGrowth { get; set; }
    public int DmgSoftRound { get; set; }
    public double DmgGrowthLate { get; set; }
    public long XpAnchor { get; set; }
    public double XpGrowth { get; set; } = 1;
    public int XpSoftRound { get; set; }
    public double XpGrowthLate { get; set; }
    public double EliteXpMult { get; set; } = 1;
    public double BossXpMult { get; set; } = 1;
    public string CurrencyId { get; set; } = "";
    public int RoundBase { get; set; }
    public int RoundStep { get; set; }
    public long MobBounty { get; set; }
    public long EliteBountyMult { get; set; }
    public long BossBountyMult { get; set; }
    public double KillCreditMinShare { get; set; } = 0.05;
    public int[] PriestClassIds { get; set; } = [];
    public double PriestKillShare { get; set; }
    public Dictionary<int, long> Milestones { get; set; } = new();
    public Dictionary<string, ArenaMobSpell> MobSpells { get; set; } = new();
    public ArenaBossMechanics BossMechanics { get; set; } = new();
    public ArenaMayor Mayor { get; set; } = new();
    public ArenaCrowdShouts Crowd { get; set; } = new();
    public bool Valid { get; set; } = true;

    public const string OpenKey = "arena.open";

    public static bool IsClosed(GameWorld world) => !world.WorldState.Get<bool>(OpenKey);

    public bool Enabled(GameWorld world)
    {
        if (!Valid) return false;
        if (IsClosed(world)) return false;
        if (LobbyMap == 0 || ArenaMap == 0) return false;
        if (LobbySpawn is null || LobbySpawn.Length != 2) return false;
        if (ArenaRect is null || ArenaRect.Length != 4) return false;
        if (Bands is null || Bands.Count == 0) return false;
        return world.MapHandler.Maps.ContainsKey(LobbyMap) && world.MapHandler.Maps.ContainsKey(ArenaMap);
    }

    public static ArenaConfig Disabled { get; } = new() { Valid = false };

    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    private static ArenaConfig? cached;
    private static string cachedKey = "";
    private static string lastErrorKey = "";
    private static string failedKey = "";

    public static ArenaConfig Get(GameWorld world)
    {
        string path = world.Settings.DataPathAbsolute + "/Scripts/Arena/arena.json";
        string key;
        if (File.Exists(path))
        {
            key = "mtime:" + File.GetLastWriteTimeUtc(path).ToString("O");
        }
        else
        {
            key = "missing:" + path;
        }
        if (failedKey == key) return Disabled;
        if (cached is not null && cachedKey == key) return cached;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            var config = JsonSerializer.Deserialize<ArenaConfig>(File.ReadAllText(path), options) ?? new ArenaConfig();
            Validate(config);
            config.Valid = true;
            cached = config;
            cachedKey = key;
            failedKey = "";
            return config;
        }
        catch (Exception e)
        {
            if (key != lastErrorKey)
            {
                lastErrorKey = key;
                log.Error(e, "Arena config {0} unusable; arena disabled", path);
            }
            failedKey = key;
            cached = null;
            return Disabled;
        }
    }

    private static void Validate(ArenaConfig c)
    {
        if (c.A is < 0 or > 2 || c.B is < 0 or > 2)
            throw new JsonException("Arena a and b must be in [0, 2]");
        if (c.HpGrowth <= 0 || c.DmgGrowth <= 0)
            throw new JsonException("Arena hpGrowth and dmgGrowth must be > 0");
        if (c.DmgSoftRound > 0 && c.DmgGrowthLate <= 0)
            throw new JsonException("Arena dmgGrowthLate must be > 0 when dmgSoftRound is set");
        if (c.HpSoftRound > 0 && c.HpGrowthLate <= 0)
            throw new JsonException("Arena hpGrowthLate must be > 0 when hpSoftRound is set");
        if (c.BossHpGrowth < 0)
            throw new JsonException("Arena bossHpGrowth must be >= 0 (0 follows hpGrowth)");
        if (c.BossHpGrowth > 0 && c.BossHpSoftRound > 0 && c.BossHpGrowthLate <= 0)
            throw new JsonException("Arena bossHpGrowthLate must be > 0 when bossHpSoftRound is set");
        if (c.BossHpPlayerCurve < 0)
            throw new JsonException("Arena bossHpPlayerCurve must be >= 0");
        if (c.XpAnchor < 0 || c.XpGrowth <= 0 || c.EliteXpMult < 0 || c.BossXpMult < 0)
            throw new JsonException("Arena xpAnchor, eliteXpMult and bossXpMult must be >= 0 and xpGrowth > 0");
        if (c.XpSoftRound > 0 && c.XpGrowthLate <= 0)
            throw new JsonException("Arena xpGrowthLate must be > 0 when xpSoftRound is set");
        if (c.AnchorMobHp <= 0 || c.AnchorMobWeaponDamage <= 0)
            throw new JsonException("Arena anchorMobHp and anchorMobWeaponDamage must be > 0");
        if (c.MinXpSold <= 0 || c.TopXpSold < c.MinXpSold)
            throw new JsonException("Arena minXpSold must be > 0 and topXpSold >= minXpSold");
        if (c.BandSize < 1)
            throw new JsonException("Arena bandSize must be >= 1");
        if (c.BossEvery < 1)
            throw new JsonException("Arena bossEvery must be >= 1");
        if (c.IntervalHours <= 0 || 24 % c.IntervalHours != 0)
            throw new JsonException("Arena intervalHours must be a positive divisor of 24");
        if (!TimeSpan.TryParseExact(c.AnchorUtc, @"hh\:mm", CultureInfo.InvariantCulture, out _))
            throw new JsonException("Arena anchorUtc must be an HH:mm time");
        if (c.Bands is null || c.Bands.Count == 0)
            throw new JsonException("Arena bands must contain at least one band");
        foreach (var band in c.Bands ?? new List<ArenaBand>())
            if (band.Normals is null || band.Normals.Length == 0)
                throw new JsonException("Arena bands entries need at least one normal template");
        foreach (var (name, spell) in c.MobSpells ?? new Dictionary<string, ArenaMobSpell>())
            if (spell.Damage < 0 || spell.Heal is < 0 or > 1 || spell.Cooldown < 0 || spell.Range < 0)
                throw new JsonException("Arena mobSpells entry " + name + " has a negative or out-of-range value");
        var m = c.BossMechanics ?? throw new JsonException("Arena bossMechanics must be an object");
        if (m.GapSeconds is not { Length: 2 } || m.GapSeconds[0] <= 0 || m.GapSeconds[1] < m.GapSeconds[0])
            throw new JsonException("Arena bossMechanics.gapSeconds must be [min, max] with 0 < min <= max");
        if (m.Swap is not null && (m.Swap.EverySeconds is not { Length: 2 } || m.Swap.EverySeconds[0] <= 0
            || m.Swap.EverySeconds[1] < m.Swap.EverySeconds[0] || m.Swap.Emote is not { Length: 2 }))
            throw new JsonException("Arena bossMechanics.swap needs everySeconds [min, max] and emote [animation, sheet]");
        if (m.Summon is not null && (m.Summon.AtHp is null || m.Summon.AtHp.Any(f => f is <= 0 or >= 1) || m.Summon.PerPlayers < 1))
            throw new JsonException("Arena bossMechanics.summon.atHp values must be in (0, 1) and perPlayers >= 1");
        foreach (var windup in new double?[] { m.Slam?.Windup, m.Pull?.Windup, m.Charge?.Windup, m.Mark?.Windup, m.SafeZone?.Windup, m.Swap?.Windup })
            if (windup is <= 0)
                throw new JsonException("Arena bossMechanics windups must be > 0");
        if (c.SpawnRadius < 0)
            throw new JsonException("Arena spawnRadius must be >= 0");
        if (c.KillCreditMinShare is < 0 or > 1)
            throw new JsonException("Arena killCreditMinShare must be in [0, 1]");
        if (c.PriestKillShare is < 0 or > 1)
            throw new JsonException("Arena priestKillShare must be in [0, 1]");
        if (c.Mayor is null || c.Mayor.Lines is null || c.Mayor.RoundChance is < 0 or > 1 || c.Mayor.ClearChance is < 0 or > 1)
            throw new JsonException("Arena mayor needs lines, and roundChance and clearChance in [0, 1]");
        if (c.Crowd is null || c.Crowd.EverySeconds is null || c.Crowd.EverySeconds is { Length: not (0 or 2) }
            || c.Crowd.EverySeconds.Any(s => s <= 0) || c.Crowd.BooChance is < 0 or > 1 || c.Crowd.NearestCrowd < 1)
            throw new JsonException("Arena crowd everySeconds must be empty or [min, max] > 0, booChance in [0, 1], nearestCrowd >= 1");
        if (c.RoundSeconds <= c.TimeoutWarningSeconds)
            throw new JsonException("Arena roundSeconds must be > timeoutWarningSeconds");
    }
}
