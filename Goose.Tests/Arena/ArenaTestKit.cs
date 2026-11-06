using System.Text.Json;
using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Arena;

public sealed class ArenaTestKit : IDisposable
{
    public TestWorldFixture Fixture { get; }
    public GameWorld World => Fixture.World;
    public object Host { get; }
    public string ConfigPath { get; }

    public const int LobbyMapId = 40001;
    public const int ArenaMapId = 40002;
    public static readonly int[] LobbySpawn = [10, 10];
    public static readonly int[] ArenaRect = [10, 10, 80, 80];
    public static readonly int[] BandNormalIds = [41001, 41002, 41003];
    public const int BandBossId = 41101;

    public ArenaTestKit()
    {
        Fixture = new TestWorldFixture();
        var arenaDir = Path.Combine(Fixture.DataDirectory, "Scripts", "Arena");
        Directory.CreateDirectory(arenaDir);
        ConfigPath = Path.Combine(arenaDir, "arena.json");
        foreach (var file in Directory.EnumerateFiles(Path.Combine(ShippedDataDirectory, "Scripts", "Arena"), "*.csx"))
            File.Copy(file, Path.Combine(arenaDir, Path.GetFileName(file)), overwrite: true);
        File.Copy(Path.Combine(ShippedDataDirectory, "Scripts", "Global", "Arena.csx"),
            Path.Combine(Fixture.DataDirectory, "Scripts", "Global", "Arena.csx"), overwrite: true);

        foreach (var id in BandNormalIds)
            Fixture.AddNpcTemplate(id, "Arena Mob " + id);
        Fixture.AddNpcTemplate(BandBossId, "Arena Boss");
        Fixture.AddBaseMap(LobbyMapId, "Arena Lobby", 100, 100);
        Fixture.AddBaseMap(ArenaMapId, "Arena", 100, 100);

        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(DefaultConfig()));
        File.WriteAllText(Path.Combine(Fixture.DataDirectory, "Scripts", "Global", "ArenaHost.csx"), HostScript);
        Host = Fixture.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/ArenaHost.csx").Object;
    }

    public void SetConfig(Action<JsonObject> mutate)
    {
        var json = JsonNode.Parse(File.ReadAllText(ConfigPath))!.AsObject();
        mutate(json);
        File.WriteAllText(ConfigPath, json.ToJsonString());
        File.SetLastWriteTimeUtc(ConfigPath, File.GetLastWriteTimeUtc(ConfigPath).AddSeconds(1));
    }

    public Script<T> Compile<T>(string folder, string file) where T : class
    {
        var targetDir = Path.Combine(Fixture.DataDirectory, "Scripts", folder);
        Directory.CreateDirectory(targetDir);
        File.Copy(Path.Combine(ShippedDataDirectory, "Scripts", folder, file),
            Path.Combine(targetDir, file), overwrite: true);
        return Fixture.World.ScriptHandler.GetScript<T>("Scripts/" + folder + "/" + file);
    }

    public static object? Call(object scriptObject, string method, params object?[] args)
        => scriptObject.GetType().GetMethod(method)!.Invoke(scriptObject, args);

    public void Dispose() => Fixture.Dispose();

    private object DefaultConfig()
    {
        return new
        {
            lobbyMap = LobbyMapId,
            lobbySpawn = LobbySpawn,
            arenaMap = ArenaMapId,
            arenaRect = ArenaRect,
            intervalHours = 3,
            anchorUtc = "00:00",
            announceMinutes = new[] { 15, 10, 5 },
            countdownSeconds = 10,
            breakSeconds = 15,
            roundSeconds = 180,
            timeoutWarningSeconds = 30,
            stragglerCount = 3,
            minPlayerDistance = 3,
            baseCount = 4,
            perPlayer = 2,
            bandSize = 10,
            bands = new[] { new { normals = BandNormalIds, boss = BandBossId } },
            eliteFromRound = 3,
            eliteChance = 0.05,
            eliteChanceGrowth = 0.02,
            eliteChanceMax = 0.30,
            eliteHpMult = 5,
            eliteDmgMult = 2,
            eliteTitle = "Elite",
            eliteTint = new[] { 255, 160, 0, 255 },
            bossEvery = 5,
            bossRoundAddFraction = 0.5,
            bossHpMult = 40,
            bossHpPerPlayer = 0.25,
            bossDmgMult = 3,
            minXpSold = 4000000,
            topXpSold = 3000000000,
            a = 0.75,
            b = 0.75,
            anchorRound = 20,
            anchorMobHp = 12000000,
            anchorMobWeaponDamage = 65000,
            hpGrowth = 1.10,
            dmgGrowth = 1.08,
            currencyId = "arena_coin",
            roundBase = 5,
            roundStep = 1,
            mobBounty = 1,
            eliteBountyMult = 5,
            bossBountyMult = 25,
            milestones = new Dictionary<string, long> { ["5"] = 20, ["10"] = 60, ["15"] = 120, ["20"] = 200 },
        };
    }

    private const string HostScript = """
        #load "../Arena/ArenaConfig.csx"
        using System;
        using Goose;
        using Goose.Scripting;

        public class ArenaHost : BaseGlobalScript
        {
            public object Config(GameWorld world) => ArenaConfig.Get(world);
            public bool Enabled(GameWorld world) => ArenaConfig.Get(world).Enabled(world);
        }

        return typeof(ArenaHost);
        """;

    public static string ShippedDataDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "Goose", "Data", "Illutia");
                if (Directory.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("Goose/Data/Illutia not found");
        }
    }
}
