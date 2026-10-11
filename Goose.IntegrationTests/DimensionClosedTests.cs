using Goose;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests;

public class DimensionClosedTests
{
    private const int Offset = 100000;
    private const int StartMapId = 1;

    private static (GlobalScriptFixture Fixture, GlobalScriptFixture.CapturingPlayer Player) Loaded(int mapId)
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(StartMapId, "Town", width: 100, height: 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var player = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(mapId)!, 5, 5);
        player.Properties["dimension.max"] = 6;
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3)!;
        player.Level = 50;
        player.Experience = 200_000_000_000L;
        player.Sent.Clear();

        return (fixture, player);
    }

    [Fact]
    public void Dimensions_above_zero_are_refused_while_closed()
    {
        var (fixture, player) = Loaded(StartMapId);
        using var _ = fixture;

        Assert.True(fixture.RunCommand(player, "/dimension 1"));

        Assert.Equal(StartMapId, player.MapID);
        Assert.Contains(player.Sent, m => m.Contains("The dimensions are closed for now."));
    }

    [Fact]
    public void Dimension_zero_still_works_while_closed()
    {
        var (fixture, player) = Loaded(StartMapId + Offset * 2);
        using var _ = fixture;

        Assert.True(fixture.RunCommand(player, "/dimension 0"));

        Assert.Equal(StartMapId, player.MapID);
    }

    [Fact]
    public void Game_masters_can_still_enter_while_closed()
    {
        var (fixture, player) = Loaded(StartMapId);
        using var _ = fixture;
        player.Access = Player.AccessStatus.GameMaster;

        Assert.True(fixture.RunCommand(player, "/dimension 1"));

        Assert.Equal(StartMapId + Offset, player.MapID);
    }

    [Fact]
    public void Opening_the_dimensions_in_world_state_lets_players_in()
    {
        var (fixture, player) = Loaded(StartMapId);
        using var _ = fixture;
        fixture.World.WorldState.Set("dimensions.open", true);

        Assert.True(fixture.RunCommand(player, "/dimension 1"));

        Assert.Equal(StartMapId + Offset, player.MapID);
    }
}
