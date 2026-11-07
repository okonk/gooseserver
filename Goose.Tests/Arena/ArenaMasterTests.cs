using Goose;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Arena;

public class ArenaMasterTests : IDisposable
{
    private readonly ArenaTestKit kit;

    public ArenaMasterTests()
    {
        kit = new ArenaTestKit();
        var script = kit.Compile<INPCScript>("Arena", "ArenaMaster.csx");
        var template = kit.Fixture.AddNpcTemplate(9001, "Arena Master", t => t.Script = script);
        kit.Fixture.SpawnNpc(kit.LobbyMap, 5, 5, template);
    }

    public void Dispose() => kit.Dispose();

    private OptionListWindow ClickNpc(TestWorldFixture.CapturingPlayer player)
    {
        kit.Fixture.RunCommand(player, "LC5,5");
        return (OptionListWindow)player.Windows[^1];
    }

    private void ClickLine(TestWorldFixture.CapturingPlayer player, int windowId, int line)
        => kit.Fixture.RunCommand(player, $"WBC{20 + line},{windowId},0,0,0");

    private List<string> TextLines(TestWorldFixture.CapturingPlayer player, int windowId)
        => player.Sent.Where(s => s.StartsWith($"WNF{windowId},"))
            .Select(s =>
            {
                var rest = s.Substring($"WNF{windowId},".Length);
                return rest[(rest.IndexOf(',') + 1)..].Split('|')[0];
            })
            .ToList();

