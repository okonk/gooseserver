using System.Net;
using System.Net.Sockets;
using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class SetConfigCommandTests : IDisposable
{
    private readonly TestWorldFixture fixtureA;
    private readonly TestWorldFixture fixtureB;
    private readonly List<Socket> gmSockets = new();

    public SetConfigCommandTests()
    {
        fixtureA = new TestWorldFixture(s => s.IdleTimeout = 10);
        fixtureB = new TestWorldFixture(s => s.IdleTimeout = 20);
    }

    public void Dispose()
    {
        foreach (var sock in gmSockets)
            sock.Dispose();
        fixtureA.Dispose();
        fixtureB.Dispose();
    }

    private static TestWorldFixture.CapturingPlayer GM(TestWorldFixture fixture, string name)
    {
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "Town"), 1, 1, name);
        player.Access = Player.AccessStatus.GameMaster;
        return player;
    }

    private TestWorldFixture.CapturingPlayer RegisterGM(TestWorldFixture fixture, string name)
    {
        var player = GM(fixture, name);
        player.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        gmSockets.Add(player.Sock);
        fixture.World.PlayerHandler.AddPlayer(player, fixture.World);
        return player;
    }

    [Fact]
    public void NumericChange_MutatesOnlyTheSuppliedWorld()
    {
        var gmA = RegisterGM(fixtureA, "GM-A");
        var gmB = RegisterGM(fixtureB, "GM-B");

        Assert.True(fixtureA.RunCommand(gmA, "/setconfig IdleTimeout 55"));

        Assert.Equal(55, fixtureA.Settings.IdleTimeout);
        Assert.Equal(20, fixtureB.Settings.IdleTimeout);
        Assert.Contains(gmA.Sent, m => m.Contains("[GM] Set Game Setting IdleTimeout to: 55"));
    }

    [Fact]
    public void StringChange_MutatesOnlyTheSuppliedWorld()
    {
        var gmA = RegisterGM(fixtureA, "GM-A");
        var gmB = RegisterGM(fixtureB, "GM-B");

        Assert.True(fixtureA.RunCommand(gmA, "/setconfig ServerName AlphaServer"));

        Assert.Equal("AlphaServer", fixtureA.Settings.ServerName);
        Assert.Null(fixtureB.Settings.ServerName);
        Assert.Contains(gmA.Sent, m => m.Contains("[GM] Set Game Setting ServerName to: AlphaServer"));
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
        var gmA = RegisterGM(fixtureA, "GM-A");

        Assert.True(fixtureA.RunCommand(gmA, "/setconfig IdleTimeout nope"));

        Assert.Equal(10, fixtureA.Settings.IdleTimeout);
        Assert.Contains(gmA.Sent, m => m.Contains("Couldn't set value 'nope' for IdleTimeout."));
        Assert.DoesNotContain(gmA.Sent, m => m.Contains("[GM] Set Game Setting"));
    }

    [Fact]
    public void MissingValue_SendsUsage()
    {
        var gmA = RegisterGM(fixtureA, "GM-A");

        Assert.False(fixtureA.RunCommand(gmA, "/setconfig"));
        Assert.True(fixtureA.RunCommand(gmA, "/setconfig "));

        Assert.Equal(10, fixtureA.Settings.IdleTimeout);
        Assert.Contains(gmA.Sent, m => m.Contains("Usage: /setconfig <setting> <value...>"));
    }
}
