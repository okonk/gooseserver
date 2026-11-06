using Goose;
using Goose.Scripting;

namespace Goose.Tests.Arena;

public sealed class ArenaMapTests : IDisposable
{
    private readonly ArenaTestKit kit = new();
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private const string ClosedMessage = "The arena is closed. Wait in the lobby for the next run.";

    public void Dispose() => kit.Dispose();

    private static object? Run(ArenaTestKit kit) => ArenaTestKit.Call(kit.Host, "Run", kit.World);
    private static object? LastRun(ArenaTestKit kit) => ArenaTestKit.Call(kit.Host, "LastRun", kit.World);
    private static void StartRun(ArenaTestKit kit, DateTime now) => ArenaTestKit.Call(kit.Host, "StartRun", kit.World, now);
    private static void Tick(ArenaTestKit kit, DateTime now) => ArenaTestKit.Call(kit.Host, "Tick", kit.World, now);

    private static object? Prop(object? obj, string name) => ArenaTestKit.GetProp(obj, name);
    private static object LastResult(ArenaTestKit kit, string name)
    {
        var last = LastRun(kit)!;
        return ((System.Collections.IEnumerable)Prop(last, "Results")!)
            .Cast<object>()
            .Single(r => (string)Prop(r, "Name") == name);
    }

    [Fact]
    public void Non_participant_cannot_join_and_gm_can()
    {
        var script = kit.AttachMapScript();
        var p1 = kit.LobbyPlayer(70001, "P1");
        Assert.Equal(ClosedMessage, script.Object.CanPlayerJoin(kit.ArenaMap, p1, kit.World));
        var gm = kit.LobbyPlayer(70002, "GM", gm: true);
        Assert.Null(script.Object.CanPlayerJoin(kit.ArenaMap, gm, kit.World));

        StartRun(kit, Now);
        Assert.Null(script.Object.CanPlayerJoin(kit.ArenaMap, p1, kit.World));
        var p2 = kit.LobbyPlayer(70003, "P2");
        Assert.Equal(ClosedMessage, script.Object.CanPlayerJoin(kit.ArenaMap, p2, kit.World));
    }

    [Fact]
    public void Relogging_non_participant_is_evicted_to_lobby()
    {
        kit.AttachMapScript();
        var p = kit.LobbyPlayer(70001, "P1");
        kit.Enter(kit.ArenaMap, p);

        Assert.Contains(p.Sent, s => s.Contains(ClosedMessage));
        Assert.Equal(ArenaTestKit.LobbyMapId, p.MapID);
        Assert.Equal(10, p.MapX);
        Assert.Equal(10, p.MapY);
    }

    [Fact]
    public void Death_eliminates_and_respawns_in_lobby()
    {
        kit.AttachMapScript();
        var p1 = kit.LobbyPlayer(70001, "P1");
        p1.ExperienceSold = 3_000_000_000;
        StartRun(kit, Now);
        Tick(kit, Now.AddSeconds(11));
        kit.Enter(kit.ArenaMap, p1);
        p1.CurrentHP = 1;
        var mob = kit.ArenaMap.NPCs.First();
        mob.Attack(p1, kit.World);

        Assert.Equal(ArenaTestKit.LobbyMapId, p1.MapID);
        Assert.Equal(p1.MaxHP / 2, p1.CurrentHP);
        Assert.Null(Run(kit));
        Assert.NotNull(LastRun(kit));
        Assert.Equal(0, (int)Prop(LastResult(kit, "P1"), "RoundsCleared"));
    }

    [Fact]
    public void Death_then_left_eliminates_once()
    {
        kit.AttachMapScript();
        var p1 = kit.LobbyPlayer(70001, "P1");
        p1.ExperienceSold = 3_000_000_000;
        StartRun(kit, Now);
        Tick(kit, Now.AddSeconds(11));
        kit.Enter(kit.ArenaMap, p1);
        p1.CurrentHP = 1;
        var mob = kit.ArenaMap.NPCs.First();
        mob.Attack(p1, kit.World);
        kit.ArenaMap.RemovePlayer(p1, kit.World);

        Assert.Equal(1, p1.Sent.Count(s => s.Contains("You were eliminated")));
        Assert.NotNull(LastRun(kit));
    }

    [Fact]
    public void Disconnect_eliminates()
    {
        kit.AttachMapScript();
        var p1 = kit.LobbyPlayer(70001, "P1");
        StartRun(kit, Now);
        Tick(kit, Now.AddSeconds(11));
        kit.Enter(kit.ArenaMap, p1);
        kit.ArenaMap.RemovePlayer(p1, kit.World);

        Assert.Null(Run(kit));
        Assert.NotNull(LastRun(kit));
        Assert.Equal(0, (int)Prop(LastResult(kit, "P1"), "RoundsCleared"));
    }
}
