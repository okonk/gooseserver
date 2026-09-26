using System.Collections.Immutable;
using System.Text.Json;
using Goose;
using Goose.Events;
using Goose.Logs;
using Xunit;

namespace Goose.IntegrationTests
{
    public class LogSearchSessionPagingTests
    {
        private static LogQueryRow RowWithText(string text)
            => new(1, 123, 0, true, 0, false, 0, false, 0, false, 1, true, 2, true,
                "Chat", "Communication", LogOtherIdKind.Unused, null, null, null, "s", text);

        private static LogSearchQuery CursorlessBase()
            => new(1000, 2000, null, null, ImmutableArray<int>.Empty, "", null);

        [Fact]
        public void SuccessfulFresh_CommitsSessionAfterPrebuild_ReturnsPageOneTokenAndNextOnlyWhenHasMore()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();

            var lrf = fixture.LastLrf(gm)!;
            Assert.True(lrf.HasMore);
            Assert.True(LogPageTokenCodec.IsCanonical(lrf.Current));
            Assert.True(LogPageTokenCodec.IsCanonical(lrf.Next));
            Assert.NotEqual(lrf.Current, lrf.Next);
            Assert.NotNull(viewer.Session);
            Assert.Equal(lrf.Current, viewer.Session!.FirstPageToken);

            using var small = new LogSearchServerFixture();
            small.InsertRowsAscending(3);
            var gm2 = small.AddGm("Gm", 1);
            var viewer2 = small.OpenViewer(gm2);
            small.Monotonic = 0;

            small.Send(gm2, LogSearchServerFixture.Fresh(viewer2.ID, 1));
            small.Pump();

            var lrf2 = small.LastLrf(gm2)!;
            Assert.False(lrf2.HasMore);
            Assert.Equal("", lrf2.Next);
            Assert.True(LogPageTokenCodec.IsCanonical(lrf2.Current));
            Assert.Equal([3, 2, 1], small.LastRowIds(gm2));
            Assert.Equal(lrf2.Current, viewer2.Session!.FirstPageToken);
        }

