using System.Collections.Immutable;
using System.Text.Json;
using Goose;
using Goose.Logs;
using Xunit;

namespace Goose.IntegrationTests
{
    public class LogSearchDeliveryTests
    {
        private static string BigText(int index, int size)
            => "row" + index + "-" + new string('a', size);

        private static long Ticks(int i)
            => LogSearchServerFixture.ToTicks(LogSearchServerFixture.StartMs + 1_000)
                + (i + 1) * TimeSpan.TicksPerSecond;

        private static void InsertBigRows(LogSearchServerFixture fixture, int count, int size = 100_000)
        {
            for (int i = 0; i < count; i++)
                fixture.InsertLog(Ticks(i), 0, 0, 0, 0, BigText(i, size));
        }

        private static LogQueryRow RowWithText(string text)
            => new(1, 123, 0, true, 0, false, 0, false, 0, false, 1, true, 2, true,
                "Chat", "Communication", LogOtherIdKind.Unused, null, null, null, "s", text);

        private static LogSearchQuery CursorlessBase()
            => new(1000, 2000, null, null, ImmutableArray<int>.Empty, "", null);

        [Fact]
        public void NothingIsSentBetweenDbBarrierAndUpdate()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.GameWorld.EventHandler.AddEvent(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            fixture.GameWorld.Database.Execute(conn => { });

            Assert.True(fixture.GameWorld.PendingCompletionCount >= 1);
            Assert.Empty(gm.Sent.Where(s => s.StartsWith("LR")));

            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);
            Assert.Empty(gm.Sent.Where(s => s.StartsWith("LR")));

            fixture.Pump();
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Contains(gm.Sent, s => s.StartsWith($"LRB{viewer.ID},1"));
            Assert.NotNull(fixture.LastLrf(gm));
        }

        [Fact]
        public void OneUpdateSendsAtMostBudgetBytes_LargeResponseKeepsQueryingDeliveringIdlePhases()
        {
            using var fixture = new LogSearchServerFixture();
            InsertBigRows(fixture, 5);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            Assert.Equal(LogSearchPhase.Querying, viewer.Phase);
            fixture.GameWorld.Database.Execute(conn => { });

            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);
            Assert.Empty(gm.Sent.Where(s => s.StartsWith("LR")));

