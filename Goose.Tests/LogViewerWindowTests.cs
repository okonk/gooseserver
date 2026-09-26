using System.Collections.Immutable;
using Goose;
using Goose.Events;
using Goose.Logs;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class LogViewerWindowTests
{
    private static string GroupDisplay(LogEventGroup group) => group switch
    {
        LogEventGroup.Communication => "Communication",
        LogEventGroup.SessionsSecurity => "Sessions/Security",
        LogEventGroup.Social => "Social",
        LogEventGroup.ItemsEconomy => "Items/Economy",
        LogEventGroup.GmActions => "GM Actions",
        _ => "Other/Retired",
    };

    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player) SetupGm()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(7, "Map, One");
        var player = fixture.CommandPlayerOn(map, 1, 1);
        player.Access = Player.AccessStatus.GameMaster;
        return (fixture, player);
    }

    private static LogSearchQuery MakeBaseQuery()
        => new(1000, 2000, 42, null, ImmutableArray<int>.Empty, "", null);

    [Fact]
    public void Open_SendsMkwThenIdOrderedLmtThenAscendingLmmThenLmdThenEnw()
    {
        var (fixture, player) = SetupGm();

        Assert.True(LogViewerWindow.Open(player, fixture.World));

        var sent = player.Sent;
        Assert.StartsWith("MKW1001,29,GM Log Viewer,0,1,0,0,0", sent[0]);

        int enwIndex = sent.FindIndex(s => s.StartsWith("ENW"));
        Assert.Equal(sent.Count - 1, enwIndex);
        Assert.Equal("ENW1001", sent[enwIndex].TrimEnd(LogProtocolPackets.PacketDelimiter));

        var middle = sent.Skip(1).Take(enwIndex - 1)
            .Select(s => s.TrimEnd(LogProtocolPackets.PacketDelimiter)).ToList();

        int lmtCount = 0;
        while (lmtCount < middle.Count && middle[lmtCount].StartsWith("LMT"))
            lmtCount++;
        var lmts = middle.Take(lmtCount).ToList();
        var expected = LogEventRegistry.Known.OrderBy(d => d.Id).ToList();
        Assert.Equal(expected.Count, lmts.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            var parts = lmts[i].Split(',');
            Assert.Equal(4, parts.Length);
            Assert.Equal("LMT1001", parts[0]);
            Assert.Equal(expected[i].Id.ToString(), parts[1]);
            Assert.Equal(ProtocolTextCodec.EncodeText(GroupDisplay(expected[i].Group)), parts[2]);
            Assert.Equal(ProtocolTextCodec.EncodeText(expected[i].Label), parts[3]);
        }
        Assert.Contains(expected, d => d.Id == 14);
        Assert.Contains(expected, d => d.Id == 10013);

        var lmms = middle.Skip(lmtCount).TakeWhile(s => s.StartsWith("LMM")).ToList();
        Assert.Single(lmms);
        string mapB64 = lmms[0]["LMM1001,7,".Length..];
        Assert.True(ProtocolTextCodec.TryDecodeText(mapB64, 1024, out string mapName));
        Assert.Equal("Map, One", mapName);
        Assert.Equal(ProtocolTextCodec.EncodeText("Map, One"), mapB64);

        Assert.StartsWith("LMD1001,", middle[^1]);
        var lmdParts = middle[^1].Split(',');
        Assert.Equal(3, lmdParts.Length);
        long start = long.Parse(lmdParts[1]);
        long end = long.Parse(lmdParts[2]);
        Assert.Equal(86_400_000, end - start);
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Assert.InRange(end, now - 5_000, now + 5_000);
    }

    [Fact]
    public void Open_MultipleMaps_SendsAscendingMapIds()
    {
        var (fixture, player) = SetupGm();
        fixture.AddBaseMap(2, "Alpha");
        fixture.AddBaseMap(9, "Beta");

        Assert.True(LogViewerWindow.Open(player, fixture.World));

        var lmms = player.Sent.Where(s => s.StartsWith("LMM"))
            .Select(s => s.TrimEnd(LogProtocolPackets.PacketDelimiter)).ToArray();
        Assert.Equal(
            [
                "LMM1001,2," + ProtocolTextCodec.EncodeText("Alpha"),
                "LMM1001,7," + ProtocolTextCodec.EncodeText("Map, One"),
                "LMM1001,9," + ProtocolTextCodec.EncodeText("Beta"),
            ],
            lmms);
    }

    [Fact]
    public void Open_ExistingViewer_ClosesOldBeforePublishingNew()
    {
        var (fixture, player) = SetupGm();

        Assert.True(LogViewerWindow.Open(player, fixture.World));
        var first = player.Windows[0];
        player.Sent.Clear();

        Assert.True(LogViewerWindow.Open(player, fixture.World));

        Assert.Single(player.Windows);
        var second = player.Windows[0];
        Assert.NotSame(first, second);
        Assert.Equal(first.ID + 1, second.ID);
        var clwIndex = player.Sent.FindIndex(s => s.StartsWith("CLW"));
        var mkwIndex = player.Sent.FindIndex(s => s.StartsWith("MKW"));
        Assert.True(clwIndex >= 0 && clwIndex < mkwIndex);
        Assert.Equal($"CLW{first.ID}", player.Sent[clwIndex].TrimEnd(LogProtocolPackets.PacketDelimiter));
        Assert.StartsWith($"MKW{second.ID},29,GM Log Viewer,", player.Sent[mkwIndex]);
    }

    [Fact]
    public void WindowButtonClick_ClosesWithoutClwEcho()
    {
        var (fixture, player) = SetupGm();

        Assert.True(LogViewerWindow.Open(player, fixture.World));
        int id = player.Windows[0].ID;

        new WindowButtonClickEvent { Player = player, Data = $"WBC2,{id},0,0,0" }.Ready(fixture.World);

        Assert.Empty(player.Windows);
        Assert.DoesNotContain(player.Sent, s => s.StartsWith("CLW"));
    }

    [Fact]
    public void ReopenAndClose_DestroyOldSessionCapabilities()
    {
        var (fixture, player) = SetupGm();
        var baseQuery = MakeBaseQuery();

        Assert.True(LogViewerWindow.Open(player, fixture.World));
        var session = new LogViewerSearchSession(baseQuery, 1);
        ((LogViewerWindow)player.Windows[0]).Session = session;
        var token = session.IssueToken(new LogPageCursor(1, null, null));
        Assert.True(session.TryResolve(token, out _, out _));

        Assert.True(LogViewerWindow.Open(player, fixture.World));
        Assert.False(session.TryResolve(session.FirstPageToken, out _, out _));
        Assert.False(session.TryResolve(token, out _, out _));
        Assert.Null(((LogViewerWindow)player.Windows[0]).Session);

        var session2 = new LogViewerSearchSession(baseQuery, 2);
        ((LogViewerWindow)player.Windows[0]).Session = session2;
        new WindowButtonClickEvent
        {
            Player = player,
            Data = $"WBC2,{player.Windows[0].ID},0,0,0",
        }.Ready(fixture.World);
        Assert.False(session2.TryResolve(session2.FirstPageToken, out _, out _));
    }
}
