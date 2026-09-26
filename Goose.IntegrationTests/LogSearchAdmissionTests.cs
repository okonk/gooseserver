using System.Collections.Immutable;
using Goose;
using Goose.Events;
using Goose.Logs;
using Xunit;

namespace Goose.IntegrationTests
{
    public class LogSearchAdmissionTests
    {
        private static LogQueryRow RowWithText(string text)
            => new(1, 123, 0, true, 0, false, 0, false, 0, false, 1, true, 2, true,
                "Chat", "Communication", LogOtherIdKind.Unused, null, null, null, "s", text);

        private static LogSearchQuery CursorlessBase()
            => new(1000, 2000, null, null, ImmutableArray<int>.Empty, "", null);

        [Fact]
        public void RestrictedDispatch_SwallowsNormalLqs_AndServiceRecheckCatchesRevoke()
        {
            using var fixture = new LogSearchServerFixture();
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            string packet = LogSearchServerFixture.Fresh(viewer.ID, 1);

            var normal = fixture.AddGm("Normal", 2);
            normal.Access = Player.AccessStatus.Normal;
            int eventsBefore = fixture.GameWorld.EventHandler.Count;
            Assert.True(fixture.GameWorld.EventHandler.AddEvent(normal, packet));
            Assert.Equal(eventsBefore, fixture.GameWorld.EventHandler.Count);
            Assert.Empty(normal.Sent);
            Assert.Equal(0, fixture.ViewLogsAuditCount);

            var ev = new LogQueryEvent { Player = gm, Data = packet };
            ev.ClientOriginated = true;
            fixture.GameWorld.EventHandler.AddEvent(ev);
            gm.Access = Player.AccessStatus.Normal;
            fixture.GameWorld.Update();

            Assert.DoesNotContain(gm.Sent, s => s.StartsWith("LR"));
            Assert.Equal(0, fixture.ViewLogsAuditCount);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
        }

