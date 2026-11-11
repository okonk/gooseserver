using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Arena;

public sealed class ArenaHypeTests : IDisposable
{
    private const int MayorId = 41900;
    private const int FanId = 41901;
    private readonly ArenaTestKit kit = new();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public void Dispose() => kit.Dispose();

    private object? Run() => ArenaTestKit.Call(kit.Host, "Run", kit.World);
    private void SaveRun(object run) => ArenaTestKit.Call(kit.Host, "SaveRun", kit.World, run);
    private void StartRun(DateTime now) => ArenaTestKit.Call(kit.Host, "StartRun", kit.World, now);
    private void Tick(DateTime now) => ArenaTestKit.Call(kit.Host, "Tick", kit.World, now);

    private NPC Spawn(int id, string name, int x, int y, Script<INPCScript>? script = null)
    {
        var template = kit.Fixture.AddNpcTemplate(id, name, t =>
        {
            t.NPCType = NPCTemplate.Types.Ambient;
            t.Script = script;
        });
        return kit.Fixture.SpawnNpc(kit.ArenaMap, x, y, template);
    }

    private TestWorldFixture.CapturingPlayer EnterRound1()
    {
        var player = kit.LobbyPlayer(70001, "Hero");
        StartRun(Now);
        kit.Enter(kit.ArenaMap, player);
        Tick(Now.AddSeconds(10));
        return player;
    }

    [Fact]
    public void Mayor_announces_the_boss_by_name_on_a_boss_round()
    {
        var mayor = Spawn(MayorId, "Mayor", 50, 50);
        kit.SetConfig(j => j["mayor"] = new JsonObject
        {
            ["templateId"] = MayorId,
            ["lines"] = new JsonObject { ["boss"] = new JsonArray("Behold {boss} in round {round}!") },
        });
        var player = EnterRound1();
        foreach (var npc in kit.ArenaMap.NPCs.Where(n => n.Properties.ContainsKey("arena.run")).ToList())
            npc.Attacked(player, long.MaxValue / 4, kit.World);

        var run = Run()!;
        ArenaTestKit.SetProp(run, "Round", 4);
        ArenaTestKit.SetProp(run, "Phase", 2);
        ArenaTestKit.SetProp(run, "PhaseUntilUtc", Now);
        SaveRun(run);
        Tick(Now.AddSeconds(11));

        Assert.Contains(P.Chat(mayor.LoginID, mayor.Name, "Behold Boss in round 5!") + "\x01", player.Sent);
    }

    [Fact]
    public void Crowd_near_a_participant_cheers_them_by_name()
    {
        kit.SetConfig(j => j["crowd"] = new JsonObject
        {
            ["everySeconds"] = new JsonArray(1, 1),
            ["booChance"] = 0,
            ["cheers"] = new JsonArray("GO {player}!"),
        });
        var player = EnterRound1();
        var crowdScript = kit.World.ScriptHandler.GetScript<INPCScript>("Scripts/Arena/ArenaCrowd.csx");
        var fan = Spawn(FanId, "Fan", player.MapX + 1, player.MapY, crowdScript);

        Tick(Now.AddSeconds(11));
        Tick(Now.AddSeconds(13));

        Assert.Contains(P.Chat(fan.LoginID, fan.Name, "GO Hero!") + "\x01", player.Sent);
    }

    [Fact]
    public void Nothing_is_said_without_mayor_or_crowd_config()
    {
        var player = EnterRound1();
        Tick(Now.AddSeconds(11));
        Tick(Now.AddSeconds(30));

        Assert.DoesNotContain(player.Sent, s => s.StartsWith("^"));
    }
}
