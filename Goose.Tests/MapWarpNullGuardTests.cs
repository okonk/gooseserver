using Goose.Testing;

namespace Goose.Tests;

public class MapWarpNullGuardTests
{
    [Fact]
    public void MoveOntoWarpTileWithNullTargetMap_BouncesPlayerBack()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        // Tile index is y * Width + x (Goose/Map.cs), NOT y * (Width+1) + x.
        map.tiles[2 * map.Width + 2] = new WarpTile { WarpMap = null!, WarpX = 5, WarpY = 5 };
        var player = fixture.CommandPlayerOn(map, 2, 3);

        fixture.RunCommand(player, "M1");

        Assert.Equal(3, player.MapY);
        Assert.Contains(player.Sent, s => s.StartsWith("SUP"));
    }

    [Fact]
    public void LoginContinued_SavedMapMissing_FallsBackToStartingMap()
    {
        using var fixture = new TestWorldFixture(s => { s.StartingMapID = 1; s.MOTD = ""; });
        fixture.AddBaseMap(1, "start");
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(2, "other"), 1, 1);
        player.Spellbook = new Spellbook(player, fixture.Settings);
        player.State = Player.States.LoadingGame;
        player.MapID = 999;

        fixture.RunCommand(player, "LCNT");

        Assert.Equal(1, player.MapID);
        Assert.Equal(Player.States.LoadingMap, player.State);
        Assert.Contains(player.Sent, s => s.StartsWith("SCMMap1.map"));
    }
}