    [Fact]
    public void Click_opens_hub_with_balance_and_next_run()
    {
        var player = kit.LobbyPlayer(1, "Alice");
        kit.World.CurrencyHandler.Get("arena_coin")!.Add(player, 1240, kit.World);
        var hub = ClickNpc(player);
        Assert.Contains(player.Sent, s => s.StartsWith($"MKW{hub.ID},27,Arena,"));
        var lines = TextLines(player, hub.ID);
        Assert.Equal("Arena Coins: 1,240", lines[0]);
        var cfg = ArenaTestKit.Call(kit.Host, "Config", kit.World)!;
        var interval = TimeSpan.FromHours((int)(double)ArenaTestKit.GetProp(cfg, "IntervalHours")!);
        var expected = new HashSet<string>();
        foreach (var offset in new[] { -65, 0, 65 })
        {
            var now = DateTime.UtcNow.AddSeconds(offset);
            var next = (DateTime)ArenaTestKit.Call(kit.Host, "NextStartUtc", now, cfg)!;
            foreach (var start in new[] { next, next + interval })
            {
                var ts = start - now;
                expected.Add(ts.TotalSeconds < 60
                    ? "Next run in under a minute"
                    : $"Next run in {(int)ts.TotalHours}h {ts.Minutes}m");
            }
        }
        Assert.Contains(lines[1], expected);
        Assert.Equal("Leaderboards", lines[^1]);
        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public void Hub_shows_run_in_progress()
    {
        kit.LobbyPlayer(2, "A");
        kit.LobbyPlayer(3, "B");
        var gm = kit.LobbyPlayer(4, "GM", gm: true);
        ArenaTestKit.Call(kit.Host, "StartRun", kit.World, DateTime.UtcNow);
        var hub = ClickNpc(gm);
        var lines = TextLines(gm, hub.ID);
        Assert.Equal("Run in progress: round 0, 2 alive", lines[1]);
    }

    [Fact]
    public void Disabled_config_with_active_run_shows_in_progress()
    {
        kit.LobbyPlayer(2, "A");
        kit.LobbyPlayer(3, "B");
        var gm = kit.LobbyPlayer(4, "GM", gm: true);
        ArenaTestKit.Call(kit.Host, "StartRun", kit.World, DateTime.UtcNow);
        kit.SetConfig(json => json["lobbyMap"] = 0);
        var hub = ClickNpc(gm);
        var lines = TextLines(gm, hub.ID);
        Assert.Equal("Run in progress: round 0, 2 alive", lines[1]);
    }

    [Fact]
    public void Disabled_config_shows_closed()
    {
        kit.SetConfig(json => json["lobbyMap"] = 0);
        var player = kit.LobbyPlayer(1, "Alice");
        var hub = ClickNpc(player);
        var lines = TextLines(player, hub.ID);
        Assert.Equal("The arena is closed.", lines[1]);
    }

    [Fact]
    public void Leaderboard_navigation_and_back()
    {
        var player = kit.LobbyPlayer(1, "Alice");
        var hub = ClickNpc(player);
        var hubLines = TextLines(player, hub.ID);
        ClickLine(player, hub.ID, hubLines.Count - 1);
        var menu = (OptionListWindow)player.Windows[^1];
        Assert.Contains(player.Sent, s => s.StartsWith($"MKW{menu.ID},27,Leaderboards,"));
        Assert.Equal(new[]
        {
            "Best round - this week", "Best round - all time",
            "Total kills - this week", "Total kills - all time",
        }, TextLines(player, menu.ID).ToArray());
        kit.Fixture.RunCommand(player, $"WBC3,{menu.ID},0,0,0");
        var hub2 = (OptionListWindow)player.Windows[^1];
        Assert.Contains(player.Sent, s => s.StartsWith($"MKW{hub2.ID},27,Arena,"));
    }

    [Fact]
    public void Hub_shows_last_run()
    {
        kit.LobbyPlayer(2, "A");
        kit.LobbyPlayer(3, "B");
        var gm = kit.LobbyPlayer(4, "GM", gm: true);
        ArenaTestKit.Call(kit.Host, "StartRun", kit.World, DateTime.UtcNow);
        var run = ArenaTestKit.Call(kit.Host, "Run", kit.World)!;
        var participants = (System.Collections.IDictionary)ArenaTestKit.GetProp(run, "Participants")!;
        ArenaTestKit.SetProp(participants[2]!, "RoundsCleared", 7);
        ArenaTestKit.SetProp(participants[3]!, "RoundsCleared", 3);
        ArenaTestKit.Call(kit.Host, "SaveRun", kit.World, run);
        ArenaTestKit.Call(kit.Host, "EndRun", kit.World, "test");
        var hub = ClickNpc(gm);
        var lines = TextLines(gm, hub.ID);
        Assert.Equal("Last run: round 7 (2 players)", lines[2]);
        Assert.Equal("Leaderboards", lines[^1]);
    }

    [Fact]
    public void Empty_board_shows_no_entries()
    {
        var player = kit.LobbyPlayer(1, "Alice");
        var hub = ClickNpc(player);
        var hubLines = TextLines(player, hub.ID);
        ClickLine(player, hub.ID, hubLines.Count - 1);
        var menu = (OptionListWindow)player.Windows[^1];
        ClickLine(player, menu.ID, 0);
        var board = (OptionListWindow)player.Windows[^1];
        Assert.Equal(new[] { "No entries yet." }, TextLines(player, board.ID).ToArray());
        kit.Fixture.RunCommand(player, $"WBC3,{board.ID},0,0,0");
        var menu2 = (OptionListWindow)player.Windows[^1];
        Assert.Contains(player.Sent, s => s.StartsWith($"MKW{menu2.ID},27,Leaderboards,"));
    }

    [Fact]
    public void Board_shows_top_ten_and_own_rank()
    {
        var player = kit.LobbyPlayer(103, "Alice");
        kit.Fixture.RegisterDatabasePlayer(player);
        SeedKills(12);
        var hub = ClickNpc(player);
        var hubLines = TextLines(player, hub.ID);
        ClickLine(player, hub.ID, hubLines.Count - 1);
        var menu = (OptionListWindow)player.Windows[^1];
        ClickLine(player, menu.ID, 3);
        var board = (OptionListWindow)player.Windows[^1];
        Assert.Contains(player.Sent, s => s.StartsWith($"MKW{board.ID},27,Leaderboards,"));
        var page1 = TextLines(player, board.ID);
        Assert.Equal(new[]
        {
            "1. Unknown - 12", "2. Unknown - 11", "3. Alice - 10", "4. Unknown - 9",
            "5. Unknown - 8", "6. Unknown - 7", "7. Unknown - 6", "8. Unknown - 5",
        }, page1.ToArray());
        var before = player.Sent.Count;
        kit.Fixture.RunCommand(player, $"WBC4,{board.ID},0,0,0");
        var page2 = player.Sent.Skip(before)
            .Where(s => s.StartsWith($"WNF{board.ID},"))
            .Select(s =>
            {
                var rest = s.Substring($"WNF{board.ID},".Length);
                return rest[(rest.IndexOf(',') + 1)..].Split('|')[0];
            })
            .ToList();
        Assert.Equal(new[] { "9. Unknown - 4", "10. Unknown - 3", "", "You: #3 - 10" }, page2.ToArray());
    }

    [Fact]
    public void Unranked_player_sees_unranked()
    {
        var player = kit.LobbyPlayer(500, "Nobody");
        SeedKills(1);
        var hub = ClickNpc(player);
        var hubLines = TextLines(player, hub.ID);
        ClickLine(player, hub.ID, hubLines.Count - 1);
        var menu = (OptionListWindow)player.Windows[^1];
        ClickLine(player, menu.ID, 3);
        var board = (OptionListWindow)player.Windows[^1];
        Assert.Equal(new[] { "1. Unknown - 12", "", "You: unranked" }, TextLines(player, board.ID).ToArray());
    }

    private void SeedKills(int count)
    {
        var week = (string)ArenaTestKit.Call(kit.Host, "WeekKey", DateTime.UtcNow)!;
        var board = ArenaTestKit.Call(kit.Host, "NewBoard")!;
        var entries = (System.Collections.IDictionary)ArenaTestKit.GetProp(board, "Players")!;
        for (var i = 0; i < count; i++)
        {
            var entry = ArenaTestKit.Call(kit.Host, "NewEntry")!;
            ArenaTestKit.SetProp(entry, "Id", 101 + i);
            ArenaTestKit.SetProp(entry, "Kills", 12 - i);
            ArenaTestKit.SetProp(entry, "Week", week);
            ArenaTestKit.SetProp(entry, "WeekKills", 12 - i);
            entries[101 + i] = entry;
        }
        ArenaTestKit.Call(kit.Host, "BoardSave", kit.World, board);
    }
}
