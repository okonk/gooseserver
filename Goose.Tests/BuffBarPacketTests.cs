using Goose.Testing;

namespace Goose.Tests;

public class BuffBarPacketTests
{
    [Fact]
    public void SendBuffBar_TimedBuff_SendsRemainingMs()
    {
        using var fixture = new TestWorldFixture(s => s.BuffBarVisibleSize = 3);
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "Speed",
            e => { e.Duration = 120; e.BuffGraphic = 5; e.BuffGraphicFile = 12; });
        var now = fixture.World.TimeNow;
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect, TimeCast = now });

        player.SendBuffBar(fixture.World);

        var remainingMs = ParseRemainingMs(player.Sent, "BUF1,");
        Assert.InRange(remainingMs, 119000, 120000);
        Assert.Contains("BUF2" + "\x01", player.Sent);
        Assert.Contains("BUF3" + "\x01", player.Sent);
    }

    [Fact]
    public void SendBuffBar_PartiallyElapsedBuff_SendsRemainingNotTotal()
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "Speed",
            e => { e.Duration = 120; e.BuffGraphic = 5; e.BuffGraphicFile = 12; });
        var now = fixture.World.TimeNow;
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect, TimeCast = now - 60 * fixture.World.TimerFrequency });

        player.SendBuffBar(fixture.World);

        var remainingMs = ParseRemainingMs(player.Sent, "BUF1,");
        Assert.InRange(remainingMs, 59000, 60000);
    }

    private static long ParseRemainingMs(List<string> sent, string prefix)
    {
        var packet = sent.Single(s => s.StartsWith(prefix));
        return long.Parse(packet.TrimEnd('\x01').Split(',')[4]);
    }

    [Fact]
    public void SendBuffBar_PermanentBuff_SendsZeroDuration()
    {
        // Duration 0 → "BUF1,5,12,Charm,0" (client shows no sweep for 0)
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 5; e.BuffGraphicFile = 12; });
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect });

        player.SendBuffBar(fixture.World);

        Assert.Contains("BUF1,5,12,Charm,0" + "\x01", player.Sent);
    }

    [Fact]
    public void SendBuffBar_EmptySlot_HasNoDurationField()
    {
        // Adversarial: catches unconditionally appending ",0" to empty slots
        using var fixture = new TestWorldFixture(s => s.BuffBarVisibleSize = 2);
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);

        player.SendBuffBar(fixture.World);

        Assert.Contains("BUF1" + "\x01", player.Sent);
        Assert.Contains("BUF2" + "\x01", player.Sent);
        Assert.DoesNotContain("BUF1,0" + "\x01", player.Sent);
    }
}
