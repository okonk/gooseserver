using Goose;
using Xunit;

namespace Goose.Tests.Revive;

public class ReviveGatingTests
{
    private static readonly string DeadNotice = P.Notice(NoticeType.Info, "You can't do that while dead.");

    private static (ReviveTestKit Kit, Testing.TestWorldFixture.CapturingPlayer Corpse) Corpse()
    {
        var kit = new ReviveTestKit();
        var corpse = kit.PlayerAt(5, 5);
        kit.Kill(corpse);
        corpse.Sent.Clear();
        return (kit, corpse);
    }

    [Theory]
    [InlineData("CAST1,1")]
    [InlineData("USE1")]
    [InlineData("DRP1,1")]
    [InlineData("GET")]
    [InlineData("DITM1")]
    [InlineData("OCB")]
    [InlineData("WBC1,1")]
    [InlineData("VPI1,1,1")]
    [InlineData("VSI1,1,1")]
    [InlineData("ITW1,1,1")]
    [InlineData("WTI1,1,1")]
    [InlineData("WTW1,1,1,1")]
    [InlineData("EWS1,1,1")]
    [InlineData("EWC1,1")]
    [InlineData("CWS1,1,1")]
    [InlineData("CWC1,1")]
    [InlineData("STC1,1,1")]
    [InlineData("AMT1,1")]
    [InlineData("LC8,5")]
    [InlineData("RC8,5")]
    [InlineData("/dropgold 5")]
    public void Blocked_action_sends_the_dead_notice(string packet)
    {
        var (kit, corpse) = Corpse();
        using (kit)
        {
            Assert.True(kit.Fixture.RunCommand(corpse, packet));
            Assert.Contains(corpse.Sent, s => s.Contains(DeadNotice));
        }
    }

    [Theory]
    [InlineData("M1")]
    [InlineData("M2")]
    [InlineData("F2")]
    [InlineData("ATT")]
    [InlineData("EMOT1")]
    [InlineData("MNT")]
    public void Silent_blocks_send_no_notice_and_do_not_move(string packet)
    {
        var (kit, corpse) = Corpse();
        using (kit)
        {
            var facing = corpse.Facing;

            Assert.True(kit.Fixture.RunCommand(corpse, packet));

            Assert.DoesNotContain(corpse.Sent, s => s.Contains(DeadNotice));
            Assert.Equal(5, corpse.MapX);
            Assert.Equal(5, corpse.MapY);
            Assert.Equal(facing, corpse.Facing);
        }
    }

    [Fact]
    public void Left_click_on_an_empty_tile_sends_no_notice()
    {
        var (kit, corpse) = Corpse();
        using (kit)
        {
            Assert.True(kit.Fixture.RunCommand(corpse, "LC10,10"));
            Assert.DoesNotContain(corpse.Sent, s => s.Contains(DeadNotice));
        }
    }

    [Fact]
    public void Corpse_can_chat()
    {
        using var kit = new ReviveTestKit();
        kit.Map.CanChat = true;
        var corpse = kit.PlayerAt(5, 5);
        kit.Kill(corpse);
        var listener = kit.PlayerAt(6, 6, "Listener");
        listener.Sent.Clear();

        Assert.True(kit.Fixture.RunCommand(corpse, ";hello there"));

        Assert.Contains(listener.Sent, s => s.Contains("hello there"));
        Assert.DoesNotContain(corpse.Sent, s => s.Contains(DeadNotice));
    }
}
