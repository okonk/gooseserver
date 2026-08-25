using Goose;
using Goose.Testing;
using Goose.Tests.Collections;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class SetConfigCommandTests : IDisposable
{
    private readonly TestWorldFixture fixtureA;
    private readonly TestWorldFixture fixtureB;
    private readonly GooseSettings previousSettings;

    public SetConfigCommandTests()
    {
        previousSettings = GameWorld.Settings;
        fixtureA = new TestWorldFixture(s => s.IdleTimeout = 10);
        fixtureB = new TestWorldFixture(s => s.IdleTimeout = 20);
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
        fixtureA.Dispose();
        fixtureB.Dispose();
    }

    private static TestWorldFixture.CapturingPlayer GM(TestWorldFixture fixture, string name)
    {
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "Town"), 1, 1, name);
        player.Access = Player.AccessStatus.GameMaster;
        return player;
    }

    [Fact]
    public void NumericChange_MutatesOnlyTheSuppliedWorld()
    {
        var gmA = GM(fixtureA, "GM-A");
        var gmB = GM(fixtureB, "GM-B");

        Assert.True(fixtureA.RunCommand(gmA, "/setconfig IdleTimeout 55"));

        Assert.Equal(55, fixtureA.Settings.IdleTimeout);
        Assert.Equal(20, fixtureB.Settings.IdleTimeout);
        Assert.NotEqual(55, GameWorld.Settings.IdleTimeout);
    }

    [Fact]
    public void StringChange_MutatesOnlyTheSuppliedWorld()
    {
        var gmA = GM(fixtureA, "GM-A");
        var gmB = GM(fixtureB, "GM-B");

        Assert.True(fixtureA.RunCommand(gmA, "/setconfig ServerName AlphaServer"));

        Assert.Equal("AlphaServer", fixtureA.Settings.ServerName);
        Assert.Null(fixtureB.Settings.ServerName);
    }

    [Fact]
    public void UnknownProperty_LeavesBothWorldsUnchangedAndReportsTheError()
    {
        var gmA = GM(fixtureA, "GM-A");

        Assert.True(fixtureA.RunCommand(gmA, "/setconfig NotASetting 1"));

        Assert.Equal(10, fixtureA.Settings.IdleTimeout);
        Assert.Equal(20, fixtureB.Settings.IdleTimeout);
        Assert.Contains(gmA.Sent, m => m.Contains("Couldn't find Game Setting: NotASetting."));
    }

    [Fact]
    public void UnparsableValue_LeavesTheTargetSettingUnchanged()
    {
        var gmA = GM(fixtureA, "GM-A");

        Assert.True(fixtureA.RunCommand(gmA, "/setconfig IdleTimeout nope"));

        Assert.Equal(10, fixtureA.Settings.IdleTimeout);
    }
}
