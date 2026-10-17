using Goose;
using Goose.Testing;

namespace Goose.Tests;

public class BuffNoticeTests
{
    private const string OnText = "You feel strong.";
    private const string OffText = "You feel weak.";

    private static (TestWorldFixture fixture, TestWorldFixture.CapturingPlayer player, Buff buff) WorldWithBuffedPlayer()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "buff",
            e => { e.OnEffectText = OnText; e.OffEffectText = OffText; e.Duration = 120; });
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };

        return (fixture, player, buff);
    }

    [Fact]
    public void AddBuff_SendsBuffOnNotice()
    {
        var (fixture, player, buff) = WorldWithBuffedPlayer();
        using (fixture)
        {
            player.AddBuff(buff, fixture.World);

            Assert.Contains(player.Sent, s => s.StartsWith(P.Notice(NoticeType.BuffOn, OnText)));
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("$7" + OnText));
        }
    }

    [Fact]
    public void RemoveBuff_SendsBuffOffNotice()
    {
        var (fixture, player, buff) = WorldWithBuffedPlayer();
        using (fixture)
        {
            player.AddBuff(buff, fixture.World);
            player.Sent.Clear();
            player.State = Player.States.Ready;

            player.RemoveBuff(buff, fixture.World);

            Assert.Contains(player.Sent, s => s.StartsWith(P.Notice(NoticeType.BuffOff, OffText)));
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("$7" + OffText));
        }
    }

    [Fact]
    public void AddBuff_SameEffectRenew_SendsOffThenOn()
    {
        var (fixture, player, buff) = WorldWithBuffedPlayer();
        using (fixture)
        {
            player.AddBuff(buff, fixture.World);
            player.Sent.Clear();

            player.AddBuff(new Buff { Caster = player, Target = player, SpellEffect = buff.SpellEffect }, fixture.World);

            int offIndex = player.Sent.FindIndex(s => s.StartsWith(P.Notice(NoticeType.BuffOff, OffText)));
            int onIndex = player.Sent.FindIndex(s => s.StartsWith(P.Notice(NoticeType.BuffOn, OnText)));

            Assert.True(offIndex >= 0, "expected a BuffOff notice on renew");
            Assert.True(onIndex >= 0, "expected a BuffOn notice on renew");
            Assert.True(offIndex < onIndex, "BuffOff notice must precede the BuffOn notice");
        }
    }

    [Fact]
    public void AddBuff_NonStackingOver_SendsInfoNotice()
    {
        var (fixture, player, buff) = WorldWithBuffedPlayer();
        using (fixture)
        {
            player.AddBuff(buff, fixture.World);
            player.Sent.Clear();

            var other = fixture.AddBaseSpellEffect(2, "other", e => { e.Duration = 120; });
            other.BuffDoesntStackOver.Add(buff.SpellEffect);
            var newBuff = new Buff { Caster = player, Target = player, SpellEffect = other };

            player.AddBuff(newBuff, fixture.World);

            Assert.Contains(player.Sent, s => s.StartsWith(P.Notice(NoticeType.Info, "The buff had no effect.")));
            Assert.DoesNotContain(player.Sent, s => s.StartsWith("$7The buff had no effect."));
            Assert.Single(player.Buffs);
            Assert.Same(buff, player.Buffs[0]);
        }
    }

    [Fact]
    public void AddBuff_EmptyOnEffectText_SendsNoNotice()
    {
        var (fixture, player, _) = WorldWithBuffedPlayer();
        using (fixture)
        {
            var effect = fixture.AddBaseSpellEffect(2, "silent", e => { e.Duration = 120; e.OnEffectText = ""; e.OffEffectText = ""; });
            var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };

            player.AddBuff(buff, fixture.World);

            Assert.DoesNotContain(player.Sent, s => s.StartsWith("NTF2"));
        }
    }
}
