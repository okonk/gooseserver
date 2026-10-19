using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Goose;

public sealed class HalloweenMaps
{
    public int House { get; set; }
    public int Basement { get; set; }
    public int Grove { get; set; }
    public int[] GroveExit { get; set; } = [];
    public int[] FoyerSpawn { get; set; } = [];
    public int[] BasementArrival { get; set; } = [];
    public int[] Entrance { get; set; } = [];
}

public sealed class HalloweenItems
{
    public int Candy { get; set; }
    public int PartyCostume { get; set; }
    public int[] Masks { get; set; } = [];
    public int RitualCandle { get; set; }
}

public sealed class HalloweenNpcs
{
    public int Stalker { get; set; }
    public int Groundskeeper { get; set; }
    public int Weeper { get; set; }
    public int WailingGhost { get; set; }
    public int FlickeringGhost { get; set; }
    public int PinkPhantom { get; set; }
    public int BloodyGhost { get; set; }
    public int RestlessBones { get; set; }
}

public sealed class HalloweenCandy
{
    public int LevelOnly { get; set; }
    public double BaseChance { get; set; }
    public int[] BaseAmount { get; set; } = [];
    public Dictionary<int, int[]> ByTemplate { get; set; } = new();
    public int StalkerSurvived { get; set; }
}

public sealed class HalloweenRoom
{
    public string Name { get; set; } = "";
    public int[] Rect { get; set; } = [];
    public double Weight { get; set; }
    public string Table { get; set; } = "house";
}

public sealed class HalloweenAmbushTable
{
    public int Fake { get; set; }
    public int Ghosts { get; set; }
    public int Bones { get; set; }
    public int Bloody { get; set; }
    public int Phantom { get; set; }
    public int Stalker { get; set; }
}

public sealed class HalloweenPack
{
    public int Min { get; set; }
    public int Max { get; set; }
}

// Packs are written as a single number ("wailing": 2) or a [min, max] range ("bones": [3, 4]).
public sealed class HalloweenPackConverter : JsonConverter<HalloweenPack>
{
    public override HalloweenPack? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            int n = reader.GetInt32();
            return new HalloweenPack { Min = n, Max = n };
        }
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Halloween pack must be a number or a [min, max] array");
        int min = 0, max = 0, count = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (reader.TokenType != JsonTokenType.Number)
                throw new JsonException("Halloween pack entry must be a number");
            if (count == 0) min = reader.GetInt32();
            else if (count == 1) max = reader.GetInt32();
            else throw new JsonException("Halloween pack must have exactly two entries");
            count++;
        }
        if (count != 2 || min > max)
            throw new JsonException("Halloween pack must be [min, max] with min <= max");
        return new HalloweenPack { Min = min, Max = max };
    }

    public override void Write(Utf8JsonWriter writer, HalloweenPack value, JsonSerializerOptions options)
    {
        if (value.Min == value.Max)
        {
            writer.WriteNumberValue(value.Min);
        }
        else
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Min);
            writer.WriteNumberValue(value.Max);
            writer.WriteEndArray();
        }
    }
}

public sealed class HalloweenAmbushConfig
{
    public double BaseChance { get; set; }
    public double BasementWeight { get; set; }
    public double DefaultWeight { get; set; }
    public int CooldownSeconds { get; set; }
    public int BasementCooldownSeconds { get; set; }
    public int FakeCooldownSeconds { get; set; }
    public int? BasementFakeCooldownSeconds { get; set; }
    public int GroupCooldownRadius { get; set; }
    public int PackRadius { get; set; }
    public int PackMax { get; set; }
    public int? PackPerPlayer { get; set; }
    public int? BasementPackMax { get; set; }
    public int DespawnSeconds { get; set; }
    public int MaxLivePerMap { get; set; }
    public int? BasementMaxLive { get; set; }
    public int MaxPhantomsHouse { get; set; }
    public int MaxPhantomsBasement { get; set; }
    public Dictionary<string, HalloweenAmbushTable> Tables { get; set; } = new();
    public Dictionary<string, HalloweenPack> Packs { get; set; } = new();
    public Dictionary<string, int[]> Candy { get; set; } = new();
    public Dictionary<string, string[]> FakeScares { get; set; } = new();
}

public sealed class HalloweenStalker
{
    public int GraceSeconds { get; set; }
    public int MaxLive { get; set; }
    public int SpawnBehind { get; set; }
    public int ChainsawMinMinutes { get; set; }
    public int ChainsawMaxMinutes { get; set; }
    public int ChainsawMinPlayers { get; set; }
    public int ChainsawDelaySeconds { get; set; }
    public int CostumeGraceSeconds { get; set; }
}

public sealed class HalloweenStalkerChase
{
    public int ChaseSeconds { get; set; }
    public int StartDelaySeconds { get; set; }
    public double SpeedOffset { get; set; }
    public double MinDelaySeconds { get; set; }
    public int WarnRadius { get; set; }
    public int WarnSeconds { get; set; }
}

