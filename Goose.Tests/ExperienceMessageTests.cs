using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class ExperienceMessageTests
{
    [Fact]
    public void Too_far_away_is_sent_as_a_notice()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", 20, 20);
        var player = fixture.CommandPlayerOn(map, 5, 5, "A");

        player.AddExperience(0, fixture.World, Player.ExperienceMessage.TooFarAway);

        Assert.Contains(player.Sent, s => s.TrimEnd('\x1') == P.Notice(NoticeType.Info, "You were too far away to gain experience."));
    }
}
