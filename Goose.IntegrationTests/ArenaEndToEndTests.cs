using System.Text.Json;
using System.Text.Json.Nodes;
using Goose;
using Goose.IntegrationTests.Fixtures;
using Goose.Scripting;

namespace Goose.IntegrationTests;

public sealed class ArenaEndToEndTests : IDisposable
{
    private readonly ArenaFixture fixture = new();

    public void Dispose() => fixture.Dispose();

    private static void Tick(ArenaFixture fixture, object controller, DateTime now)
        => controller.GetType().GetMethod("Tick")!.Invoke(controller, new object?[] { fixture.World, now });

    private static JsonObject Run(ArenaFixture fixture)
        => JsonSerializer.Deserialize<JsonObject>(fixture.World.WorldState.Get<string>("arena.run")!)!;

    private static long Wallet(ArenaFixture fixture, Player player)
        => fixture.World.CurrencyHandler.Get("arena_coin")!.GetBalance(player);

    [Fact]
    public void All_arena_scripts_compile()
    {
        fixture.InstallShippedScripts();
        fixture.AddArenaMaps();

        var controller = fixture.World.ScriptHandler.GetScript<IGlobalScript>("Scripts/Global/Arena.csx");
        var mapScript = fixture.World.ScriptHandler.GetScript<IMapScript>("Scripts/Arena/ArenaMap.csx");
        var mobScript = fixture.World.ScriptHandler.GetScript<INPCScript>("Scripts/Arena/ArenaMob.csx");

        Assert.NotNull(controller.Object);
        Assert.NotNull(mapScript.Object);
        Assert.NotNull(mobScript.Object);
    }

    [Fact]
    public void Full_run_two_players()
    {
        fixture.InstallShippedScripts();
        fixture.AddArenaMaps();
        fixture.AddArenaMobTemplates();

        var controller = fixture.CompileController().Object;
        var mapScript = fixture.CompileMapScript();
        var arenaMap = fixture.World.MapHandler.Maps[ArenaFixture.ArenaMapId];
        arenaMap.Script = mapScript;

        fixture.WriteConfigForTest();
        fixture.World.CurrencyHandler.Register(new DataCurrency("arena_coin", "Arena Coin", "Arena", null, true));
        fixture.Settings.MaxAC = 255;

        var p1 = fixture.LobbyPlayer(70001, "P1");
        var p2 = fixture.LobbyPlayer(70002, "P2");
        p1.ExperienceSold = 3_000_000_000;
        p2.ExperienceSold = 3_000_000_000;

        // Shipped schedule: anchor 00:00 UTC every 3 hours. One tick just before the 03:00
        // anchor stores lastTick; the next tick just after it crosses the anchor and starts.
        var t0 = DateTime.UtcNow.Date.AddHours(2).AddMinutes(59).AddSeconds(30);
        Tick(fixture, controller, t0);
        Assert.Null(fixture.World.WorldState.Get<string>("arena.run"));

        var start = t0.AddSeconds(60);
        Tick(fixture, controller, start);
        Assert.NotNull(fixture.World.WorldState.Get<string>("arena.run"));
        Assert.Equal(ArenaFixture.ArenaMapId, p1.MapID);
        Assert.Equal(ArenaFixture.ArenaMapId, p2.MapID);

        fixture.EnterArena(p1);
        fixture.EnterArena(p2);

        Tick(fixture, controller, start.AddSeconds(11));
        Assert.Equal(8, arenaMap.NPCs.Count);

        foreach (var npc in arenaMap.NPCs.ToList())
            npc.Attacked(p1, long.MaxValue / 4, fixture.World);

        Tick(fixture, controller, start.AddSeconds(12));
        var run = Run(fixture);
        Assert.Equal(2, run["Phase"]!.GetValue<int>());
        Assert.Equal(14, Wallet(fixture, p1));
        Assert.Equal(6, Wallet(fixture, p2));

        Tick(fixture, controller, start.AddSeconds(28));
        Assert.Equal(8, arenaMap.NPCs.Count);

        p1.CurrentHP = 1;
        arenaMap.NPCs.First().Attack(p1, fixture.World);
        Assert.Equal(ArenaFixture.LobbyMapId, p1.MapID);
        Assert.False(Run(fixture)["Participants"]!["70001"]!.AsObject()["Alive"]!.GetValue<bool>());
        Assert.True(Run(fixture)["Participants"]!["70002"]!.AsObject()["Alive"]!.GetValue<bool>());

        p2.WarpTo(fixture.World, fixture.World.MapHandler.Maps[ArenaFixture.LobbyMapId], 10, 10);

        Assert.Null(fixture.World.WorldState.Get<string>("arena.run"));
        var last = JsonSerializer.Deserialize<JsonObject>(
            fixture.World.WorldState.Get<string>("arena.lastRun")!)!;
        var results = last["Results"]!.AsArray().ToList();
        Assert.Equal(2, results.Count);
        var p1Result = results.Single(r => r!["Name"]!.GetValue<string>() == "P1")!;
        var p2Result = results.Single(r => r!["Name"]!.GetValue<string>() == "P2")!;
        Assert.Equal(1, p1Result["RoundsCleared"]!.GetValue<int>());
        Assert.Equal(1, p2Result["RoundsCleared"]!.GetValue<int>());
        Assert.Equal(14, p1Result["Coins"]!.GetValue<long>());
        Assert.Equal(6, p2Result["Coins"]!.GetValue<long>());
        Assert.Equal(ArenaFixture.LobbyMapId, p2.MapID);
        Assert.Empty(arenaMap.NPCs);
        Assert.Contains(p1.Sent, s => s.Contains("You were eliminated"));

        var board = JsonSerializer.Deserialize<JsonObject>(
            fixture.World.WorldState.Get<string>("arena.board")!)!;
        var boardPlayers = board["Players"]!.AsObject();
        Assert.Equal(2, boardPlayers.Count);
        var b1 = boardPlayers["70001"]!.AsObject();
        Assert.Equal(1, b1["BestRound"]!.GetValue<int>());
        Assert.Equal(8, b1["Kills"]!.GetValue<long>());
        Assert.Equal(1, b1["WeekBestRound"]!.GetValue<int>());
        Assert.Equal(8, b1["WeekKills"]!.GetValue<long>());
        var b2 = boardPlayers["70002"]!.AsObject();
        Assert.Equal(1, b2["BestRound"]!.GetValue<int>());
        Assert.Equal(0, b2["Kills"]!.GetValue<long>());
        Assert.Equal(1, b2["WeekBestRound"]!.GetValue<int>());
        Assert.Equal(0, b2["WeekKills"]!.GetValue<long>());
    }
}