public sealed class HalloweenPentagram
{
    public int[][] Points { get; set; } = [];
    public int[] Centre { get; set; } = [];
}

public sealed class HalloweenTown
{
    public int Map { get; set; }
    public int[] Rect { get; set; } = [];
}

public sealed class HalloweenInvasion
{
    public List<HalloweenTown> Towns { get; set; } = [];
    public int[] Templates { get; set; } = [];
    public int Count { get; set; }
    public int EveryMinutes { get; set; }
    public int LifetimeMinutes { get; set; }
}

public sealed class HalloweenEffects
{
    public int WeeperCap { get; set; }
    public int GroundskeeperCurve { get; set; }
    public int GhostSnareOnHit { get; set; }
}

public sealed class HalloweenWeeper
{
    public int CapPerHit { get; set; }
    public double BleedPercentPerStack { get; set; }
    public int BleedMaxStacks { get; set; }
    public int BleedSeconds { get; set; }
    public int BleedTickSeconds { get; set; }
    public int[] Start { get; set; } = [];
    public double HitPercent { get; set; }
    public int StragglerRadius { get; set; }
    public double StragglerMultiplier { get; set; }
    public int LungeSeconds { get; set; }
    public int LungeRadius { get; set; }
    public int LeashRadius { get; set; }
    public int LeashSeconds { get; set; }
    public int LeashMaxDistance { get; set; }
    public int ResetSeconds { get; set; }
    public int FootstepRadius { get; set; }
    public int FootstepSeconds { get; set; }
    public Dictionary<string, int[][]> Loops { get; set; } = new();
}

public sealed class HalloweenCurve
{
    public int Floor { get; set; }
    public int Band { get; set; }
    public double Exponent { get; set; }
}

public sealed class HalloweenGroundskeeper
{
    public double BloodyDamageMultiplier { get; set; }
    public HalloweenCurve Curve { get; set; } = new();
    public long HpPerPlayer { get; set; }
    public double[] Phases { get; set; } = [];
    public double HitPercent { get; set; }
    public double BigHitPercent { get; set; }
    public int BigHitEvery { get; set; }
    public double BigHitWarnSeconds { get; set; }
    public double EnrageAttackSpeed { get; set; }
    public int LightsOutSeconds { get; set; }
    public int LightsOutAlpha { get; set; }
    public int FinalLightsOutSeconds { get; set; }
    public int[] FinalLightsOutEverySeconds { get; set; } = [];
    public Dictionary<string, int[][]> Adds { get; set; } = new();
    public int EmptyDespawnSeconds { get; set; }
    public double JumpSeconds { get; set; }
}

public sealed class HalloweenGoal
{
    public long[] Tiers { get; set; } = [];
}

public sealed class HalloweenConfig
{
    public static HalloweenConfig Disabled { get; } = new();

    public HalloweenMaps Maps { get; set; } = new();
    public double XpBonus { get; set; }
    public HalloweenItems Items { get; set; } = new();
    public HalloweenNpcs Npcs { get; set; } = new();
    public HalloweenCandy Candy { get; set; } = new();
    public List<HalloweenRoom> Rooms { get; set; } = new();
    public HalloweenAmbushConfig Ambush { get; set; } = new();
    public HalloweenStalker Stalker { get; set; } = new();
    public HalloweenStalkerChase StalkerChase { get; set; } = new();
    public HalloweenPentagram Pentagram { get; set; } = new();
    public HalloweenInvasion Invasion { get; set; } = new();
    public HalloweenEffects Effects { get; set; } = new();
    public HalloweenWeeper Weeper { get; set; } = new();
    public HalloweenGroundskeeper Groundskeeper { get; set; } = new();
    public HalloweenGoal Goal { get; set; } = new();

    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    private static HalloweenConfig? cached;
    private static string cachedKey = "";
    private static string lastErrorKey = "";
    private static string failedKey = "";

