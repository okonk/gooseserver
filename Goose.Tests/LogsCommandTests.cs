using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class LogsCommandTests
{
    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, CommandContext Ctx) Setup(
        Player.AccessStatus access = Player.AccessStatus.GameMaster,
        Player.States state = Player.States.Ready)
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        player.Access = access;
        player.State = state;
        var ctx = new CommandContext(player, fixture.World, new CommandRegistry(), [], "");
        return (fixture, player, ctx);
    }

    [Fact]
    public void ViewLogs_IsAppendedWithoutRenumbering_AndGmOnly()
    {
        Assert.Equal(0, (int)AccessPrivilege.IgnoreMapRequirements);
        Assert.Equal(33, (int)AccessPrivilege.Shutdown);
        Assert.Equal(34, (int)AccessPrivilege.Debug);
        Assert.Equal(35, (int)AccessPrivilege.ViewLogs);
        Assert.Equal(36, Enum.GetValues<AccessPrivilege>().Length);

        foreach (Player.AccessStatus access in Enum.GetValues<Player.AccessStatus>())
        {
            var player = new Player(0) { Access = access };
            bool expected = access == Player.AccessStatus.GameMaster;
            Assert.Equal(expected, AccessLevels.HasPrivilege(player, AccessPrivilege.ViewLogs));
        }
    }

    [Theory]
    [InlineData(Player.AccessStatus.Normal)]
    [InlineData(Player.AccessStatus.Guide)]
    [InlineData(Player.AccessStatus.Helper)]
    [InlineData(Player.AccessStatus.EventMaster)]
    public void Unprivileged_Logs_IsSwallowedWithNoWindowOrPackets(Player.AccessStatus access)
    {
        var (fixture, player, _) = Setup(access);

        Assert.True(fixture.RunCommand(player, "/logs"));

        Assert.Empty(player.Windows);
        Assert.Empty(player.Sent);
    }

    [Fact]
    public void ReadyGm_Logs_OpensExactlyOneCloseOnlyFrame29Viewer()
    {
        var (fixture, player, _) = Setup();

        Assert.True(fixture.RunCommand(player, "/logs"));

        Assert.Single(player.Windows);
        var window = player.Windows[0];
        Assert.IsType<LogViewerWindow>(window);
        Assert.Equal(Window.WindowFrames.LogViewer, window.Frame);
        Assert.Equal(Window.WindowTypes.LogViewer, window.Type);
        Assert.Equal("GM Log Viewer", window.Title);
        Assert.Equal("0,1,0,0,0", window.Buttons);
        Assert.Contains(player.Sent, s => s.StartsWith($"MKW{window.ID},29,GM Log Viewer,"));

        Assert.True(fixture.RunCommand(player, "/logs"));
        Assert.Single(player.Windows);
    }

    [Fact]
    public void NotReadyGm_DirectExecute_OpensNothing()
    {
        var (_, player, ctx) = Setup(state: Player.States.LoadingMap);

        new LogsCommand().Execute(ctx);

        Assert.Empty(player.Windows);
        Assert.Empty(player.Sent);
    }

    [Fact]
    public void DispatchThenRevoke_BeforeReady_OpensNothing()
    {
        var (fixture, player, _) = Setup();

        Assert.True(fixture.World.EventHandler.AddEvent(player, "/logs"));
        player.Access = Player.AccessStatus.Normal;
        fixture.World.EventHandler.Update(fixture.World);

        Assert.Empty(player.Windows);
        Assert.Empty(player.Sent);
    }

    [Fact]
    public void DirectOpen_RefusesNotReadyOrUnauthorizedPlayers()
    {
        var (fixture, player, _) = Setup(state: Player.States.LoadingMap);
        Assert.False(LogViewerWindow.Open(player, fixture.World));
        Assert.Empty(player.Windows);
        Assert.Empty(player.Sent);

        var (fixture2, player2, _) = Setup(access: Player.AccessStatus.Guide);
        Assert.False(LogViewerWindow.Open(player2, fixture2.World));
        Assert.Empty(player2.Windows);
        Assert.Empty(player2.Sent);
    }
}
