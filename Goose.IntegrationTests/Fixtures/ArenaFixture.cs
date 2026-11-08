using System.Text.Json;
using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;
using Goose.Testing;

namespace Goose.IntegrationTests.Fixtures;

public sealed class ArenaFixture : TestWorldFixture
{
    public const int LobbyMapId = 40001;
    public const int ArenaMapId = 40002;
    public static readonly int[] BandNormalIds = [41001, 41002, 41003];
    public const int BandBossId = 41101;

    private static readonly (string Source, string Relative)[] ShippedScripts =
    {
        ("ArenaConfig.csx",     "Scripts/Arena/ArenaConfig.csx"),
        ("ArenaMath.csx",       "Scripts/Arena/ArenaMath.csx"),
        ("ArenaState.csx",      "Scripts/Arena/ArenaState.csx"),
        ("ArenaBoard.csx",      "Scripts/Arena/ArenaBoard.csx"),
        ("ArenaCore.csx",       "Scripts/Arena/ArenaCore.csx"),
        ("ArenaSpells.csx",     "Scripts/Arena/ArenaSpells.csx"),
        ("ArenaMob.csx",        "Scripts/Arena/ArenaMob.csx"),
        ("ArenaMap.csx",        "Scripts/Arena/ArenaMap.csx"),
        ("ArenaController.csx", "Scripts/Arena/ArenaController.csx"),
        ("Arena.csx",           "Scripts/Global/Arena.csx"),
    };

    public void InstallShippedScripts()
    {
        Directory.CreateDirectory(Path.Combine(DataDirectory, "Scripts", "Arena"));
        foreach (var (source, relative) in ShippedScripts)
        {
            var from = Path.Combine(AppContext.BaseDirectory, "ArenaScripts", source);
            if (!File.Exists(from))
                throw new FileNotFoundException(
                    $"{source} is not in the test output. Add its <None Include> to Goose.IntegrationTests.csproj.", from);
            File.Copy(from, Path.Combine(DataDirectory, relative), overwrite: true);
        }
        File.Copy(Path.Combine(AppContext.BaseDirectory, "ArenaScripts", "arena.json"),
            Path.Combine(DataDirectory, "Scripts", "Arena", "arena.json"), overwrite: true);
    }

    public void AddArenaMaps()
    {
        AddBaseMap(LobbyMapId, "Arena Lobby", 100, 100);
        AddBaseMap(ArenaMapId, "Arena", 100, 100);
    }

    public void AddArenaMobTemplates()
    {
        var mobScript = World.ScriptHandler.GetScript<INPCScript>("Scripts/Arena/ArenaMob.csx");
        foreach (var id in BandNormalIds)
            AddNpcTemplate(id, "Arena Mob " + id, t => t.Script = mobScript);
        AddNpcTemplate(BandBossId, "Arena Boss", t => t.Script = mobScript);
    }

    // Call after InstallShippedScripts/Compile*: it overwrites the shipped arena.json, whose
    // placeholder map ids (0) leave the arena disabled, and swaps in the fixture's mob templates.
    public void WriteConfigForTest()
    {
        var options = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        var json = JsonNode.Parse(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ArenaScripts", "arena.json")), null, options)!;
        json["lobbyMap"] = LobbyMapId;
        json["arenaMap"] = ArenaMapId;
        json["arenaRect"] = new JsonArray(10, 10, 80, 80);
        json["bands"] = new JsonArray(new JsonObject
        {
            ["normals"] = new JsonArray(BandNormalIds.Select(id => (JsonNode)id).ToArray()),
            ["boss"] = BandBossId,
        });
        File.WriteAllText(Path.Combine(DataDirectory, "Scripts", "Arena", "arena.json"), json.ToJsonString());
    }

    public Script<IGlobalScript> CompileController()
    {
        InstallShippedScripts();
        return World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/Arena.csx");
    }

    public Script<IMapScript> CompileMapScript()
    {
        InstallShippedScripts();
        return World.ScriptHandler.GetScript<IMapScript>("Scripts/Arena/ArenaMap.csx");
    }

    public CapturingPlayer LobbyPlayer(int playerId, string name)
    {
        var player = CommandPlayerOn(World.MapHandler.Maps[LobbyMapId], 10, 10, name);
        player.PlayerID = playerId;
        AddOnlinePlayer(player);
        World.MapHandler.Maps[LobbyMapId].Players.Add(player);
        return player;
    }

    public void EnterArena(CapturingPlayer player)
    {
        player.State = Player.States.Ready;
        player.Map = World.MapHandler.Maps[ArenaMapId];
        player.MapID = ArenaMapId;
        World.MapHandler.Maps[ArenaMapId].AddPlayer(player, World);
    }
}
