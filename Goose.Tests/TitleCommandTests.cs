using Goose.Testing;

namespace Goose.Tests;

public class TitleCommandTests
{
    private static TestWorldFixture Setup(out Map map, out TestWorldFixture.CapturingPlayer player)
    {
        var fixture = new TestWorldFixture();
        map = fixture.AddBaseMap(1, "m", 60, 40);
        player = fixture.CommandPlayerOn(map, 5, 5, "Tester");
        map.AddPlayer(player, fixture.World);
        return fixture;
    }

    [Fact]
    public void Title_lists_unlocked_options_alphabetically_with_clear_last()
    {
        using var fixture = Setup(out _, out var player);
        player.GrantTitle("Zeta", fixture.World);
        player.GrantTitle("alpha", fixture.World);
        player.Sent.Clear();

        Assert.True(fixture.RunCommand(player, "/title"));

        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToList();
        Assert.Contains("alpha (current)|0|0|0|0|*", lines[0]);
        Assert.Contains("Zeta|0|0|0|0|*", lines[1]);
        Assert.Contains("Clear|0|0|0|0|*", lines[2]);
    }

    [Fact]
    public void Picking_a_line_equips_it_and_republishes_the_character_to_self_and_range()
    {
        using var fixture = Setup(out var map, out var player);
        var other = fixture.CommandPlayerOn(map, 6, 5, "Other");
        map.AddPlayer(other, fixture.World);
        player.GrantTitle("Lord", fixture.World);
        player.GrantTitle("Duke", fixture.World);
        player.SetTitle("Lord", fixture.World);
        player.Sent.Clear();
        other.Sent.Clear();

        Assert.True(fixture.RunCommand(player, "/title"));
        var window = Assert.IsType<OptionListWindow>(player.Windows[0]);
        window.LineClicked(0, 0, player, fixture.World);

        Assert.Equal("Duke", player.Title);
        Assert.Contains(player.Sent, s => s.StartsWith("ERC" + player.LoginID));
        Assert.Contains(player.Sent, s => s.StartsWith("MKC" + player.LoginID) && s.Contains("Duke"));
        Assert.Contains(other.Sent, s => s.StartsWith("MKC" + player.LoginID) && s.Contains("Duke"));
        Assert.Equal(new List<string> { "Lord", "Duke" }, player.UnlockedTitles());
    }

    [Fact]
    public void Clear_empties_the_display_and_keeps_everything_unlocked()
    {
        using var fixture = Setup(out _, out var player);
        player.GrantTitle("Lord", fixture.World);

        Assert.True(fixture.RunCommand(player, "/title"));
        var window = Assert.IsType<OptionListWindow>(player.Windows[0]);
        window.LineClicked(1, 0, player, fixture.World);

        Assert.Equal("", player.Title);
        Assert.Equal(new List<string> { "Lord" }, player.UnlockedTitles());
    }

    [Fact]
    public void Opening_the_picker_twice_does_not_stack_windows()
    {
        using var fixture = Setup(out _, out var player);

        fixture.RunCommand(player, "/title");
        fixture.RunCommand(player, "/title");

        Assert.Single(player.Windows);
    }

    [Fact]
    public void An_empty_collection_still_offers_clear()
    {
        using var fixture = Setup(out _, out var player);
        player.Sent.Clear();

        fixture.RunCommand(player, "/title");

        var lines = player.Sent.Where(s => s.StartsWith("WNF")).ToList();
        Assert.Single(lines);
        Assert.Contains("Clear", lines[0]);
    }

    [Fact]
    public void More_than_eight_titles_pages_and_keeps_clear_on_the_last_page()
    {
        using var fixture = Setup(out _, out var player);
        for (var i = 0; i < 9; i++)
            player.GrantTitle("T" + i, fixture.World);
        player.Sent.Clear();

        fixture.RunCommand(player, "/title");

        Assert.DoesNotContain("Clear", string.Join("|", player.Sent.Where(s => s.StartsWith("WNF"))));
        var window = Assert.IsType<OptionListWindow>(player.Windows[0]);
        window.Clicked(Window.ButtonTypes.Next, 0, 0, 0, player, fixture.World);
        Assert.Contains(player.Sent.Where(s => s.StartsWith("WNF")).Skip(8), s => s.Contains("Clear"));
    }
}