        [Fact]
        public void ValidationFailure_PreservesCommittedSessionAndItsTokens()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            var lrf = fixture.LastLrf(gm)!;
            string first = lrf.Current;
            string next = lrf.Next;
            var session = viewer.Session!;

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2, startMs: 10, endMs: 5));
            fixture.Pump();
            Assert.Equal("Start must be before end.", fixture.LrxMessage(gm, viewer.ID, 2));
            Assert.Same(session, viewer.Session);
            Assert.Equal(first, viewer.Session!.FirstPageToken);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 3, next));
            fixture.Pump();
            Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2, 1], fixture.LastRowIds(gm));
            Assert.Same(session, viewer.Session);
        }

        [Fact]
        public void DbFailure_PreservesCommittedSessionAndItsTokens()
        {
            using var fixture = new LogSearchServerFixture(createPlayersTable: false);
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            var lrf = fixture.LastLrf(gm)!;
            var session = viewer.Session!;

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2, participant: "Nobody"));
            fixture.Pump();
            Assert.Equal("Log search failed.", fixture.LrxMessage(gm, viewer.ID, 2));
            Assert.Same(session, viewer.Session);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 3, lrf.Next));
            fixture.Pump();
            Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2, 1], fixture.LastRowIds(gm));
        }

        [Fact]
        public void SerializationAndResponseSizeFailures_PreserveCommittedSessionAndItsTokens()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            var lrf = fixture.LastLrf(gm)!;
            var session = viewer.Session!;
            var baseQuery = CursorlessBase();

            viewer.BeginQuery(10);
            fixture.Service.ReserveDbSlot();
            fixture.Service.CompleteFresh(gm, viewer, 10, baseQuery,
                new LogSearchPage(new[] { RowWithText(new string('x', 300_000)) }, false, null));
            fixture.GameWorld.Update();
            Assert.Equal("Result is too large to display. Narrow the search.", fixture.LastLrxMessage(gm));
            Assert.Same(session, viewer.Session);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);

            viewer.BeginQuery(11);
            fixture.Service.ReserveDbSlot();
            var oversizedRows = Enumerable.Range(0, 50).Select(_ => RowWithText(new string('x', 100_000))).ToArray();
            fixture.Service.CompleteFresh(gm, viewer, 11, baseQuery,
                new LogSearchPage(oversizedRows, true, new LogPageCursor(5, 9, 8)));
            fixture.GameWorld.Update();
            Assert.Equal("Result is too large to display. Narrow the search.", fixture.LastLrxMessage(gm));
            Assert.Same(session, viewer.Session);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 12, lrf.Next));
            fixture.Pump();
            Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2, 1], fixture.LastRowIds(gm));
        }

        [Fact]
        public void PageWithNextToken_ReturnsSuppliedTokenAsCurrent_AndServerIssuedNextFromNextCursor()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            string next = fixture.LastLrf(gm)!.Next;

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, next));
            fixture.Pump();

            var lrf = fixture.LastLrf(gm)!;
            Assert.Equal(next, lrf.Current);
            Assert.False(lrf.HasMore);
            Assert.Equal("", lrf.Next);
            Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2, 1], fixture.LastRowIds(gm));
        }

        [Fact]
        public void PageOneToken_ReplaysOriginalSnapshotAfterNewerAndOlderInserts()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            var lrf = fixture.LastLrf(gm)!;
            string first = lrf.Current;
            string next = lrf.Next;
            List<long> originalPageOne = fixture.LastRowIds(gm);
            Assert.Equal(50, originalPageOne.Count);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, next));
            fixture.Pump();

            long baseTicks = LogSearchServerFixture.ToTicks(LogSearchServerFixture.StartMs + 1_000);
            fixture.InsertLog(baseTicks + 61 * TimeSpan.TicksPerSecond, 0, 0, 0, 0, "newer");
            fixture.InsertLog(baseTicks + TimeSpan.TicksPerSecond / 2, 0, 0, 0, 0, "older");

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 3, first));
            fixture.Pump();

            Assert.Equal(originalPageOne, fixture.LastRowIds(gm));
        }

        [Fact]
        public void PageOneReplay_AddsNoAuditAndDoesNotHitFreshRateLimit()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            var lrf = fixture.LastLrf(gm)!;
            Assert.Equal(1, fixture.ViewLogsAuditCount);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, lrf.Next));
            fixture.Pump();
            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 3, lrf.Current));
            fixture.Pump();
            Assert.Equal(1, fixture.ViewLogsAuditCount);

            fixture.Monotonic += fixture.MillisecondTicks * 999;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 4));
            Assert.Equal("Log searches are rate limited; try again shortly.", fixture.LrxMessage(gm, viewer.ID, 4));
        }

        [Fact]
        public void ParticipantRenameAndAmbiguityAfterFresh_CannotAlterPageSemantics()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertPlayer(5, "Bob");
            fixture.InsertPlayer(9, "Alice");
            fixture.InsertRowsAscending(60, player: 5);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1, participant: "Bob"));
            fixture.Pump();
            var lrf = fixture.LastLrf(gm)!;

            fixture.RenamePlayer(5, "Bobby");
            fixture.InsertPlayer(10, "Bob");
            fixture.InsertPlayer(11, "Bob");

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, lrf.Next));
            fixture.Pump();

            Assert.Null(fixture.LrxMessage(gm, viewer.ID, 2));
            Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2, 1], fixture.LastRowIds(gm));
        }

        [Fact]
        public void FlippedTokenCharacter_RejectsBeforeDb()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            string next = fixture.LastLrf(gm)!.Next;
            string flipped = FlipToken(next);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, flipped));
            Assert.Equal("Log page token is invalid or expired.", fixture.LrxMessage(gm, viewer.ID, 2));
            fixture.GameWorld.Database.Execute(conn => { });
            Assert.Equal(0, fixture.GameWorld.Database.PendingCount);
            Assert.Equal(1, fixture.ViewLogsAuditCount);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 3, next));
            fixture.Pump();
            Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2, 1], fixture.LastRowIds(gm));
        }

        private static string FlipToken(string token)
        {
            char replacement = token[0] == 'A' ? 'B' : 'A';
            return replacement + token[1..];
        }

        [Fact]
        public void CrossScopeOldGenerationAndClearedViewerTokens_RejectBeforeDb()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm1 = fixture.AddGm("One", 1);
            var gm2 = fixture.AddGm("Two", 2);
            var viewer1 = fixture.OpenViewer(gm1);
            var viewer2 = fixture.OpenViewer(gm2);
            fixture.Monotonic = 0;

            fixture.Send(gm1, LogSearchServerFixture.Fresh(viewer1.ID, 1));
            fixture.Pump();
            string next = fixture.LastLrf(gm1)!.Next;
            int audits = fixture.ViewLogsAuditCount;

            fixture.Send(gm2, LogSearchServerFixture.Page(viewer2.ID, 1, next));
            Assert.Equal("Log page token is invalid or expired.", fixture.LrxMessage(gm2, viewer2.ID, 1));

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm1, LogSearchServerFixture.Fresh(viewer1.ID, 2, mapId: 7));
            fixture.Pump();

            fixture.Send(gm1, LogSearchServerFixture.Page(viewer1.ID, 3, next));
            Assert.Equal("Log page token is invalid or expired.", fixture.LrxMessage(gm1, viewer1.ID, 3));

            new WindowButtonClickEvent { Player = gm1, Data = $"WBC2,{viewer1.ID},0,0,0" }.Ready(fixture.GameWorld);
            var reopened = fixture.OpenViewer(gm1);
            fixture.Send(gm1, LogSearchServerFixture.Page(reopened.ID, 4, next));
            Assert.Equal("Log page token is invalid or expired.", fixture.LrxMessage(gm1, reopened.ID, 4));

            fixture.GameWorld.Database.Execute(conn => { });
            Assert.Equal(0, fixture.GameWorld.Database.PendingCount);
            Assert.Equal(audits + 1, fixture.ViewLogsAuditCount);
        }

        [Fact]
        public void OldToken_CannotSelectDifferentBaseQueryOrParticipant()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertPlayer(5, "Bob");
            fixture.InsertPlayer(9, "Alice");
            fixture.InsertRowsAscending(60, player: 5);
            fixture.InsertRowsAscending(60, player: 9);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1, participant: "Bob"));
            fixture.Pump();
            string bobNext = fixture.LastLrf(gm)!.Next;

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2, participant: "Alice"));
            fixture.Pump();
            int lrdBefore = gm.Sent.Count(s => s.StartsWith("LRD"));

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 3, bobNext));
            Assert.Equal("Log page token is invalid or expired.", fixture.LrxMessage(gm, viewer.ID, 3));
            Assert.Equal(lrdBefore, gm.Sent.Count(s => s.StartsWith("LRD")));
        }

        [Fact]
        public void RepeatedValidPage_ReusesTokenForEqualNextCursor_WithoutGrowingDuplicates()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(120);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            string t2 = fixture.LastLrf(gm)!.Next;

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, t2));
            fixture.Pump();
            string t3 = fixture.LastLrf(gm)!.Next;

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 3, t3));
            fixture.Pump();
            var lrf3 = fixture.LastLrf(gm)!;
            Assert.False(lrf3.HasMore);
            Assert.Equal("", lrf3.Next);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 4, t2));
            fixture.Pump();
            var lrf4 = fixture.LastLrf(gm)!;
            Assert.Equal(t2, lrf4.Current);
            Assert.Equal(t3, lrf4.Next);
        }

        [Fact]
        public void GarbagePageToken_CannotPerformFreshQuery_BypassAudit_OrCreateSession()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(5);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 1, LogPageTokenCodec.Create()));

            Assert.Equal("Log page token is invalid or expired.", fixture.LrxMessage(gm, viewer.ID, 1));
            Assert.Null(viewer.Session);
            Assert.Equal(0, fixture.ViewLogsAuditCount);
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith("LRB"));
            fixture.GameWorld.Database.Execute(conn => { });
            Assert.Equal(0, fixture.GameWorld.Database.PendingCount);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
        }

        [Fact]
        public void BroadSearch_ReturnsUnknownSignedInt64EventTypeThroughFallbackJson_WithoutTokenFailure()
        {
            using var fixture = new LogSearchServerFixture();
            long baseTicks = LogSearchServerFixture.ToTicks(LogSearchServerFixture.StartMs + 1_000);
            fixture.InsertLog(baseTicks + 2 * TimeSpan.TicksPerSecond, -123, 0, 0, 0, "mystery");
            fixture.InsertLog(baseTicks + TimeSpan.TicksPerSecond, 0, 0, 0, 0, "normal");
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();

            var lrf = fixture.LastLrf(gm)!;
            Assert.False(lrf.HasMore);
            Assert.True(LogPageTokenCodec.IsCanonical(lrf.Current));
            Assert.NotNull(viewer.Session);
            Assert.Null(fixture.LastLrxMessage(gm));
            var unknown = JsonDocument.Parse(fixture.LastRowJsons(gm)[0]).RootElement;
            Assert.Equal(-123, unknown.GetProperty("typeId").GetInt64());
            Assert.True(unknown.GetProperty("typeIsInteger").GetBoolean());
            Assert.Equal("Unknown event", unknown.GetProperty("eventLabel").GetString());

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, lrf.Current));
            fixture.Pump();
            Assert.Equal(2, fixture.LastRowJsons(gm).Count);
            Assert.Null(fixture.LastLrxMessage(gm));
        }
    }
}
