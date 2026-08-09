using Goose.Tests.Fakes;

namespace Goose.Tests;

public class PlayerPropertiesTests
{
    [Fact]
    public void Defaults_to_an_empty_bag()
    {
        Assert.NotNull(new Player(0).Properties);
        Assert.Empty(new Player(0).Properties);
    }

    [Fact]
    public void Reads_the_player_properties_column()
    {
        var player = new Player(0);
        player.LoadPropertiesFromColumn("{\"dimension.max\":4}");

        Assert.Equal(4, player.Properties.GetProperty<int>("dimension.max"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Tolerates_an_empty_column(string value)
    {
        // Existing rows get '' from the ALTER TABLE default, so this is the common case.
        var player = new Player(0);
        player.LoadPropertiesFromColumn(value);

        Assert.NotNull(player.Properties);
        Assert.Empty(player.Properties);
    }
}
