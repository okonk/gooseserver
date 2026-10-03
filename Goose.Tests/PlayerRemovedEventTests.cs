using Goose.Testing;

namespace Goose.Tests;

public class PlayerRemovedEventTests
{
    [Fact]
    public void RemovePlayer_FiresOnceWithPlayer()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 5, 5);
        fixture.AddOnlinePlayer(player);

        int fired = 0;
        Player? received = null;
        fixture.World.PlayerHandler.PlayerRemoved += p => { fired++; received = p; };

        fixture.World.PlayerHandler.RemovePlayer(player);

        Assert.Equal(1, fired);
        Assert.Same(player, received);
    }

    [Fact]
    public void RemovePlayerBySocket_Fires()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 5, 5);
        fixture.AddOnlinePlayer(player);

        int fired = 0;
        Player? received = null;
        fixture.World.PlayerHandler.PlayerRemoved += p => { fired++; received = p; };

        fixture.World.PlayerHandler.RemovePlayer(player.Sock);

        Assert.Equal(1, fired);
        Assert.Same(player, received);
    }
}
