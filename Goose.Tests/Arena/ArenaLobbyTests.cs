using Goose.Scripting;
using Goose.Testing;

namespace Goose.Tests.Arena;

public sealed class ArenaLobbyTests : IDisposable
{
    private readonly TestWorldFixture fixture = new();
    private readonly Map lobby;

    public ArenaLobbyTests()
    {
        lobby = fixture.AddBaseMap(57, "Arena Lobby", 60, 30);
        var folder = Path.Combine(fixture.DataDirectory, "Scripts", "Arena");
        Directory.CreateDirectory(folder);
        foreach (var file in new[] { "ArenaNurse.csx", "ArenaLobby.csx" })
            File.Copy(Path.Combine(ArenaTestKit.ShippedDataDirectory, "Scripts", "Arena", file), Path.Combine(folder, file), overwrite: true);
    }

    public void Dispose() => fixture.Dispose();

    [Fact]
    public void Patrol_nurse_punches_each_patient_facing_right_then_rests_and_returns()
    {
        foreach (var x in new[] { 39, 41, 43, 45 })
        {
            lobby.SetTile(x, 15, new BlockedTile());
            lobby.SetTile(x, 17, new BlockedTile());
        }
        var patient = fixture.AddNpcTemplate(446, "Patient", t => t.NPCType = NPCTemplate.Types.Ambient);
        foreach (var x in new[] { 39, 43, 45 })
            fixture.SpawnNpc(lobby, x, 16, patient);
        var script = fixture.World.ScriptHandler.GetScript<INPCScript>("Scripts/Arena/ArenaNurse.csx");
        var template = fixture.AddNpcTemplate(444, "Patroling Lobby Nurse", t =>
        {
            t.NPCType = NPCTemplate.Types.Ambient;
            t.Script = script;
            t.MoveSpeed = 1;
        });
        var nurse = fixture.SpawnNpc(lobby, 37, 15, template);
        var watcher = fixture.CommandPlayerOn(lobby, 40, 20);
        fixture.AddOnlinePlayer(watcher);
        lobby.Players.Add(watcher);

        var punches = new List<(int X, int Y, Direction Facing)>();
        var visited = new List<(int X, int Y)>();
        for (int tick = 0; tick < 200 && punches.Count < 3; tick++)
        {
            nurse.Properties["nurse.until"] = 0L;
            watcher.Sent.Clear();
            script.Object.OnMoveEvent(nurse, fixture.World);
            visited.Add((nurse.MapX, nurse.MapY));
            if (watcher.Sent.Any(p => p.StartsWith("ATT" + nurse.LoginID)))
                punches.Add((nurse.MapX, nurse.MapY, nurse.Facing));
        }

        Assert.Equal(new[] { (38, 16, Direction.Right), (42, 16, Direction.Right), (44, 16, Direction.Right) }, punches.ToArray());
        Assert.DoesNotContain(visited, t => t is (39, 16) or (43, 16) or (45, 16));

        for (int tick = 0; tick < 200 && (nurse.MapX, nurse.MapY) != (40, 12); tick++)
        {
            nurse.Properties["nurse.until"] = 0L;
            script.Object.OnMoveEvent(nurse, fixture.World);
        }
        Assert.Equal((40, 12), (nurse.MapX, nurse.MapY));

        script.Object.OnMoveEvent(nurse, fixture.World);
        Assert.Equal((40, 12), (nurse.MapX, nurse.MapY));
        Assert.Equal(Direction.Down, nurse.Facing);

        for (int tick = 0; tick < 200 && (nurse.MapX, nurse.MapY) != (37, 15); tick++)
        {
            nurse.Properties["nurse.until"] = 0L;
            script.Object.OnMoveEvent(nurse, fixture.World);
        }
        Assert.Equal((37, 15), (nurse.MapX, nurse.MapY));
    }

    [Fact]
    public void Stepping_or_turning_on_the_plaque_tile_shows_the_notice()
    {
        var script = fixture.World.ScriptHandler.GetScript<IMapScript>("Scripts/Arena/ArenaLobby.csx");
        var player = fixture.CommandPlayerOn(lobby, 19, 7);
        fixture.AddOnlinePlayer(player);
        player.Sent.Clear();

        script.Object.OnPlayerMove(lobby, player, fixture.World);
        Assert.Contains(player.Sent, p => p.Contains("LazyGoose, World First Doomslayer"));

        player.Sent.Clear();
        script.Object.OnPlayerTurn(lobby, player, fixture.World);
        Assert.Contains(player.Sent, p => p.Contains("LazyGoose, World First Doomslayer"));

        var elsewhere = fixture.CommandPlayerOn(lobby, 20, 7, "Other");
        fixture.AddOnlinePlayer(elsewhere);
        elsewhere.Sent.Clear();
        script.Object.OnPlayerMove(lobby, elsewhere, fixture.World);
        Assert.DoesNotContain(elsewhere.Sent, p => p.Contains("LazyGoose"));
    }
}
