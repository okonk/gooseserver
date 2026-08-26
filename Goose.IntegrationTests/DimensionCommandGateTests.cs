using Goose;
using Goose.IntegrationTests.Fixtures;
using Xunit;

namespace Goose.IntegrationTests;

public class DimensionCommandGateTests
{
    private const int Offset = 100000;
    private const int StartMapId = 1;

    private static (GlobalScriptFixture Fixture, GlobalScriptFixture.CapturingPlayer Player) Loaded()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(StartMapId, "Town", width: 100, height: 100);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var player = fixture.CommandPlayerOn(fixture.World.MapHandler.GetMap(StartMapId)!, 5, 5);
        player.Properties["dimension.max"] = 6;
        player.ClassID = 3;
        player.Class = fixture.World.ClassHandler.GetClass(3)!;
        player.Level = 50;
        player.Sent.Clear();

        return (fixture, player);
    }

    /// <summary>The baseline: a player who clears the gate still gets there.</summary>
    [Fact]
    public void Warps_when_the_gate_is_clear()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Experience = 200_000_000_000L;

        Assert.True(fixture.RunCommand(player, "/dimension 5"));

        Assert.Equal(StartMapId + Offset * 5, player.MapID);
    }

    /// <summary>The bug. Player.WarpTo (Player.cs:1234) never calls Map.PlayerCanJoin, so
    /// before the fix the command warps a level-1 rebirthed character straight into
    /// dimension 5 past a 100,000,000,000 floor.</summary>
    [Fact]
    public void Refuses_below_the_minimum_experience_and_does_not_warp()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Experience = 0;
        player.ExperienceSold = 0;

        fixture.RunCommand(player, "/dimension 5");

        Assert.Equal(StartMapId, player.MapID);
        Assert.Contains(player.Sent, m => m.Contains("experience to enter this map"));
    }

    /// <summary>The other end. Map.PlayerCanJoin gates MaxExperience too (Map.cs:644), and
    /// /dimension must respect it for the same reason.</summary>
    [Fact]
    public void Refuses_above_the_maximum_experience_and_does_not_warp()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        fixture.World.MapHandler.GetMap(StartMapId + Offset * 2)!.MaxExperience = 1_000;
        player.Experience = 500_000;

        fixture.RunCommand(player, "/dimension 2");

        Assert.Equal(StartMapId, player.MapID);
        Assert.Contains(player.Sent, m => m.Contains("at most"));
    }

    /// <summary>The gate is the map's, so DimensionMap.csx's own refusal reaches the
    /// command too — one gate, not two implementations that can drift.</summary>
    [Fact]
    public void Refuses_a_dimension_above_the_players_unlock_and_does_not_warp()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Properties["dimension.max"] = 2;
        player.Experience = 900_000_000_000L;

        fixture.RunCommand(player, "/dimension 5");

        Assert.Equal(StartMapId, player.MapID);
    }

    /// <summary>Dimension 0 is home. It must stay reachable no matter how the floors are
    /// configured — otherwise a rebirthed character standing in dimension 6 has no way
    /// back.</summary>
    [Fact]
    public void Dimension_zero_stays_reachable_with_no_experience()
    {
        var (fixture, player) = Loaded();
        using var _ = fixture;
        player.Experience = 800_000_000_000L;
        fixture.RunCommand(player, "/dimension 6");
        Assert.Equal(StartMapId + Offset * 6, player.MapID);

        // A cross-map WarpTo leaves the player in LoadingMap with Map == null until the
        // client's DLM ack (Player.cs:1311, DoneLoadingMapEvent). Simulate that ack so the
        // second command sees the player as the server would after the map load.
        player.Map = fixture.World.MapHandler.GetMap(player.MapID)!;
        player.State = Player.States.Ready;

        player.Experience = 0;
        player.ExperienceSold = 0;

        Assert.True(fixture.RunCommand(player, "/dimension 0"));

        Assert.Equal(StartMapId, player.MapID);
    }
}
