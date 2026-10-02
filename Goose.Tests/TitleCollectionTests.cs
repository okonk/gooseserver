using Goose.Testing;

namespace Goose.Tests;

public class TitleCollectionTests : IDisposable
{
    private readonly TestWorldFixture fixture = new();
    private GameWorld World => this.fixture.World;

    public void Dispose() => this.fixture.Dispose();

    [Fact]
    public void Granting_a_title_unlocks_and_equips_it()
    {
        var player = new Player(0);

        player.GrantTitle("Lord of the Vast", this.World);

        Assert.Equal("Lord of the Vast", player.Title);
        Assert.Equal(new List<string> { "Lord of the Vast" }, player.UnlockedTitles());
    }

    [Fact]
    public void Equipping_a_title_does_not_unlock_it()
    {
        var player = new Player(0);

        player.SetTitle("Temporary", this.World);

        Assert.Equal("Temporary", player.Title);
        Assert.Empty(player.UnlockedTitles());
    }

    [Fact]
    public void Granting_a_different_casing_of_a_stored_title_adds_no_entry()
    {
        var player = new Player(0);

        player.GrantTitle("Lord", this.World);
        player.GrantTitle("  LORD  ", this.World);

        Assert.Equal(new List<string> { "Lord" }, player.UnlockedTitles());
        Assert.Equal("LORD", player.Title);
    }

    [Fact]
    public void Granting_blank_unlocks_nothing_and_clears_the_display()
    {
        var player = new Player(0);
        player.GrantTitle("Lord", this.World);

        player.GrantTitle("   ", this.World);

        Assert.Equal(new List<string> { "Lord" }, player.UnlockedTitles());
        Assert.Equal("", player.Title);
    }

    [Fact]
    public void Grants_keep_insertion_order_and_surnames_are_a_separate_list()
    {
        var player = new Player(0);

        player.GrantTitle("Zeta", this.World);
        player.GrantTitle("Alpha", this.World);
        player.GrantSurname("Smith", this.World);

        Assert.Equal(new List<string> { "Zeta", "Alpha" }, player.UnlockedTitles());
        Assert.Equal(new List<string> { "Smith" }, player.UnlockedSurnames());
    }

    [Fact]
    public void A_granted_title_survives_a_properties_round_trip()
    {
        var player = new Player(0);
        player.GrantTitle("Lord", this.World);

        var reloaded = new Player(0);
        reloaded.LoadPropertiesFromColumn(JsonHelper.Serialize(player.Properties.Clone()));

        Assert.Equal(new List<string> { "Lord" }, reloaded.UnlockedTitles());
    }

    [Fact]
    public void Equipping_offline_sends_nothing_and_touches_no_map()
    {
        var player = new Player(0);

        player.GrantTitle("Lord", this.World);

        Assert.Equal(Player.States.NotLoggedIn, player.State);
        Assert.Equal("Lord", player.Title);
    }
}
