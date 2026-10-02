using Goose.Testing;

namespace Goose.Tests;

public class SurnameCommandTests
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
    public void Surname_lists_unlocked_options_alphabetically_with_clear_last()
    {
        using var fixture = Setup(out _, out var player);
        player.GrantSurname("Zeta", fixture.World);
        player.GrantSurname("alpha", fixture.World);
        player.Sent.Clear();

        Assert.True(fixture.RunCommand(player, "/surname"));

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
        player.GrantSurname("Lord", fixture.World);
        player.GrantSurname("Duke", fixture.World);
        player.SetSurname("Lord", fixture.World);
        player.Sent.Clear();
        other.Sent.Clear();

        Assert.True(fixture.RunCommand(player, "/surname"));
        var window = Assert.IsType<OptionListWindow>(player.Windows[0]);
        window.LineClicked(0, 0, player, fixture.World);

        Assert.Equal("Duke", player.Surname);
        Assert.Contains(player.Sent, s => s.StartsWith("ERC" + player.LoginID));
        Assert.Contains(player.Sent, s => s.StartsWith("MKC" + player.LoginID) && s.Contains("Duke"));
        Assert.Contains(other.Sent, s => s.StartsWith("MKC" + player.LoginID) && s.Contains("Duke"));
        Assert.Equal(new List<string> { "Lord", "Duke" }, player.UnlockedSurnames());
    }

    [Fact]
    public void Clear_empties_the_display_and_keeps_everything_unlocked()
    {
        using var fixture = Setup(out _, out var player);
        player.GrantSurname("Lord", fixture.World);

        Assert.True(fixture.RunCommand(player, "/surname"));
        var window = Assert.IsType<OptionListWindow>(player.Windows[0]);
        window.LineClicked(1, 0, player, fixture.World);

        Assert.Equal("", player.Surname);
        Assert.Equal(new List<string> { "Lord" }, player.UnlockedSurnames());
    }

    [Fact]
    public void The_surname_picker_lists_surnames_only()
    {
        using var fixture = Setup(out _, out var player);
        player.GrantTitle("Lord", fixture.World);
        player.GrantSurname("Smith", fixture.World);
        player.Sent.Clear();

        Assert.True(fixture.RunCommand(player, "/surname"));

        var joined = string.Join("|", player.Sent.Where(s => s.StartsWith("WNF")));
        Assert.Contains("Smith (current)", joined);
        Assert.Contains("Clear", joined);
        Assert.DoesNotContain("Lord", joined);
    }
}
