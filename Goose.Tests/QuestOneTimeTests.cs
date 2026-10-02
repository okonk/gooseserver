using Goose.Quests;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class QuestOneTimeTests
{
    [Fact]
    public void No_quest_is_claimed_before_any_claim_exists()
    {
        using var fixture = new TestWorldFixture();
        Assert.False(fixture.World.QuestHandler.IsClaimed(9));
    }

    [Fact]
    public void GetPlayerName_resolves_registered_player_and_null_for_unknown_id()
    {
        using var fixture = new TestWorldFixture();
        var player = new TestWorldFixture.CapturingPlayer { Name = "Bob", PlayerID = 42 };
        fixture.RegisterDatabasePlayer(player);

        Assert.Equal("Bob", fixture.World.PlayerHandler.GetPlayerName(42));
        Assert.Null(fixture.World.PlayerHandler.GetPlayerName(9999));
    }
}