            int sentBefore = gm.Sent.Count;
            fixture.GameWorld.Update();
            int bytes = gm.Sent.Skip(sentBefore).Sum(s => s.Length + 1);
            Assert.True(bytes > 0);
            Assert.True(bytes <= LogSearchService.DeliveryBudgetBytes, $"update sent {bytes} bytes");
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            int guard = 0;
            while (viewer.Phase == LogSearchPhase.Delivering && guard++ < 100)
            {
                sentBefore = gm.Sent.Count;
                fixture.GameWorld.Update();
                bytes = gm.Sent.Skip(sentBefore).Sum(s => s.Length + 1);
                Assert.True(bytes <= LogSearchService.DeliveryBudgetBytes, $"update sent {bytes} bytes");
            }

            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Equal(5, fixture.LastRowJsons(gm).Count);
            var lrf = fixture.LastLrf(gm)!;
            Assert.False(lrf.HasMore);
            Assert.Equal("", lrf.Next);
        }

        [Fact]
        public void LargeRowsProduceOrderedBoundedLrdSegments_ExactTextReconstructs()
        {
            using var fixture = new LogSearchServerFixture();
            string[] texts = [BigText(1, 120_000), BigText(2, 120_000), BigText(3, 120_000)];
            for (int i = 0; i < texts.Length; i++)
                fixture.InsertLog(Ticks(i), 0, 0, 0, 0, texts[i]);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();

            var segments = gm.Sent.Where(s => s.StartsWith("LRD")).ToList();
            Assert.True(segments.Count > texts.Length);
            Assert.All(segments, s =>
            {
                string payload = s.TrimEnd(LogProtocolPackets.PacketDelimiter).Split(',')[5];
                Assert.True(payload.Length <= LogProtocolPackets.MaxSegmentLength, payload.Length.ToString());
            });

            int firstRowChunks = segments.Count(s =>
                s.TrimEnd(LogProtocolPackets.PacketDelimiter).Split(',')[2] == "0");
            Assert.True(firstRowChunks > 1);
            var chunkOrder = segments
                .Where(s => s.TrimEnd(LogProtocolPackets.PacketDelimiter).Split(',')[2] == "0")
                .Select(s => int.Parse(s.TrimEnd(LogProtocolPackets.PacketDelimiter).Split(',')[3]))
                .ToList();
            Assert.Equal(chunkOrder.OrderBy(x => x), chunkOrder);

            var jsons = fixture.LastRowJsons(gm);
            Assert.Equal(3, jsons.Count);
            for (int i = 0; i < 3; i++)
            {
                using var doc = JsonDocument.Parse(jsons[i]);
                Assert.Equal(texts[2 - i], doc.RootElement.GetProperty("originalText").GetString());
            }
        }

        private static string TextForJsonLength(int target)
        {
            var probe = RowWithText(new string('a', 1_000));
            Assert.True(LogRowJsonSerializer.TrySerialize(probe, out byte[] json));
            int textLength = 1_000 + target - json.Length;
            Assert.True(textLength > 0);
            return new string('a', textLength);
        }

        [Fact]
        public void OneByteOverRowAndOneByteOverResponse_SendOnlySafeLrx_PreservePriorSession()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();
            var session = viewer.Session!;

            string overText = TextForJsonLength(LogRowJsonSerializer.MaxSerializedBytes + 1);
            Assert.True(LogRowJsonSerializer.TrySerialize(RowWithText(overText[..^1]), out byte[] under));
            Assert.Equal(LogRowJsonSerializer.MaxSerializedBytes, under.Length);
            Assert.False(LogRowJsonSerializer.TrySerialize(RowWithText(overText), out _));

            viewer.BeginQuery(10);
            fixture.Service.ReserveDbSlot();
            fixture.Service.CompleteFresh(gm, viewer, 10, viewer.SessionGeneration, CursorlessBase(),
                new LogSearchPage(new[] { RowWithText(overText) }, false, null));
            fixture.GameWorld.Update();

            Assert.Equal("Result is too large to display. Narrow the search.", fixture.LrxMessage(gm, viewer.ID, 10));
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRB{viewer.ID},10,"));
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRD{viewer.ID},10,"));
            Assert.Same(session, viewer.Session);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);

            var overRows = Enumerable.Range(0, 17).Select(_ => RowWithText(TextForJsonLength(250_000))).ToArray();
            viewer.BeginQuery(11);
            fixture.Service.ReserveDbSlot();
            fixture.Service.CompleteFresh(gm, viewer, 11, viewer.SessionGeneration, CursorlessBase(),
                new LogSearchPage(overRows, true, new LogPageCursor(5, 9, 8)));
            fixture.GameWorld.Update();

            Assert.Equal("Result is too large to display. Narrow the search.", fixture.LrxMessage(gm, viewer.ID, 11));
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRB{viewer.ID},11,"));
            Assert.Same(session, viewer.Session);
            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
        }

        [Fact]
        public void EmptyPageSendsLrbThenLrf_NonemptyCurrentTokenAndEmptyNext()
        {
            using var fixture = new LogSearchServerFixture();
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();

            var lrf = fixture.LastLrf(gm)!;
            Assert.False(lrf.HasMore);
            Assert.NotEqual("", lrf.Current);
            Assert.True(LogPageTokenCodec.IsCanonical(lrf.Current));
            Assert.Equal("", lrf.Next);
            Assert.Equal(1, gm.Sent.Count(s => s.StartsWith($"LRB{viewer.ID},1")));
            Assert.Equal(1, gm.Sent.Count(s => s.StartsWith($"LRF{viewer.ID},1")));
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith("LRD"));
        }

        [Fact]
        public void FiftyOneRowPageEmitsFiftyRowsAndLrfTokens_UnknownInt64TypeSurvives()
        {
            using var fixture = new LogSearchServerFixture();
            long baseTicks = LogSearchServerFixture.ToTicks(LogSearchServerFixture.StartMs + 1_000);
            for (int i = 1; i <= 50; i++)
                fixture.InsertLog(baseTicks + i * TimeSpan.TicksPerSecond, 0, 0, 0, 0, "row " + i);
            fixture.InsertLog(baseTicks + 51 * TimeSpan.TicksPerSecond, -123, 0, 0, 0, "mystery");
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.Pump();

            var jsons = fixture.LastRowJsons(gm);
            Assert.Equal(50, jsons.Count);
            var lrf = fixture.LastLrf(gm)!;
            Assert.True(lrf.HasMore);
            Assert.True(LogPageTokenCodec.IsCanonical(lrf.Current));
            Assert.True(LogPageTokenCodec.IsCanonical(lrf.Next));
            Assert.NotEqual(lrf.Current, lrf.Next);

            string mystery = jsons.Single(j => j.Contains("mystery"));
            using var doc = JsonDocument.Parse(mystery);
            Assert.Equal(-123, doc.RootElement.GetProperty("typeId").GetInt64());
            Assert.True(doc.RootElement.GetProperty("typeIsInteger").GetBoolean());
            Assert.Equal("Unknown event", doc.RootElement.GetProperty("eventLabel").GetString());
        }

        private sealed class CapturingNLogTarget : NLog.Targets.TargetWithLayout
        {
            public List<NLog.LogEventInfo> Events { get; } = [];

            protected override void Write(NLog.LogEventInfo logEvent) => this.Events.Add(logEvent);
        }

        [Fact]
        public void ValidationAndDbErrorsEmitPacedLrxOnly_NlogRetainsFullDbException()
        {
            using (var fixture = new LogSearchServerFixture())
            {
                var gm = fixture.AddGm("Gm", 1);
                var viewer = fixture.OpenViewer(gm);
                fixture.Monotonic = 0;

                fixture.GameWorld.EventHandler.AddEvent(gm,
                    LogSearchServerFixture.Fresh(viewer.ID, 1, startMs: 10, endMs: 5));
                fixture.GameWorld.Update();
                fixture.GameWorld.Database.Execute(conn => { });
                Assert.DoesNotContain(gm.Sent, s => s.StartsWith("LR"));
                fixture.Pump();

                Assert.Equal("Start must be before end.", fixture.LrxMessage(gm, viewer.ID, 1));
                Assert.DoesNotContain(gm.Sent, s => s.StartsWith("LRB"));
            }

            using (var fixture = new LogSearchServerFixture(createPlayersTable: false))
            {
                var previous = NLog.LogManager.Configuration;
                var target = new CapturingNLogTarget();
                var config = new NLog.Config.LoggingConfiguration();
                config.AddTarget("mem", target);
                config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, target);
                NLog.LogManager.Configuration = config;
                try
                {
                    var gm = fixture.AddGm("Gm", 1);
                    var viewer = fixture.OpenViewer(gm);
                    fixture.Monotonic = 0;

                    fixture.GameWorld.EventHandler.AddEvent(gm,
                        LogSearchServerFixture.Fresh(viewer.ID, 1, participant: "Nobody"));
                    fixture.GameWorld.Update();
                    fixture.GameWorld.Database.Execute(conn => { });
                    Assert.DoesNotContain(gm.Sent, s => s.StartsWith("LR"));
                    fixture.Pump();

                    Assert.Equal("Log search failed.", fixture.LrxMessage(gm, viewer.ID, 1));
                    Assert.DoesNotContain(gm.Sent, s => s.StartsWith("LRB"));
                    Assert.DoesNotContain(gm.Sent, s => s.Contains("no such table"));
                    Assert.Contains(target.Events, e =>
                        e.Level == NLog.LogLevel.Error
                        && e.Exception is not null
                        && e.Exception.Message.Contains("no such table"));
                }
                finally
                {
                    NLog.LogManager.Configuration = previous;
                }
            }
        }

        [Fact]
        public void SendBufferAboveWatermarkPausesDelivery_ResumesAtOrBelowWatermark()
        {
            using var fixture = new LogSearchServerFixture();
            fixture.InsertRowsAscending(3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            fixture.SetSendBufferCount(gm, LogSearchService.SendBufferWatermark + 1);
            for (int i = 0; i < 5; i++)
            {
                fixture.GameWorld.Update();
                Assert.Empty(gm.Sent.Where(s => s.StartsWith("LR")));
            }
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);
            Assert.Equal(1, fixture.Service.PendingDeliveryCount);

            fixture.SetSendBufferCount(gm, LogSearchService.SendBufferWatermark);
            fixture.Pump();
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.NotNull(fixture.LastLrf(gm));
        }

        [Fact]
        public void PermanentlySlowSocketSendsNothingAndNeverApproachesDisconnectCeiling()
        {
            using var fixture = new LogSearchServerFixture();
            InsertBigRows(fixture, 5);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();

            fixture.SetSendBufferCount(gm, LogSearchService.SendBufferWatermark + 1);
            for (int i = 0; i < 20; i++)
                fixture.GameWorld.Update();

            Assert.Empty(gm.Sent.Where(s => s.StartsWith("LR")));
            Assert.Equal(LogSearchService.SendBufferWatermark + 1, gm.SendBuffer.Count);
            Assert.True(gm.SendBuffer.Count < Player.MaxSendBufferSize);
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            fixture.SetSendBufferCount(gm, 0);
            fixture.Pump();
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
            Assert.Equal(5, fixture.LastRowJsons(gm).Count);
        }

        [Fact]
        public void TwoViewersRoundRobin_PausedViewerDoesNotStarveOther()
        {
            using var fixture = new LogSearchServerFixture();
            long baseTicks = LogSearchServerFixture.ToTicks(LogSearchServerFixture.StartMs + 1_000);
            for (int i = 1; i <= 3; i++)
                fixture.InsertLog(baseTicks + i * TimeSpan.TicksPerSecond, 0, 0, 0, 0, BigText(i, 100_000));
            long bBase = LogSearchServerFixture.ToTicks(LogSearchServerFixture.StartMs + 4_000_000);
            for (int i = 1; i <= 2; i++)
                fixture.InsertLog(bBase + i * TimeSpan.TicksPerSecond, 0, 0, 0, 0, "small " + i);

            var gmA = fixture.AddGm("A", 1);
            var viewerA = fixture.OpenViewer(gmA);
            var gmB = fixture.AddGm("B", 2);
            var viewerB = fixture.OpenViewer(gmB);
            fixture.Monotonic = 0;

            fixture.GameWorld.EventHandler.AddEvent(gmA, LogSearchServerFixture.Fresh(viewerA.ID, 1));
            fixture.GameWorld.EventHandler.AddEvent(gmB, LogSearchServerFixture.Fresh(viewerB.ID, 1,
                startMs: LogSearchServerFixture.StartMs + 4_000_000,
                endMs: LogSearchServerFixture.StartMs + 4_000_000 + 3_600_000));
            fixture.GameWorld.Update();
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();

            fixture.SetSendBufferCount(gmA, LogSearchService.SendBufferWatermark + 1);
            for (int i = 0; i < 3; i++)
            {
                fixture.GameWorld.Update();
                Assert.Empty(gmA.Sent.Where(s => s.StartsWith("LR")));
                Assert.Equal(LogSearchPhase.Delivering, viewerA.Phase);
            }

            Assert.NotNull(fixture.LastLrf(gmB));
            Assert.Equal(LogSearchPhase.Idle, viewerB.Phase);
            Assert.Equal(2, fixture.LastRowJsons(gmB).Count);

            fixture.SetSendBufferCount(gmA, 0);
            fixture.Pump();
            Assert.Equal(LogSearchPhase.Idle, viewerA.Phase);
            Assert.Equal(3, fixture.LastRowJsons(gmA).Count);
        }

        [Fact]
        public void PartialLrbLrdAcrossUpdatesWithoutLrfNeverCommits_OnlyLrfCompletes()
        {
            using var fixture = new LogSearchServerFixture();
            InsertBigRows(fixture, 3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();
            fixture.GameWorld.Update();

            Assert.Contains(gm.Sent, s => s.StartsWith($"LRB{viewer.ID},1"));
            Assert.Contains(gm.Sent, s => s.StartsWith($"LRD{viewer.ID},1"));
            Assert.DoesNotContain(gm.Sent, s => s.StartsWith($"LRF{viewer.ID},1"));
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2));
            Assert.Equal("A log search is already running.", fixture.LrxMessage(gm, viewer.ID, 2));
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            fixture.Pump();
            Assert.Equal(1, gm.Sent.Count(s => s.StartsWith($"LRF{viewer.ID},1")));
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
        }

        [Fact]
        public void CapacityFreedWhileDelivering_SameViewerNewLqsRejected()
        {
            using var fixture = new LogSearchServerFixture();
            InsertBigRows(fixture, 3);
            var gm = fixture.AddGm("Gm", 1);
            var viewer = fixture.OpenViewer(gm);
            fixture.Monotonic = 0;

            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 1));
            fixture.GameWorld.Database.Execute(conn => { });
            fixture.GameWorld.Update();
            fixture.GameWorld.Update();

            Assert.Equal(0, fixture.Service.ActiveDbQueryCount);
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            fixture.Monotonic += fixture.MillisecondTicks * 1000;
            fixture.Send(gm, LogSearchServerFixture.Fresh(viewer.ID, 2));
            Assert.Equal("A log search is already running.", fixture.LrxMessage(gm, viewer.ID, 2));
            Assert.Equal(LogSearchPhase.Delivering, viewer.Phase);

            fixture.Pump();
            Assert.Equal(LogSearchPhase.Idle, viewer.Phase);
        }
    }
}
