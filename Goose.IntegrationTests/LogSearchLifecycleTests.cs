using System.Collections.Immutable;
using System.Data;
using System.Data.SQLite;
using Goose;
using Goose.Events;
using Goose.Logs;
using Goose.Testing;
using Xunit;

namespace Goose.IntegrationTests
{
    public class LogSearchLifecycleTests
    {
        private static string BigText(int index, int size)
            => "row" + index + "-" + new string('a', size);

        private static long Ticks(int i)
            => LogSearchServerFixture.ToTicks(LogSearchServerFixture.StartMs + 1_000)
                + (i + 1) * TimeSpan.TicksPerSecond;

        private static LogSearchServerFixture WithPartialDelivery(
            out TestWorldFixture.CapturingPlayer gm, out LogViewerWindow viewer, out int sentBefore)
        {
            var fixture = new LogSearchServerFixture(logoutLagTime: 10_000);
            for (int i = 0; i < 3; i++)
                fixture.InsertLog(Ticks(i), 0, 0, 0, 0, BigText(i, 100_000));
            gm = fixture.AddGm("Gm", 1);
            viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;
            int id = viewer.ID;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();
            fixture.GameWorld.Update();
            Assert.Contains(gm.Sent, s => s.StartsWith($"LRB{id},1"));
            Assert.Contains(gm.Sent, s => s.StartsWith($"LRD{id},1"));
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRF{id},1"));
            sentBefore = gm.Sent.Count;
            return fixture;
        }

        [Fact]
        public void WbcCloseDuringQuerying_ClearsState_CompletionReleasesCapacityAndSendsNothing()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            var block = new ManualResetEventSlim(false);
            fixture.GameWorld.Database.Enqueue(conn => block.Wait());
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);

