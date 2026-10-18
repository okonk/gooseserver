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
        var pack = new HalloweenPack();
        int i = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;
            if (i == 0) pack.Min = reader.GetInt32();
            if (i == 1) pack.Max = reader.GetInt32();
            i++;
        }
        return pack;
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

public sealed class HalloweenAmbush
{
    public double BaseChance { get; set; }
    public double BasementWeight { get; set; }
    public double DefaultWeight { get; set; }
    public int CooldownSeconds { get; set; }
    public int BasementCooldownSeconds { get; set; }
    public int FakeCooldownSeconds { get; set; }
    public int GroupCooldownRadius { get; set; }
    public int PackRadius { get; set; }
    public int PackMax { get; set; }
    public int DespawnSeconds { get; set; }
    public int MaxLivePerMap { get; set; }
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

public sealed class HalloweenPentagram
{
    public int[][] Points { get; set; } = [];
    public int[] Centre { get; set; } = [];
}

public sealed class HalloweenInvasion
{
    public int[] Towns { get; set; } = [];
    public int[] Templates { get; set; } = [];
    public int Count { get; set; }
    public int EveryMinutes { get; set; }
    public int LifetimeMinutes { get; set; }
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
    public HalloweenAmbush Ambush { get; set; } = new();
    public HalloweenStalker Stalker { get; set; } = new();
    public HalloweenPentagram Pentagram { get; set; } = new();
    public HalloweenInvasion Invasion { get; set; } = new();

    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    private static HalloweenConfig? cached;
    private static DateTime cachedMtime;
    private static string lastErrorKey = "";

    public static HalloweenConfig Get(GameWorld world)
    {
        string path = world.Settings.DataPathAbsolute + "/Scripts/Global/Halloween/halloween.json";
        try
        {
            DateTime mtime = File.GetLastWriteTimeUtc(path);
            if (cached is not null && cachedMtime == mtime)
                return cached;
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new HalloweenPackConverter() },
            };
            var config = JsonSerializer.Deserialize<HalloweenConfig>(File.ReadAllText(path), options) ?? new HalloweenConfig();
            cached = config;
            cachedMtime = mtime;
            return config;
        }
        catch (Exception e)
        {
            string key;
            try
            {
                key = "mtime:" + File.GetLastWriteTimeUtc(path).ToString("O");
            }
            catch
            {
                key = "missing:" + path;
            }
            if (key != lastErrorKey)
            {
                lastErrorKey = key;
                log.Error(e, "Halloween config {0} unusable; event disabled", path);
            }
            return Disabled;
        }
    }
}
