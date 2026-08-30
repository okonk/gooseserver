using Goose;
using Goose.Commands;
using Goose.Testing;
using Xunit;

namespace Goose.IntegrationTests;

public class CommandFrameworkTests
{
    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, Map Map) WorldAndPlayer()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Test");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        player.Access = Player.AccessStatus.Normal;
        return (fixture, player, map);
    }

    [Fact]
    public void Help_opens_a_window_on_the_player()
    {
        var (fixture, player, _) = WorldAndPlayer();
        using var _ = fixture;

        Assert.True(fixture.RunCommand(player, "/help"));

        Assert.Contains(player.Sent, m => m.StartsWith("MKW"));
        Assert.Contains(player.Sent, m => m.StartsWith("ENW"));
        Assert.Contains(player.Windows, w => w is HelpWindow);
    }

    [Fact]
    public void Help_hides_privileged_sections_from_normal_players()
    {
        var (fixture, normal, map) = WorldAndPlayer();
        using var _ = fixture;
        var gm = fixture.CommandPlayerOn(map, 2, 2, name: "GM");
        gm.Access = Player.AccessStatus.GameMaster;

        Assert.True(fixture.World.Commands.Register("/itestgm ", AccessPrivilege.Ban, "Admin", "Test.",
            (CommandContext ctx) => ctx.Send("gm ok")));

        Assert.True(fixture.RunCommand(normal, "/help"));
        Assert.Contains(normal.Sent, m => m.Contains("General (1)"));
        Assert.DoesNotContain(normal.Sent, m => m.Contains("Admin"));
        Assert.DoesNotContain(normal.Sent, m => m.Contains("/itestgm"));

        Assert.True(fixture.RunCommand(gm, "/help"));
        Assert.Contains(gm.Sent, m => m.Contains("General (1)"));
        Assert.Contains(gm.Sent, m => m.Contains("Admin (1)"));
    }

    [Fact]
    public void Open_script_registration_runs_through_the_queue()
    {
        var (fixture, player, _) = WorldAndPlayer();
        using var _ = fixture;

        Assert.True(fixture.World.Commands.Register("/itestcmd ", "General", "Test.",
            (CommandContext ctx, int n) => ctx.Send("got " + n)));

        Assert.True(fixture.RunCommand(player, "/itestcmd 7"));
        Assert.Contains(player.Sent, m => m.Contains("got 7"));
    }

    [Fact]
    public void Re_registering_a_key_replaces_the_handler()
    {
        var (fixture, player, _) = WorldAndPlayer();
        using var _ = fixture;

        Assert.True(fixture.World.Commands.Register("/itestcmd", "Test.", "First.",
            (CommandContext ctx) => ctx.Send("old")));
        Assert.True(fixture.RunCommand(player, "/itestcmd"));
        Assert.Contains(player.Sent, m => m.Contains("old"));
        player.Sent.Clear();

        Assert.True(fixture.World.Commands.Register("/itestcmd", "Test.", "Second.",
            (CommandContext ctx) => ctx.Send("new")));
        Assert.True(fixture.RunCommand(player, "/itestcmd"));
        Assert.Contains(player.Sent, m => m.Contains("new"));
    }

    [Fact]
    public async Task Registering_from_a_background_thread_while_commands_run_is_safe()
    {
        var (fixture, player, _) = WorldAndPlayer();
        using var _ = fixture;

        Assert.True(fixture.World.Commands.Register("/itestconc ", "Test.", "Conc.",
            (CommandContext ctx) => ctx.Send("conc ok")));

        var exceptions = new List<Exception>();
        var registerTask = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 100; i++)
                    fixture.World.Commands.Register("/itestconc ", "Test.", "Conc.",
                        (CommandContext ctx) => ctx.Send("conc ok"));
            }
            catch (Exception e)
            {
                lock (exceptions) exceptions.Add(e);
            }
        });

        for (var i = 0; i < 100; i++)
            Assert.True(fixture.RunCommand(player, "/itestconc "));

        await registerTask;
        lock (exceptions) Assert.Empty(exceptions);

        player.Sent.Clear();
        Assert.True(fixture.RunCommand(player, "/itestconc "));
        Assert.Contains(player.Sent, m => m.Contains("conc ok"));
    }

    [Fact]
    public void Legacy_commands_behave_as_before()
    {
        var (fixture, player, map) = WorldAndPlayer();
        using var _ = fixture;
        map.AddPlayer(player, fixture.World);

        Assert.True(fixture.RunCommand(player, "/who"));
        Assert.Contains(player.Sent, m => m.StartsWith("#") && m.Contains("Tester"));
        Assert.Contains(player.Sent, m => m.Contains("[Matched 1 players]"));

        player.Sent.Clear();
        Assert.True(fixture.RunCommand(player, "/ban x"));
        Assert.Empty(player.Sent);
    }
}