    public static HalloweenConfig Get(GameWorld world)
    {
        string path = world.Settings.DataPathAbsolute + "/Scripts/Global/Halloween/halloween.json";
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
                Converters = { new HalloweenPackConverter() },
            };
            var config = JsonSerializer.Deserialize<HalloweenConfig>(File.ReadAllText(path), options) ?? new HalloweenConfig();
            Require(config.Maps, "maps");
            Require(config.Items, "items");
            Require(config.Npcs, "npcs");
            Require(config.Candy, "candy");
            Require(config.Rooms, "rooms");
            Require(config.Ambush, "ambush");
            Require(config.Stalker, "stalker");
            Require(config.StalkerChase, "stalkerChase");
            Require(config.Pentagram, "pentagram");
            Require(config.Invasion, "invasion");
            Require(config.Effects, "effects");
            Require(config.Weeper, "weeper");
            Require(config.Groundskeeper, "groundskeeper");
            Require(config.Groundskeeper.Curve, "groundskeeper.curve");
            Require(config.Ambush.Tables, "ambush.tables");
            Require(config.Ambush.Packs, "ambush.packs");
            Require(config.Ambush.Candy, "ambush.candy");
            Require(config.Ambush.FakeScares, "ambush.fakeScares");
            Require(config.Items.Masks, "items.masks");
            Require(config.Pentagram.Points, "pentagram.points");
            Require(config.Invasion.Towns, "invasion.towns");
            Require(config.Invasion.Templates, "invasion.templates");
            Require(config.Candy.ByTemplate, "candy.byTemplate");
            foreach (var (table, weights) in config.Ambush.Tables)
                Require(weights, "ambush.tables." + table);
            foreach (var (table, scares) in config.Ambush.FakeScares)
                if (scares is null)
                    throw new JsonException("Halloween ambush.fakeScares entry " + table + " must not be null");
            foreach (var (name, point) in new Dictionary<string, int[]>
            {
                ["groveExit"] = config.Maps.GroveExit,
                ["foyerSpawn"] = config.Maps.FoyerSpawn,
                ["basementArrival"] = config.Maps.BasementArrival,
                ["entrance"] = config.Maps.Entrance,
            })
                if (point is null || point.Length != 2)
                    throw new JsonException("Halloween maps." + name + " must be a [x, y] array");
            foreach (var room in config.Rooms)
                if (room.Rect is null || room.Rect.Length != 4)
                    throw new JsonException("Halloween room " + room.Name + " rect must have exactly 4 elements");
            foreach (var (packKey, pack) in config.Ambush.Packs)
                if (pack is null)
                    throw new JsonException("Halloween pack " + packKey + " must not be null");
            if (config.Candy.BaseAmount is null || config.Candy.BaseAmount.Length != 2 ||
                config.Candy.BaseAmount[0] > config.Candy.BaseAmount[1])
                throw new JsonException("Halloween candy baseAmount must be [min, max] with min <= max");
            foreach (var (templateId, range) in config.Candy.ByTemplate)
                if (range is null || range.Length != 2 || range[0] > range[1])
                    throw new JsonException("Halloween candy byTemplate entry " + templateId +
                        " must be [min, max] with min <= max");
            foreach (var (candyKey, range) in config.Ambush.Candy)
                if (range is null || range.Length != 2 || range[0] > range[1])
                    throw new JsonException("Halloween ambush.candy entry " + candyKey +
                        " must be [min, max] with min <= max");
            foreach (var town in config.Invasion.Towns)
                if (town.Rect is null || town.Rect.Length != 4 || town.Rect[2] <= 0 || town.Rect[3] <= 0)
                    throw new JsonException("Halloween invasion town " + town.Map +
                        " rect must have exactly 4 elements with positive width and height");
            if (config.Weeper.CapPerHit <= 0)
                throw new JsonException("Halloween weeper.capPerHit must be > 0");
            if (config.Weeper.BleedMaxStacks <= 0 || config.Weeper.BleedSeconds <= 0 ||
                config.Weeper.BleedPercentPerStack <= 0)
                throw new JsonException("Halloween weeper bleedMaxStacks, bleedSeconds and bleedPercentPerStack must be > 0");
            if (config.Weeper.Start is null || config.Weeper.Start.Length != 2)
                throw new JsonException("Halloween weeper.start must be a [x, y] array");
            if (config.Weeper.Loops.Count == 0)
                throw new JsonException("Halloween weeper.loops must not be empty");
            foreach (var (loop, points) in config.Weeper.Loops)
            {
                if (points is null || points.Length == 0)
                    throw new JsonException("Halloween weeper loop " + loop + " must not be empty");
                foreach (var point in points)
                    if (point is null || point.Length != 2)
                        throw new JsonException("Halloween weeper loop " + loop + " waypoints must be [x, y] arrays");
            }
            if (config.Weeper.HitPercent <= 0)
                throw new JsonException("Halloween weeper.hitPercent must be > 0");
            if (config.Weeper.StragglerMultiplier < 1)
                throw new JsonException("Halloween weeper.stragglerMultiplier must be >= 1");
            if (config.Weeper.StragglerRadius <= 0 || config.Weeper.LungeSeconds <= 0 || config.Weeper.LungeRadius <= 0 ||
                config.Weeper.LeashRadius <= 0 || config.Weeper.LeashSeconds <= 0 || config.Weeper.LeashMaxDistance <= 0 ||
                config.Weeper.ResetSeconds <= 0 || config.Weeper.FootstepRadius <= 0 || config.Weeper.FootstepSeconds <= 0 ||
                config.Weeper.BleedTickSeconds <= 0)
                throw new JsonException("Halloween weeper stragglerRadius, lungeSeconds, lungeRadius, leashRadius, leashSeconds, " +
                    "leashMaxDistance, resetSeconds, footstepRadius, footstepSeconds and bleedTickSeconds must be > 0");
            if (config.Groundskeeper.Curve.Band <= 0 || config.Groundskeeper.Curve.Exponent <= 0)
                throw new JsonException("Halloween groundskeeper.curve band and exponent must be > 0");
            if (config.Groundskeeper.Curve.Floor < 0)
                throw new JsonException("Halloween groundskeeper.curve.floor must be >= 0");
            if (config.Groundskeeper.HpPerPlayer <= 0)
                throw new JsonException("Halloween groundskeeper.hpPerPlayer must be > 0");
            if (config.Groundskeeper.BloodyDamageMultiplier <= 0 || config.Groundskeeper.BloodyDamageMultiplier > 1)
                throw new JsonException("Halloween groundskeeper.bloodyDamageMultiplier must be in (0, 1]");
            if (config.Groundskeeper.Phases is null || config.Groundskeeper.Phases.Length != 4)
                throw new JsonException("Halloween groundskeeper.phases must have exactly 4 values");
            foreach (var phase in config.Groundskeeper.Phases)
                if (phase <= 0 || phase > 1)
                    throw new JsonException("Halloween groundskeeper.phases values must be in (0, 1]");
            for (int i = 1; i < config.Groundskeeper.Phases.Length; i++)
                if (config.Groundskeeper.Phases[i - 1] <= config.Groundskeeper.Phases[i])
                    throw new JsonException("Halloween groundskeeper.phases must be strictly descending");
            if (config.Groundskeeper.HitPercent <= 0 || config.Groundskeeper.BigHitPercent <= 0)
                throw new JsonException("Halloween groundskeeper.hitPercent and bigHitPercent must be > 0");
            if (config.Groundskeeper.BigHitEvery <= 1)
                throw new JsonException("Halloween groundskeeper.bigHitEvery must be > 1");
            if (config.Groundskeeper.BigHitWarnSeconds <= 0 || config.Groundskeeper.EnrageAttackSpeed <= 0 ||
                config.Groundskeeper.LightsOutSeconds <= 0 || config.Groundskeeper.LightsOutAlpha <= 0 ||
                config.Groundskeeper.EmptyDespawnSeconds <= 0)
                throw new JsonException("Halloween groundskeeper bigHitWarnSeconds, enrageAttackSpeed, lightsOutSeconds, " +
                    "lightsOutAlpha and emptyDespawnSeconds must be > 0");
            if (config.Groundskeeper.JumpSeconds < 0)
                throw new JsonException("Halloween groundskeeper.jumpSeconds must be >= 0");
            if (config.Groundskeeper.FinalLightsOutSeconds < 0)
                throw new JsonException("Halloween groundskeeper.finalLightsOutSeconds must be >= 0");
            if (config.Groundskeeper.FinalLightsOutEverySeconds is not ([] or [>= 0, >= 0]) ||
                config.Groundskeeper.FinalLightsOutEverySeconds is [var min, var max] && min > max)
                throw new JsonException("Halloween groundskeeper.finalLightsOutEverySeconds must be [min, max] seconds");
            foreach (var (kind, entries) in config.Groundskeeper.Adds)
                if (entries is null || entries.Any(e => e is null || e.Length != 2 || e[1] < 0))
                    throw new JsonException("Halloween groundskeeper.adds entry " + kind +
                        " must be a list of [templateId, count] with count >= 0");
            for (int i = 0; i < config.Goal.Tiers.Length; i++)
                if (config.Goal.Tiers[i] <= 0 || (i > 0 && config.Goal.Tiers[i] <= config.Goal.Tiers[i - 1]))
                    throw new JsonException("Halloween goal.tiers must be positive and strictly ascending");
            if (config.StalkerChase.ChaseSeconds <= 0 || config.StalkerChase.StartDelaySeconds <= 0 ||
                config.StalkerChase.SpeedOffset <= 0 || config.StalkerChase.WarnRadius <= 0 ||
                config.StalkerChase.WarnSeconds <= 0)
                throw new JsonException("Halloween stalkerChase chaseSeconds, startDelaySeconds, speedOffset, warnRadius " +
                    "and warnSeconds must be > 0");
            if (config.StalkerChase.MinDelaySeconds < 0)
                throw new JsonException("Halloween stalkerChase.minDelaySeconds must be >= 0");
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
                log.Error(e, "Halloween config {0} unusable; event disabled", path);
            }
            failedKey = key;
            cached = null;
            return Disabled;
        }
    }

    private static void Require<T>(T? value, string name) where T : class
    {
        if (value is null) throw new JsonException("Halloween " + name + " must not be null");
    }
}