        [Fact]
        public void ValidFresh_MarksQuerying_ReservesOneSlot_ValidatesThenExecutesOnSameWorkItem()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));

            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            Assert.Equal(1, viewer.ActiveRequestId);
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);

            fixture.Pump();

            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            Assert.Contains(gm.Sent, s => s.StartsWith($"LRB{viewer.ID},1"));
            Assert.Equal([3, 2, 1], fixture.LastRowIds(gm));
            var lrf = fixture.LastLrf(gm)!;
            Assert.False(lrf.HasMore);
            Assert.Equal("", lrf.Next);
            Assert.True(LogPageTokenCodec.IsCanonical(lrf.Current));
            Assert.Equal(1, fixture.ViewLogsAuditCount);
        }

        [Fact]
        public void Page_NeverValidatesFresh_ExecutesOnlyStoredBaseWithResolvedCursor()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60, player: 5);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1, participant: "#5"));
            fixture.Pump();
            var lrf = fixture.LastLrf(gm)!;
            Assert.True(lrf.HasMore);
            Assert.Equal(1, fixture.ViewLogsAuditCount);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, lrf.Next));
            fixture.Pump();

            Assert.Equal([10, 9, 8, 7, 6, 5, 4, 3, 2, 1], fixture.LastRowIds(gm));
            Assert.Equal(1, fixture.ViewLogsAuditCount);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            var lrf2 = fixture.LastLrf(gm)!;
            Assert.False(lrf2.HasMore);
            Assert.Equal(lrf.Next, lrf2.Current);
        }

        [Fact]
        public void SameViewer_RejectsFreshAndPageWhileQueryingOrDelivering()
        {
            using var fixture = new LogSearchServerFixture();
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 2));
            fixture.GameWorld.Update();
            Assert.Equal("A log search is already running.", fixture.LrxMessage(gm, viewer.ID, 2));
            fixture.Pump();

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 3));
            fixture.GameWorld.Update();
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            string token = viewer.Session!.FirstPageToken;
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 4));
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Page(viewer.ID, 5, token));
            fixture.GameWorld.Update();
            Assert.Equal("A log search is already running.", fixture.LrxMessage(gm, viewer.ID, 4));
            Assert.Equal("A log search is already running.", fixture.LrxMessage(gm, viewer.ID, 5));

            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 6));
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            fixture.Pump();
        }

        [Fact]
        public void Fresh_RateLimitedAt999ms_AdmittedAt1000ms_ReplacementCannotBypassPerPlayerClock()
        {
            using var fixture = new LogSearchServerFixture();
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();

            fixture.Monotonic += fixture.MillisecondTicks * 999;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2));
            Assert.Equal("Log searches are rate limited; try again shortly.", fixture.LrxMessage(gm, viewer.ID, 2));
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            fixture.Monotonic += fixture.MillisecondTicks;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 3));
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            fixture.Pump();

            new WindowButtonClickEvent { Player = gm, Data = $"WBC2,{viewer.ID},0,0,0" }.Ready(fixture.GameWorld);
            var replacement = fixture.OpenViewer(gm);
            Assert.NotSame(viewer, replacement);

            fixture.Monotonic += fixture.MillisecondTicks * 999;
            fixture.Send(gm, LogSearchServerFixture.Fresh(replacement.ID, 4));
            Assert.Equal("Log searches are rate limited; try again shortly.", fixture.LrxMessage(gm, replacement.ID, 4));

            fixture.Monotonic += fixture.MillisecondTicks;
            fixture.Send(gm, LogSearchServerFixture.Fresh(replacement.ID, 5));
            Assert.Equal(LogSearchPhase.Querying, replacement.Phase);
            fixture.Pump();
        }

        [Fact]
        public void Page_NeverRateLimited_NeverUpdatesFreshTimestamp()
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
            Assert.Equal(1, fixture.ViewLogsAuditCount);

            fixture.Monotonic += fixture.MillisecondTicks * 999;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 3));
            Assert.Equal("Log searches are rate limited; try again shortly.", fixture.LrxMessage(gm, viewer.ID, 3));

            fixture.Monotonic += fixture.MillisecondTicks;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 4));
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            fixture.Pump();
        }

        [Fact]
        public void FourBlockedQueries_IncludeRunningItem_RejectFifth_DrainReleasesSlotDuringSlowDelivery()
        {
            using var fixture = new LogSearchServerFixture();
            var gms = new[]
            {
                fixture.AddGm("A", 1), fixture.AddGm("B", 2), fixture.AddGm("C", 3),
                fixture.AddGm("D", 4), fixture.AddGm("E", 5),
            };
            var viewers = gms.Select(g => fixture.OpenViewer(g)).ToArray();
            fixture.Monotonic = 0;
            var block = new ManualResetEventSlim(false);
            fixture.GameWorld.Database.Enqueue(conn => block.Wait());
            for (int i = 0; i < 4; i++)
                fixture.GameWorld.EventHandler.AddEvent(gms[i], LogSearchServerFixture.Fresh(viewers[i].ID, 1));
            fixture.GameWorld.Update();
            Assert.Equal(4, fixture.Service.ActiveDbQueryCount);

            fixture.GameWorld.EventHandler.AddEvent(gms[4], LogSearchServerFixture.Fresh(viewers[4].ID, 1));
            fixture.GameWorld.Update();
            Assert.Equal("Too many log searches are in progress; try again shortly.",
                fixture.LrxMessage(gms[4], viewers[4].ID, 1));

            block.Set();
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            Assert.All(viewers.Take(4), v => Assert.Equal(LogSearchPhase.Delivering, v.Phase));

            fixture.Send(gms[4], LogSearchServerFixture.Fresh(viewers[4].ID, 2));
            Assert.Equal(LogSearchPhase.Querying, viewers[4].Phase);
            fixture.Pump();
        }

        [Fact]
        public void SynchronousEnqueueFailure_RollsBackQueryingAndCapacity_ConsumesNoRateOrAudit()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.GameWorld.Database.Stop();
            SetDatabase(fixture.GameWorld, new Database());
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));

            Assert.Equal("Log search failed.", fixture.LrxMessage(gm, viewer.ID, 1));
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            Assert.Equal(0, fixture.ViewLogsAuditCount);

            var db = new Database();
            db.Start(fixture.DbPath);
            SetDatabase(fixture.GameWorld, db);
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2));
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            fixture.Pump();
            Assert.Equal(1, fixture.ViewLogsAuditCount);
        }

        private static void SetDatabase(GameWorld world, Database database)
        {
            typeof(GameWorld).GetProperty(nameof(GameWorld.Database))!.SetValue(world, database);
        }

        [Fact]
        public void EveryEnqueuedFresh_AppendsExactlyOneExactAudit_IncludingValidationDbAndOversizeFailures()
        {
            using (var fixture = new LogSearchServerFixture())
            {
                fixture.InsertRowsAscending(2);
                var gm = fixture.AddGm("Gm", 1);
                var viewer = fixture.OpenViewer(gm);
                fixture.Monotonic = 0;

                fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1,
                    participant: "Bob", mapId: 5, typeIds: new[] { 14, 0 }, text: "hi"));
                fixture.Pump();

                Assert.Equal(1, fixture.ViewLogsAuditCount);
                Log audit = fixture.GameWorld.LogHandler.Pending.Single(l => l.Type == Log.Types.ViewLogs);
                Assert.Equal(1, audit.PlayerID);
                Assert.Equal(7, audit.MapID);
                Assert.Equal(3, audit.MapX);
                Assert.Equal(4, audit.MapY);
                Assert.Equal(
                    "{\"startUtcMilliseconds\":1700000000000,\"endUtcMilliseconds\":1700003600000," +
                    "\"participant\":\"Bob\",\"mapId\":5,\"eventIds\":[0,14]," +
                    "\"groups\":[\"Communication\",\"Other/Retired\"],\"text\":\"hi\"}",
                    audit.Text);
            }

            using (var fixture = new LogSearchServerFixture())
            {
                var gm = fixture.AddGm("Gm", 1);
                var viewer = fixture.OpenViewer(gm);
                fixture.Monotonic = 0;

                fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1, startMs: 10, endMs: 5));
                fixture.Pump();

                Assert.Equal("Start must be before end.", fixture.LrxMessage(gm, viewer.ID, 1));
                Assert.Equal(1, fixture.ViewLogsAuditCount);
            }

            using (var fixture = new LogSearchServerFixture(createPlayersTable: false))
            {
                var gm = fixture.AddGm("Gm", 1);
                var viewer = fixture.OpenViewer(gm);
                fixture.Monotonic = 0;

                fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1, participant: "Nobody"));
                fixture.Pump();

                Assert.Equal("Log search failed.", fixture.LrxMessage(gm, viewer.ID, 1));
                Assert.Equal(1, fixture.ViewLogsAuditCount);
            }

            using (var fixture = new LogSearchServerFixture())
            {
                var gm = fixture.AddGm("Gm", 1);
                var viewer = fixture.OpenViewer(gm);
                fixture.Monotonic = 0;
                var input = new LogFreshSearchInput { StartUtcMilliseconds = 0, EndUtcMilliseconds = 9 };

                viewer.BeginQuery(77);
                fixture.Service.ReserveDbSlot();
                fixture.Service.RecordFreshAdmission(gm, input);
                fixture.Service.CompleteFresh(gm, viewer, 77, viewer.SessionGeneration, CursorlessBase(),
                    new LogSearchPage(new[] { RowWithText(new string('x', 300_000)) }, false, null));
                fixture.GameWorld.Update();

                Assert.Equal("Result is too large to display. Narrow the search.", fixture.LastLrxMessage(gm));
                Assert.Equal(1, fixture.ViewLogsAuditCount);
                Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
                Assert.Null(viewer.Session);
            }
        }

        [Fact]
        public void PageAndEveryRejectedPath_AppendNoAudit()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(60);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            Assert.Equal(1, fixture.ViewLogsAuditCount);
            string next = fixture.LastLrf(gm)!.Next;

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, next));
            fixture.Pump();
            Assert.Equal(1, fixture.ViewLogsAuditCount);

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 3, LogPageTokenCodec.Create()));
            Assert.Equal(1, fixture.ViewLogsAuditCount);

            fixture.Monotonic += fixture.MillisecondTicks * 999;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 4));
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 5));
            Assert.Equal(1, fixture.ViewLogsAuditCount);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
        }

        [Fact]
        public void PendingAudit_IsAbsentFromTheDeliveredFreshResult()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(2);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1, participant: "AuditProbe", text: "needle"));
            fixture.Pump();

            Log audit = fixture.GameWorld.LogHandler.Pending.Single(l => l.Type == Log.Types.ViewLogs);
            Assert.Equal(1, fixture.ViewLogsAuditCount);
            Assert.DoesNotContain(gm.Sent, s => s.Contains(audit.Text));
            Assert.DoesNotContain(fixture.LastRowJsons(gm), json => json.Contains(audit.Text));
        }

        [Fact]
        public void MalformedIdentifiableCurrentRequest_ReceivesMalformedLrx_StaleIdentityIsSilent()
        {
            using var fixture = new LogSearchServerFixture();
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);

            fixture.Send(gm, $"LQS{viewer.ID},5,P,short");
            Assert.Equal("Malformed log search request.", fixture.LrxMessage(gm, viewer.ID, 5));
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            fixture.Send(gm, "LQS999,5,P,short");
            Assert.Equal(1, gm.Sent.Count(s => s.StartsWith("LRX")));

            new WindowButtonClickEvent { Player = gm, Data = $"WBC2,{viewer.ID},0,0,0" }.Ready(fixture.GameWorld);
            var replacement = fixture.OpenViewer(gm);

            fixture.Send(gm, $"LQS{viewer.ID},5,P,short");
            Assert.Equal(1, gm.Sent.Count(s => s.StartsWith("LRX")));

            fixture.Send(gm, $"LQS{replacement.ID},5,P,short");
            Assert.Equal(2, gm.Sent.Count(s => s.StartsWith("LRX")));
            Assert.Equal("Malformed log search request.", fixture.LrxMessage(gm, replacement.ID, 5));
        }

        [Fact]
        public void MalformedIdentifiableWhileActive_ReceivesBusyMessage_NoMutation()
        {
            using var fixture = new LogSearchServerFixture();
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            var block = new ManualResetEventSlim(false);
            fixture.GameWorld.Database.Enqueue(conn => block.Wait());
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);
            Assert.Equal(1, fixture.ViewLogsAuditCount);

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm, $"LQS{viewer.ID},1,F,1,2");
            Assert.Equal("A log search is already running.", fixture.LrxMessage(gm, viewer.ID, 1));
            Assert.Null(viewer.Session);
            Assert.Equal(1, fixture.ViewLogsAuditCount);
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);

            block.Set();
            fixture.Pump();
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2));
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            fixture.Pump();
        }

        [Fact]
        public void UnknownCanonicalPageToken_RejectsBeforeDbCapacityAuditAndRate()
        {
            using var fixture = new LogSearchServerFixture();
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 1, LogPageTokenCodec.Create()));

            Assert.Equal("Log page token is invalid or expired.", fixture.LrxMessage(gm, viewer.ID, 1));
            Assert.Null(viewer.Session);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            Assert.Equal(0, fixture.ViewLogsAuditCount);
            fixture.GameWorld.Database.Execute(conn => { });
            Assert.Equal(0, fixture.GameWorld.Database.PendingCount);

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2));
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            fixture.Pump();
        }
    }
}
