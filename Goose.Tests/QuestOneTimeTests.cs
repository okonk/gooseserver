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
}
