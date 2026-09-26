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

        var (remainingMs, totalMs) = ParseRemainingAndTotalMs(player.Sent, "BUF1,");
        Assert.InRange(remainingMs, 119000, 120000);
        Assert.Equal(120000, totalMs);
        Assert.Contains("BUF2" + "\x01", player.Sent);
        Assert.Contains("BUF3" + "\x01", player.Sent);
    }

    [Fact]
    public void SendBuffBar_PartiallyElapsedBuff_SendsRemainingAndTotal()
    {
        using var fixture = new TestWorldFixture(s => s.BuffBarVisibleSize = 2);
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "Speed",
            e => { e.Duration = 120; e.BuffGraphic = 5; e.BuffGraphicFile = 12; });
        var now = fixture.World.TimeNow;
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect, TimeCast = now - 60 * fixture.World.TimerFrequency });

        player.SendBuffBar(fixture.World);

        var (remainingMs, totalMs) = ParseRemainingAndTotalMs(player.Sent, "BUF1,");
        Assert.InRange(remainingMs, 59000, 60000);
        Assert.Equal(120000, totalMs);
    }

    private static (long, long) ParseRemainingAndTotalMs(List<string> sent, string prefix)
    {
        var packet = sent.Single(s => s.StartsWith(prefix)).TrimEnd('\x01').Split(',');
        return (long.Parse(packet[4]), long.Parse(packet[5]));
    }

    [Fact]
    public void SendBuffBar_PermanentBuff_SendsZeroDuration()
    {
        // Duration 0 → "BUF1,5,12,Charm,0,0" (client shows no sweep for 0)
        using var fixture = new TestWorldFixture(s => s.BuffBarVisibleSize = 1);
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "Charm",
            e => { e.Duration = 0; e.BuffGraphic = 5; e.BuffGraphicFile = 12; });
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect });

        player.SendBuffBar(fixture.World);

        Assert.Contains("BUF1,5,12,Charm,0,0" + "\x01", player.Sent);
    }

    [Fact]
    public void SendBuffBar_MoreBuffsThanVisibleSize_SendsNoRowsPastTheCap()
    {
        using var fixture = new TestWorldFixture(s => s.BuffBarVisibleSize = 3);
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        AddBuffs(fixture, player, 5);

        player.SendBuffBar(fixture.World);

        Assert.Equal(3, player.Sent.Count(s => s.StartsWith("BUF", StringComparison.Ordinal)));
        Assert.Contains(player.Sent, s => s.StartsWith("BUF3,5,12,Buff3,"));
        Assert.DoesNotContain(player.Sent, s => s.StartsWith("BUF4", StringComparison.Ordinal));
    }

    [Fact]
    public void SendBuffBar_ShrinksAfterOverflow_LeavesNoStrandedTail()
    {
        using var fixture = new TestWorldFixture(s => s.BuffBarVisibleSize = 3);
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        AddBuffs(fixture, player, 5);
        player.SendBuffBar(fixture.World);
        player.Sent.Clear();

        player.Buffs.RemoveRange(2, 3);

        player.SendBuffBar(fixture.World);

        Assert.Contains(player.Sent, s => s.StartsWith("BUF1,5,12,Buff1,"));
        Assert.Contains(player.Sent, s => s.StartsWith("BUF2,5,12,Buff2,"));
        Assert.Contains("BUF3" + "\x01", player.Sent);
        Assert.DoesNotContain(player.Sent, s => s.StartsWith("BUF4", StringComparison.Ordinal));
    }

    [Fact]
    public void SendBuffBar_NameWithComma_KeepsTheRowFieldsAligned()
    {
        using var fixture = new TestWorldFixture(s => s.BuffBarVisibleSize = 1);
        var map = fixture.AddBaseMap(1, "m");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        var effect = fixture.AddBaseSpellEffect(1, "Speed, Greater",
            e => { e.Duration = 120; e.BuffGraphic = 5; e.BuffGraphicFile = 12; });
        player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect, TimeCast = fixture.World.TimeNow });

        player.SendBuffBar(fixture.World);

        var row = player.Sent.Single(s => s.StartsWith("BUF1,")).TrimEnd('\x01').Split(',');
        Assert.Equal(6, row.Length);
        Assert.Equal("Speed Greater", row[3]);
        Assert.InRange(long.Parse(row[4]), 119000, 120000);
        Assert.Equal("120000", row[5]);
    }

    private static void AddBuffs(TestWorldFixture fixture, Player player, int count)
    {
        for (int id = 1; id <= count; id++)
        {
            var effect = fixture.AddBaseSpellEffect(id, "Buff" + id,
                e => { e.Duration = 120; e.BuffGraphic = 5; e.BuffGraphicFile = 12; });
            player.Buffs.Add(new Buff { Caster = player, Target = player, SpellEffect = effect, TimeCast = fixture.World.TimeNow });
        }
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
