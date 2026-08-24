using System.Linq;
using Goose;
using Goose.Testing;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class ChatMessageLengthTests
{
    private const int MaxMessageLength = 300;

    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player, Map Map) NewPlayer()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town");
        map.CanChat = true;
        map.CanAuction = true;
        map.Muted = false;

        var player = fixture.CommandPlayerOn(map, 5, 5, "Alice");
        fixture.RegisterOnlinePlayer(player);
        return (fixture, player, map);
    }

    private static int CountOf(GameWorld world, Log.Types type)
        => world.LogHandler.Pending.Count(l => l.Type == type);

    [Fact]
    public void Chat_atTheLimit_isLogged()
    {
        var (fixture, player, _) = NewPlayer();
        using (fixture)
        {
            fixture.RunCommand(player, ";" + new string('a', MaxMessageLength));

            Assert.Equal(1, CountOf(fixture.World, Log.Types.Chat));
        }
    }

    [Fact]
    public void Chat_onePastTheLimit_isDropped()
    {
        var (fixture, player, _) = NewPlayer();
        using (fixture)
        {
            fixture.RunCommand(player, ";" + new string('a', MaxMessageLength + 1));

            Assert.Equal(0, CountOf(fixture.World, Log.Types.Chat));
        }
    }

    [Fact]
    public void Tell_atTheLimit_isLogged()
    {
        var (fixture, player, map) = NewPlayer();
        using (fixture)
        {
            var bob = fixture.CommandPlayerOn(map, 6, 5, "Bob");
            fixture.RegisterOnlinePlayer(bob);

            fixture.RunCommand(player, "/tell Bob " + new string('a', MaxMessageLength));

            Assert.Equal(1, CountOf(fixture.World, Log.Types.Tell));
        }
    }

    [Fact]
    public void Tell_onePastTheLimit_isDropped()
    {
        var (fixture, player, map) = NewPlayer();
        using (fixture)
        {
            var bob = fixture.CommandPlayerOn(map, 6, 5, "Bob");
            fixture.RegisterOnlinePlayer(bob);

            fixture.RunCommand(player, "/tell Bob " + new string('a', MaxMessageLength + 1));

            Assert.Equal(0, CountOf(fixture.World, Log.Types.Tell));
        }
    }

    [Fact]
    public void Auction_atTheLimit_isLogged()
    {
        var (fixture, player, _) = NewPlayer();
        using (fixture)
        {
            fixture.RunCommand(player, "/auction " + new string('a', MaxMessageLength));

            Assert.Equal(1, CountOf(fixture.World, Log.Types.Auction));
        }
    }

    [Fact]
    public void Auction_onePastTheLimit_isDropped()
    {
        var (fixture, player, _) = NewPlayer();
        using (fixture)
        {
            fixture.RunCommand(player, "/auction " + new string('a', MaxMessageLength + 1));

            Assert.Equal(0, CountOf(fixture.World, Log.Types.Auction));
        }
    }
}
