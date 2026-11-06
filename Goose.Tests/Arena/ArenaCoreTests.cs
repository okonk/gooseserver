using System.Text.Json;
using Goose;
using Goose.Scripting;

namespace Goose.Tests.Arena;

public sealed class ArenaCoreTests : IDisposable
{
    private readonly ArenaTestKit kit = new();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public void Dispose() => kit.Dispose();

    private static object? Run(ArenaTestKit kit) => ArenaTestKit.Call(kit.Host, "Run", kit.World);
    private static object? LastRun(ArenaTestKit kit) => ArenaTestKit.Call(kit.Host, "LastRun", kit.World);
    private static void SaveRun(ArenaTestKit kit, object run) => ArenaTestKit.Call(kit.Host, "SaveRun", kit.World, run);
    private static void StartRun(ArenaTestKit kit, DateTime now) => ArenaTestKit.Call(kit.Host, "StartRun", kit.World, now);
    private static void Eliminate(ArenaTestKit kit, int id, string reason) => ArenaTestKit.Call(kit.Host, "Eliminate", kit.World, id, reason);
    private static void EndRun(ArenaTestKit kit, string reason) => ArenaTestKit.Call(kit.Host, "EndRun", kit.World, reason);

    private static object? Prop(object? obj, string name) => ArenaTestKit.GetProp(obj, name);
    private static void SetProp(object obj, string name, object? value) => obj.GetType().GetProperty(name)!.SetValue(obj, value);
    private static System.Collections.IDictionary Participants(object run) => (System.Collections.IDictionary)Prop(run, "Participants")!;

    [Fact]
    public void Start_moves_lobby_players_and_skips_gms_and_other_maps()
    {
        var p1 = kit.LobbyPlayer(70001, "Alice");
        var p2 = kit.LobbyPlayer(70002, "GM", gm: true);
        var other = kit.Fixture.AddBaseMap(40003, "Elsewhere", 100, 100);
        var p3 = kit.Fixture.CommandPlayerOn(other, 5, 5, "Bob");
        p3.PlayerID = 70003;
        kit.Fixture.AddOnlinePlayer(p3);
        other.Players.Add(p3);

        StartRun(kit, Now);

        var run = Run(kit);
        Assert.NotNull(run);
        Assert.Equal(0, (int)Prop(run!, "Phase")!);
        Assert.Equal(Now.AddSeconds(10), (DateTime)Prop(run, "PhaseUntilUtc")!);
        var participants = Participants(run);
        Assert.Equal(1, participants.Count);
        Assert.Equal(true, (bool)Prop(participants[70001], "Alive")!);

        Assert.Equal(ArenaTestKit.ArenaMapId, p1.MapID);
        Assert.InRange(p1.MapX, ArenaTestKit.ArenaRect[0], ArenaTestKit.ArenaRect[0] + ArenaTestKit.ArenaRect[2] - 1);
        Assert.InRange(p1.MapY, ArenaTestKit.ArenaRect[1], ArenaTestKit.ArenaRect[1] + ArenaTestKit.ArenaRect[3] - 1);
        Assert.Equal(ArenaTestKit.LobbyMapId, p2.MapID);
        Assert.Equal(40003, p3.MapID);
        Assert.Contains(p1.Sent, s => s.Contains("[Arena]"));
    }

    [Fact]
    public void Start_with_empty_lobby_creates_no_run()
    {
        StartRun(kit, Now);

        Assert.Null(Run(kit));
    }

    [Fact]
    public void Participant_is_saved_before_warp()
    {
        var p1 = kit.LobbyPlayer(70001, "Alice");
        File.WriteAllText(Path.Combine(kit.Fixture.DataDirectory, "Scripts", "Arena", "TestStubMap.csx"),
            StubMapScript);
        var stub = kit.World.ScriptHandler.GetScript<IMapScript>("Scripts/Arena/TestStubMap.csx");
        var arenaMap = kit.World.MapHandler.Maps[ArenaTestKit.ArenaMapId];
        arenaMap.Script = stub;

        StartRun(kit, Now);
        kit.Enter(arenaMap, p1);

        var entered = (List<bool>)stub.Object.GetType().GetField("Entered")!.GetValue(stub.Object)!;
        Assert.True(entered.Last());
    }

    [Fact]
    public void Eliminate_twice_counts_once()
    {
        var p1 = kit.LobbyPlayer(70001, "Alice");
        StartRun(kit, Now);
        var run = Run(kit)!;
        SetProp(Participants(run)[70001], "RoundsCleared", 10);
        SaveRun(kit, run);

        Eliminate(kit, 70001, "test");
        Eliminate(kit, 70001, "test");

        Assert.Null(Run(kit));
        Assert.Equal(60, kit.Wallet(p1));
        var last = LastRun(kit);
        Assert.NotNull(last);
        Assert.Equal(10, (int)Prop(last!, "BestRound")!);
    }

    [Fact]
    public void Last_elimination_ends_run_pays_milestones_and_clears_state()
    {
        var p1 = kit.LobbyPlayer(70001, "Alice");
        var p2 = kit.LobbyPlayer(70002, "Bob");
        StartRun(kit, Now);
        var run = Run(kit)!;
        SetProp(Participants(run)[70001], "RoundsCleared", 5);
        SetProp(Participants(run)[70002], "RoundsCleared", 15);
        SaveRun(kit, run);

        Eliminate(kit, 70001, "test");
        var still = Run(kit);
        Assert.NotNull(still);
        Assert.Equal(true, (bool)Prop(Participants(still!)[70002], "Alive")!);

        Eliminate(kit, 70002, "test");

        Assert.Null(Run(kit));
        var last = LastRun(kit);
        Assert.NotNull(last);
        Assert.Equal(15, (int)Prop(last!, "BestRound")!);
        var results = (System.Collections.IEnumerable)Prop(last, "Results")!;
        Assert.Equal(2, results.Cast<object>().Count());
        Assert.Equal(20, kit.Wallet(p1));
        Assert.Equal(120, kit.Wallet(p2));
        Assert.Contains(p1.Sent, s => s.Contains("+20 Arena Coins (milestone bonus)"));
        Assert.Contains(p2.Sent, s => s.Contains("+120 Arena Coins (milestone bonus)"));
    }