            new WindowButtonClickEvent { Player = gm, Data = $"WBC2,{viewer.ID},0,0,0" }.Ready(fixture.GameWorld);
            Assert.DoesNotContain(gm.Windows, w => w == viewer);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Null(viewer.Session);
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);

            block.Set();
            fixture.Pump();

            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            Assert.Empty(gm.Sent.Where(s => s.StartsWith("LR")));
        }

        [Fact]
        public void WbcCloseAfterPartialLrd_ClearsDeliveryAndSession_CompletionSendsNothingFurther()
        {
            using var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent);

            new WindowButtonClickEvent { Player = gm, Data = $"WBC2,{viewer.ID},0,0,0" }.Ready(fixture.GameWorld);
            Assert.DoesNotContain(gm.Windows, w => w == viewer);
            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            fixture.Pump();
            Assert.Equal(sent, gm.Sent.Count);
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRF{viewer.ID},1"));
            Assert.Equal(0, fixture.Service.PendingDeliveryCount);
        }

        [Fact]
        public void LogsReplacementInvalidatesOldQueryDeliveryAndTokens_EvenWithReusedRequestId()
        {
            using var fixture = new LogSearchServerFixture();
            for (int i = 0; i < 3; i++)
                fixture.InsertLog(Ticks(i), 0, 0, 0, 0, BigText(i, 100_000));
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 7));
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();
            fixture.GameWorld.Update();
            Assert.Contains(gm.Sent, s => s.StartsWith($"LRB{viewer.ID},7"));
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRF{viewer.ID},7"));
            string oldToken = viewer.Session!.FirstPageToken;

            new WindowButtonClickEvent { Player = gm, Data = $"WBC2,{viewer.ID},0,0,0" }.Ready(fixture.GameWorld);
            var replacement = fixture.OpenViewer(gm);
            Assert.NotSame(viewer, replacement);
            Assert.Null(viewer.Session);
            int sent = gm.Sent.Count;

            fixture.Pump();
            Assert.Equal(sent, gm.Sent.Count);
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRF{viewer.ID},7"));
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LR{replacement.ID},"));

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm, LogSearchServerFixture.Fresh(replacement.ID, 7));
            fixture.Pump();
            Assert.Contains(gm.Sent, s => s.StartsWith($"LRF{replacement.ID},7"));

            fixture.Send(gm, LogSearchServerFixture.Page(replacement.ID, 8, oldToken));
            Assert.Equal("Log page token is invalid or expired.", fixture.LrxMessage(gm, replacement.ID, 8));
        }

        [Fact]
        public void InGameSetAccessDowngrade_ClosesViewerAndClearsDeliveryImmediately()
        {
            using var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent);
            fixture.World.RegisterDatabasePlayer(gm);

            Assert.True(fixture.World.RunCommand(gm, "/setaccess Gm Normal"));
            Assert.Equal(Player.AccessStatus.Normal, gm.Access);
            Assert.Empty(gm.Windows.OfType<LogViewerWindow>());
            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Contains(gm.Sent.Skip(sent), s => s.StartsWith("CLW" + viewer.ID));

            fixture.Pump();
            Assert.DoesNotContain(gm.Sent.Skip(sent), s => s.StartsWith("LR"));
            Assert.Equal(0, fixture.Service.PendingDeliveryCount);
        }

        [Fact]
        public void ConsoleSetAccessDowngrade_ClosesViewerAndClearsDeliveryImmediately()
        {
            using var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent);
            fixture.World.RegisterDatabasePlayer(gm);

            Goose.ConsoleCommands.SetAccessCommand.Run(fixture.GameWorld, new[] { "Gm", "Normal" });
            Assert.Equal(Player.AccessStatus.Normal, gm.Access);
            Assert.Empty(gm.Windows.OfType<LogViewerWindow>());
            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Contains(gm.Sent.Skip(sent), s => s.StartsWith("CLW" + viewer.ID));

            fixture.Pump();
            Assert.DoesNotContain(gm.Sent.Skip(sent), s => s.StartsWith("LR"));
            Assert.Equal(0, fixture.Service.PendingDeliveryCount);
        }

        [Fact]
        public void DirectAccessMutationIsCaughtBeforeTheNextPacketSend()
        {
            using var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent);

            gm.Access = Player.AccessStatus.Normal;
            fixture.GameWorld.Update();

            Assert.Equal(sent, gm.Sent.Count);
            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Equal(0, fixture.Service.PendingDeliveryCount);
        }

        [Fact]
        public void RevokeAfterLrbBeforeRemainingChunks_NeverSendsLrf_StagedRowsCannotCommit()
        {
            using var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent);

            gm.Access = Player.AccessStatus.Normal;
            fixture.GameWorld.Update();

            Assert.Equal(sent, gm.Sent.Count);
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRF{viewer.ID},1"));
            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
        }

        [Fact]
        public void LostConnectionResolvesPlayerBeforeRemoval_InvalidatesWithoutSends_DelayedLogoutCannotDeliver()
        {
            using var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent);

            fixture.GameWorld.LostConnection(gm.Sock);

            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Equal(sent, gm.Sent.Count);
            Assert.Same(gm, fixture.GameWorld.PlayerHandler.GetPlayer(gm.Sock));

            fixture.GameWorld.LostConnection(gm.Sock);
            Assert.Same(gm, fixture.GameWorld.PlayerHandler.GetPlayer(gm.Sock));

            fixture.Pump();
            Assert.Equal(sent, gm.Sent.Count);
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRF{viewer.ID},1"));
            Assert.Equal(0, fixture.Service.PendingDeliveryCount);

            fixture.GameWorld.PlayerHandler.RemovePlayer(gm);
            fixture.GameWorld.Update();
            Assert.Equal(sent, gm.Sent.Count);
        }

        [Fact]
        public void StaleViewerStatesDiscardDelivery()
        {
            using (var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent))
            {
                gm.State = Player.States.NotLoggedIn;
                fixture.GameWorld.Update();
                Assert.Equal(sent, gm.Sent.Count);
                Assert.Null(viewer.Session);
                Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            }

            using (var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent))
            {
                fixture.GameWorld.PlayerHandler.RemovePlayer(gm);
                fixture.GameWorld.Update();
                Assert.Equal(sent, gm.Sent.Count);
                Assert.Null(viewer.Session);
                Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            }

            using (var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent))
            {
                viewer.BeginQuery(99);
                fixture.GameWorld.Update();
                Assert.Equal(sent, gm.Sent.Count);
                Assert.NotNull(viewer.Session);
                Assert.Null(viewer.DeliveryPackets);
                Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            }

            using (var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent))
            {
                viewer.NextSessionGeneration();
                fixture.GameWorld.Update();
                Assert.Equal(sent, gm.Sent.Count);
                Assert.NotNull(viewer.Session);
                Assert.Null(viewer.DeliveryPackets);
                Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            }

            using (var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent))
            {
                new WindowButtonClickEvent { Player = gm, Data = $"WBC2,{viewer.ID},0,0,0" }.Ready(fixture.GameWorld);
                var replacement = fixture.OpenViewer(gm);
                fixture.Pump();
                Assert.DoesNotContain(gm.Sent.Skip(sent), s => s.StartsWith("LR"));
                Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRF{viewer.ID},1"));
                Assert.Null(viewer.Session);
                Assert.Equal(0, fixture.Service.PendingDeliveryCount);
            }
        }

        [Fact]
        public void CloseRevokeDisconnectNeverReleaseCapacityEarly_StaleCompletionsReleaseExactlyOnce()
        {
            using var fixture = new LogSearchServerFixture(logoutLagTime: 10_000);
            fixture.InsertRowsAscending(3);
            var gms = new[]
            {
                fixture.AddGm("A", 1), fixture.AddGm("B", 2), fixture.AddGm("C", 3),
                fixture.AddGm("D", 4),
            };
            var viewers = gms.Select(g => fixture.OpenViewer(g)).ToArray();
            fixture.Monotonic = 0;
            var block = new ManualResetEventSlim(false);
            fixture.GameWorld.Database.Enqueue(conn => block.Wait());
            for (int i = 0; i < 4; i++)
                fixture.GameWorld.EventHandler.AddEvent(gms[i], LogSearchServerFixture.Fresh(viewers[i].ID, 1));
            fixture.GameWorld.Update();
            Assert.Equal(4, fixture.Service.ActiveDbQueryCount);

            new WindowButtonClickEvent { Player = gms[0], Data = $"WBC2,{viewers[0].ID},0,0,0" }.Ready(fixture.GameWorld);
            gms[1].Access = Player.AccessStatus.Normal;
            fixture.GameWorld.LogSearches.OnAccessChanged(gms[1]);
            fixture.GameWorld.LostConnection(gms[2].Sock);

            Assert.Equal(4, fixture.Service.ActiveDbQueryCount);

            block.Set();
            fixture.Pump();
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            fixture.GameWorld.Update();
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);

            Assert.DoesNotContain(gms[0].Sent, s => s.StartsWith("LR"));
            Assert.DoesNotContain(gms[1].Sent, s => s.StartsWith("LRB"));
            Assert.DoesNotContain(gms[2].Sent, s => s.StartsWith("LR"));
            Assert.Contains(gms[3].Sent, s => s.StartsWith($"LRF{viewers[3].ID},1"));
        }

        private sealed class FailingPlayer : Player
        {
            public FailingPlayer() : base(0) { }
            public List<string> Sent { get; } = new();
            public int FailAfterLogPackets;
            private int logPackets;

            public override bool Send(string data)
            {
                if (data.StartsWith("LR"))
                {
                    this.logPackets++;
                    if (this.FailAfterLogPackets > 0 && this.logPackets > this.FailAfterLogPackets)
                        return false;
                }
                this.Sent.Add(data);
                return true;
            }
        }

        [Fact]
        public void SendFailureInvalidatesPlayerWithoutSends_StopsRemainingPackets_OtherViewersUnaffected()
        {
            using var fixture = new LogSearchServerFixture(logoutLagTime: 10_000);
            for (int i = 0; i < 3; i++)
                fixture.InsertLog(Ticks(i), 0, 0, 0, 0, BigText(i, 100_000));
            long bBase = LogSearchServerFixture.ToTicks(LogSearchServerFixture.StartMs + 4_000_000);
            fixture.InsertLog(bBase + TimeSpan.TicksPerSecond, 0, 0, 0, 0, "small");

            var failing = new FailingPlayer { Name = "Fail", FailAfterLogPackets = 3 };
            var failingGm = fixture.AddGm(failing, 1);
            var viewer = fixture.OpenViewer(failingGm);
            var other = fixture.AddGm("Other", 2);
            var otherViewer = fixture.OpenViewer(other);
            fixture.Monotonic = 0;

            fixture.GameWorld.EventHandler.AddEvent(failingGm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.EventHandler.AddEvent(other, LogSearchServerFixture.Fresh(otherViewer.ID, 1,
                startMs: LogSearchServerFixture.StartMs + 4_000_000,
                endMs: LogSearchServerFixture.StartMs + 4_000_000 + 3_600_000));
            fixture.GameWorld.Update();
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();

            int guard = 0;
            while (failingGm.Sent.Count(s => s.StartsWith("LR")) < 3 && guard++ < 50)
                fixture.GameWorld.Update();
            Assert.Equal(3, failingGm.Sent.Count(s => s.StartsWith("LR")));
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            fixture.GameWorld.Update();
            Assert.Equal(3, failingGm.Sent.Count(s => s.StartsWith("LR")));
            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.DoesNotContain(failingGm.Sent, s => s.StartsWith("LRF"));

            fixture.Pump();
            Assert.Contains(other.Sent, s => s.StartsWith($"LRF{otherViewer.ID},1"));
            Assert.Equal(LogSearchPhase.Idle, otherViewer.Phase);
        }

        [Fact]
        public void StopDuringQuerying_ClearsQueues_LateDbCallbackRejectedEvenIfDbStopTimedOut()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            var block = new ManualResetEventSlim(false);
            fixture.GameWorld.Database.Enqueue(conn => block.Wait());
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);

            fixture.GameWorld.BeginStopping();
            Assert.Equal(0, fixture.GameWorld.PendingCompletionCount);
            Assert.Equal(0, fixture.Service.PendingDeliveryCount);

            block.Set();
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();

            Assert.Empty(gm.Sent.Where(s => s.StartsWith("LR")));
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);
            Assert.Equal(0, fixture.GameWorld.PendingCompletionCount);
        }

        [Fact]
        public void StopDuringDelivering_ClearsDeliveriesAndSessions_SendsNothing()
        {
            using var fixture = WithPartialDelivery(out var gm, out var viewer, out var sent);

            fixture.GameWorld.BeginStopping();
            Assert.Equal(0, fixture.Service.PendingDeliveryCount);
            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            fixture.GameWorld.Update();
            Assert.Equal(sent, gm.Sent.Count);
        }

        [Fact]
        public void InvalidatePlayerBetweenAdmissionAndCompletion_StaleFreshCompletionCannotAttachToReusedRequestId()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            var block = new ManualResetEventSlim(false);
            fixture.GameWorld.Database.Enqueue(conn => block.Wait());
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 5));
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);

            fixture.Service.InvalidatePlayer(gm);
            Assert.Null(viewer.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 5, text: "row 2"));
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            Assert.Equal(2, fixture.Service.ActiveDbQueryCount);

            block.Set();
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();
            fixture.Pump();

            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            Assert.Contains(gm.Sent, s => s.StartsWith($"LRF{viewer.ID},5"));
            List<string> rows = fixture.LastRowJsons(gm);
            Assert.Single(rows);
            Assert.Contains("row 2", rows[0]);
            Assert.DoesNotContain(rows, r => r.Contains("row 1"));
            Assert.NotNull(viewer.Session);
            Assert.Equal(2, viewer.Session.Generation);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
        }

        [Fact]
        public void BeginStopping_ClearsQueryingAndIdleCommittedViewers_NoSends()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(3);
            var gm1 = fixture.AddGm("Gm1", 1);
            var viewer1 = fixture.OpenViewer(gm1);
            var gm2 = fixture.AddGm("Gm2", 2);
            var viewer2 = fixture.OpenViewer(gm2);
            fixture.Monotonic = 0;

            fixture.Send(gm2, LogSearchServerFixture.Fresh(viewer2.ID, 1));
            fixture.Pump();
            Assert.Contains(gm2.Sent, s => s.StartsWith($"LRF{viewer2.ID},1"));
            Assert.NotNull(viewer2.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer2.Phase);

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            var block = new ManualResetEventSlim(false);
            fixture.GameWorld.Database.Enqueue(conn => block.Wait());
            fixture.GameWorld.EventHandler.AddEvent(gm1, LogSearchServerFixture.Fresh(viewer1.ID, 1));
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Querying, viewer1.Phase);
            Assert.Equal(1, fixture.Service.ActiveDbQueryCount);

            int sent1 = gm1.Sent.Count;
            int sent2 = gm2.Sent.Count;

            fixture.GameWorld.BeginStopping();

            Assert.Null(viewer1.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer1.Phase);
            Assert.Null(viewer2.Session);
            Assert.Equal(LogSearchPhase.Idle, viewer2.Phase);
            Assert.Equal(0, fixture.GameWorld.PendingCompletionCount);
            Assert.Equal(0, fixture.Service.PendingDeliveryCount);
            Assert.Equal(sent1, gm1.Sent.Count);
            Assert.Equal(sent2, gm2.Sent.Count);

            block.Set();
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();

            Assert.DoesNotContain(gm1.Sent, s => s.StartsWith("LR"));
            Assert.Equal(sent2, gm2.Sent.Count);
        }

        [Fact]
        public void AdmittedFreshAuditSavedByShutdownPersistence_PAndRejectedContributeNone()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertPlayer(5, "Bob");
            fixture.InsertRowsAscending(3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1, participant: "Bob", text: "needle"));
            fixture.Pump();
            Assert.Equal(1, fixture.ViewLogsAuditCount);
            string audit = fixture.GameWorld.LogHandler.Pending.Single(l => l.Type == Log.Types.ViewLogs).Text;

            fixture.GameWorld.LogHandler.Save(fixture.GameWorld);
            fixture.GameWorld.Database.Execute(conn => { });
            int saved = fixture.GameWorld.Database.Execute<int>(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM logs WHERE log_type = @t AND text = @text;";
                command.Parameters.Add(new SQLiteParameter("@t", DbType.Int32) { Value = (int)Log.Types.ViewLogs });
                command.Parameters.Add(new SQLiteParameter("@text", DbType.String) { Value = audit });
                return Convert.ToInt32(command.ExecuteScalar());
            });
            Assert.Equal(1, saved);
            Assert.Empty(fixture.GameWorld.LogHandler.Pending);

            string current = fixture.LastLrf(gm)!.Current;
            fixture.Send(gm, LogSearchServerFixture.Page(viewer.ID, 2, current));
            fixture.Pump();
            fixture.Monotonic += fixture.MillisecondTicks * 999;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 3));

            Assert.Empty(fixture.GameWorld.LogHandler.Pending);
            int total = fixture.GameWorld.Database.Execute<int>(conn =>
            {
                using var command = conn.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM logs WHERE log_type = @t;";
                command.Parameters.Add(new SQLiteParameter("@t", DbType.Int32) { Value = (int)Log.Types.ViewLogs });
                return Convert.ToInt32(command.ExecuteScalar());
            });
            Assert.Equal(1, total);
        }
    }
}
