using Goose.Testing;

namespace Goose.Tests;

public class PartyMemberBuffPacketTests
{
    [Fact]
    public void PartyBuff_ExactPayload_NameIsFinalField()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 1, 1);
        player.LoginID = 42;
        var effect = fixture.AddBaseSpellEffect(7, "Strength, Greater",
            e => { e.Duration = 120; e.BuffGraphic = 810020; e.BuffGraphicFile = 20107; });
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect, TimeCast = fixture.World.TimeNow };

        Assert.Equal("PBA42,7,810020,20107,59000,120000,Strength, Greater",
            P.PartyBuff(player, buff, 59000, 120000));
    }

    [Fact]
    public void PartyBuffRemove_ExactPayload()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 1, 1);
        player.LoginID = 42;

        Assert.Equal("PBR42,7", P.PartyBuffRemove(player, 7));
    }

    [Fact]
    public void PartyBuffClear_ExactPayload()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 1, 1);
        player.LoginID = 42;

        Assert.Equal("PBC42", P.PartyBuffClear(player));
    }

    [Fact]
    public void GetDurations_FreshTimedBuff_ReturnsFullDuration()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 1, 1);
        var effect = fixture.AddBaseSpellEffect(7, "Strength, Greater", e => e.Duration = 120);
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect, TimeCast = fixture.World.TimeNow };

        var (remainingMs, totalMs) = buff.GetDurations(fixture.World);

        Assert.InRange(remainingMs, 119000, 120000);
        Assert.Equal(120000, totalMs);
    }

    [Fact]
    public void GetDurations_PartiallyElapsedBuff_ReturnsClampedRemaining()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 1, 1);
        var effect = fixture.AddBaseSpellEffect(7, "Strength, Greater", e => e.Duration = 120);
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect,
            TimeCast = fixture.World.TimeNow - 60 * fixture.World.TimerFrequency };

        var (remainingMs, totalMs) = buff.GetDurations(fixture.World);

        Assert.InRange(remainingMs, 59000, 60000);
        Assert.Equal(120000, totalMs);
    }

    [Fact]
    public void GetDurations_OverdueBuff_ReturnsZeroRemaining()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 1, 1);
        var effect = fixture.AddBaseSpellEffect(7, "Strength, Greater", e => e.Duration = 120);
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect,
            TimeCast = fixture.World.TimeNow - 200 * fixture.World.TimerFrequency };

        var (remainingMs, totalMs) = buff.GetDurations(fixture.World);

        Assert.Equal(0, remainingMs);
        Assert.Equal(120000, totalMs);
    }

    [Fact]
    public void GetDurations_PermanentBuff_ReturnsZeroPair()
    {
        using var fixture = new TestWorldFixture();
        var player = fixture.CommandPlayerOn(fixture.AddBaseMap(1, "m"), 1, 1);
        var effect = fixture.AddBaseSpellEffect(7, "Charm", e => e.Duration = 0);
        var buff = new Buff { Caster = player, Target = player, SpellEffect = effect };

        var (remainingMs, totalMs) = buff.GetDurations(fixture.World);

        Assert.Equal((0, 0), (remainingMs, totalMs));
    }

    [Fact]
    public void SendBuffBar_ExactPayload_UnchangedAfterExtractingDurations()
    {
        using var fixture = new TestWorldFixture(s => s.BuffBarVisibleSize = 2);
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(7, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 5; e.BuffGraphicFile = 12; });
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect });

        player.SendBuffBar(fixture.World);

        Assert.Contains("BUF1,5,12,Charm,0,0" + "\x01", player.Sent);
        Assert.Contains("BUF2" + "\x01", player.Sent);
    }
}
