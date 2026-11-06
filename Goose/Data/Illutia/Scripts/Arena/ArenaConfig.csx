using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Goose;

public sealed class ArenaBand
{
    public int[] Normals { get; set; } = [];
    public int Boss { get; set; }
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
    public double BossDmgMult { get; set; }
    public long MinXpSold { get; set; }
    public long TopXpSold { get; set; }
    public double A { get; set; }
    public double B { get; set; }
    public int AnchorRound { get; set; }
    public long AnchorMobHp { get; set; }
    public long AnchorMobWeaponDamage { get; set; }
    public double HpGrowth { get; set; }
    public double DmgGrowth { get; set; }
    public string CurrencyId { get; set; } = "";
    public int RoundBase { get; set; }
    public int RoundStep { get; set; }
    public long MobBounty { get; set; }
    public long EliteBountyMult { get; set; }
    public long BossBountyMult { get; set; }
    public Dictionary<int, long> Milestones { get; set; } = new();
    public bool Valid { get; set; } = true;

    public bool Enabled(GameWorld world)
    {
        if (!Valid) return false;
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
        if (c.AnchorMobHp <= 0 || c.AnchorMobWeaponDamage <= 0)
            throw new JsonException("Arena anchorMobHp and anchorMobWeaponDamage must be > 0");
        if (c.MinXpSold <= 0 || c.TopXpSold < c.MinXpSold)
            throw new JsonException("Arena minXpSold must be > 0 and topXpSold >= minXpSold");
        if (c.BandSize < 1)
            throw new JsonException("Arena bandSize must be >= 1");
        if (c.BossEvery < 1)
            throw new JsonException("Arena bossEvery must be >= 1");
        if (c.IntervalHours <= 0)
            throw new JsonException("Arena intervalHours must be > 0");
        if (!TimeSpan.TryParseExact(c.AnchorUtc, @"hh\:mm", CultureInfo.InvariantCulture, out _))
            throw new JsonException("Arena anchorUtc must be an HH:mm time");
        foreach (var band in c.Bands ?? new List<ArenaBand>())
            if (band.Normals is null || band.Normals.Length == 0)
                throw new JsonException("Arena bands entries need at least one normal template");
        if (c.RoundSeconds <= c.TimeoutWarningSeconds)
            throw new JsonException("Arena roundSeconds must be > timeoutWarningSeconds");
    }
}