    [Fact]
    public void Offline_participant_earns_no_coins()
    {
        var p1 = kit.LobbyPlayer(70001, "Alice");
        StartRun(kit, Now);
        var run = Run(kit)!;
        SetProp(Participants(run)[70001], "RoundsCleared", 5);
        SaveRun(kit, run);

        kit.World.PlayerHandler.RemovePlayer(p1);
        Eliminate(kit, 70001, "test");

        Assert.Null(Run(kit));
        var last = LastRun(kit);
        Assert.NotNull(last);
        var result = ((System.Collections.IEnumerable)Prop(last!, "Results")!).Cast<object>().Single();
        Assert.Equal(0L, (long)Prop(result, "Coins")!);
    }

    [Fact]
    public void End_run_despawns_only_this_runs_mobs()
    {
        var p1 = kit.LobbyPlayer(70001, "Alice");
        StartRun(kit, Now);
        var runId = (string)Prop(Run(kit)!, "RunId")!;
        var arenaMap = kit.World.MapHandler.Maps[ArenaTestKit.ArenaMapId];
        var template = kit.Fixture.AddNpcTemplate(42001, "Tagged Mob");
        var tagged = kit.Fixture.SpawnNpc(arenaMap, 20, 20, template, false, new PropertiesDictionary { ["arena.run"] = runId });
        var other = kit.Fixture.SpawnNpc(arenaMap, 21, 21, template, false, new PropertiesDictionary { ["arena.run"] = "other" });

        EndRun(kit, "test");

        Assert.DoesNotContain(arenaMap.NPCs, n => n == tagged);
        Assert.Equal(NPC.States.Alive, other.State);
        Assert.Contains(arenaMap.NPCs, n => n == other);
    }

    [Fact]
    public void End_run_with_onplayerleft_eliminate_does_not_corrupt_last_run()
    {
        var p1 = kit.LobbyPlayer(70001, "Alice");
        StartRun(kit, Now);
        var run = Run(kit)!;
        SetProp(Participants(run)[70001], "RoundsCleared", 10);
        SaveRun(kit, run);

        File.WriteAllText(Path.Combine(kit.Fixture.DataDirectory, "Scripts", "Arena", "TestLeftMap.csx"),
            LeftStubMapScript);
        var stub = kit.World.ScriptHandler.GetScript<IMapScript>("Scripts/Arena/TestLeftMap.csx");
        var arenaMap = kit.World.MapHandler.Maps[ArenaTestKit.ArenaMapId];
        arenaMap.Script = stub;

        p1.Map = arenaMap;
        p1.MapID = ArenaTestKit.ArenaMapId;
        arenaMap.Players.Add(p1);

        EndRun(kit, "test");

        Assert.Null(Run(kit));
        var last = LastRun(kit);
        Assert.NotNull(last);
        Assert.Equal(10, (int)Prop(last!, "BestRound")!);
        var results = (System.Collections.IEnumerable)Prop(last, "Results")!;
        Assert.Equal(60L, (long)Prop(results.Cast<object>().Single(), "Coins")!);
        Assert.Equal(60, kit.Wallet(p1));
        Assert.Equal(1, p1.Sent.Count(s => s.Contains("[Arena] The arena is closed")));
        Assert.DoesNotContain(p1.Sent, s => s.Contains("You were eliminated"));
    }

    [Fact]
    public void State_round_trips_through_world_state_save_and_load()
    {
        var p1 = kit.LobbyPlayer(70001, "Alice");
        StartRun(kit, Now);
        var before = Run(kit)!;
        var liveJson = kit.World.WorldState.Get<string>("arena.run");
        Assert.NotNull(liveJson);

        var plan = kit.World.WorldState.PlanSave();
        var upsert = plan.Upserts.Single(u => u.Key == "arena.run");
        var state = new WorldState();
        state.LoadRows(plan.Upserts.Select(u => new KeyValuePair<string, string>(u.Key, u.Json)));
        var roundTripped = state.Get<string>("arena.run");

        Assert.Equal(liveJson, roundTripped);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(roundTripped!);
        Assert.Equal((string)Prop(before, "RunId")!, parsed!["RunId"].GetString());
        Assert.Equal((int)Prop(before, "Phase")!, parsed["Phase"].GetInt32());
        Assert.Contains("70001", parsed["Participants"].GetRawText());
    }

    private const string LeftStubMapScript = """
        #load "ArenaCore.csx"
        using System;
        using Goose;
        using Goose.Scripting;

        public class TestLeftMap : BaseMapScript
        {
            public override void OnPlayerLeft(Map map, Player player, GameWorld world)
            {
                ArenaCore.Eliminate(world, player.PlayerID, "left the arena");
            }
        }

        return typeof(TestLeftMap);
        """;

    private const string StubMapScript = """
        #load "ArenaCore.csx"
        using System;
        using System.Collections.Generic;
        using Goose;
        using Goose.Scripting;

        public class TestStubMap : BaseMapScript
        {
            public static List<bool> Entered = new();

            public override void OnPlayerEntered(Map map, Player player, GameWorld world)
            {
                Entered.Add(ArenaCore.IsParticipant(world, player.PlayerID));
            }
        }

        return typeof(TestStubMap);
        """;
}
