using System.Diagnostics;

namespace Goose.Logs
{
    internal sealed class LogSearchService
    {
        public const int MaxConcurrentDbQueries = 4;

        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private static readonly long OneSecondTicks = Stopwatch.Frequency;

        private const string AlreadyRunning = "A log search is already running.";
        private const string Malformed = "Malformed log search request.";
        private const string InvalidToken = "Log page token is invalid or expired.";
        private const string RateLimited = "Log searches are rate limited; try again shortly.";
        private const string TooManyQueries = "Too many log searches are in progress; try again shortly.";
        private const string Oversize = "Result is too large to display. Narrow the search.";
        private const string Failed = "Log search failed.";

        private readonly GameWorld world;
        private readonly Func<long> monotonicNow;
        private readonly Func<string> tokenSource;
        private readonly Func<DateTimeOffset> utcNow;
        private readonly Dictionary<int, long> lastFreshTicksByPlayer = new();
        private int activeDbQueries;

        public LogSearchService(GameWorld world, Func<long>? monotonicNow = null,
            Func<string>? tokenSource = null, Func<DateTimeOffset>? utcNow = null)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.monotonicNow = monotonicNow ?? (() => world.TimeNow);
            this.tokenSource = tokenSource ?? LogPageTokenCodec.Create;
            this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
            world.DeliveryPump = this.PumpDeliveries;
        }

        public int ActiveDbQueryCount => this.activeDbQueries;

        public void Admit(Player player, string packet)
        {
            if (player.State != Player.States.Ready) return;
            if (!player.HasPrivilege(AccessPrivilege.ViewLogs)) return;
            LogViewerWindow? viewer = player.Windows.OfType<LogViewerWindow>().FirstOrDefault();
            if (viewer is null) return;

            bool parsed = LogSearchPacket.TryParse(packet, out LogSearchRequest? request);
            int windowId = request?.WindowId ?? 0;
            int requestId = request?.RequestId ?? 0;
            if (!parsed && packet.Length <= LogSearchPacket.MaxPacketLength
                && LogSearchPacket.TryIdentify(packet, out int malformedWindowId, out int malformedRequestId))
            {
                windowId = malformedWindowId;
                requestId = malformedRequestId;
            }

            if (windowId != viewer.ID) return;

            if (viewer.Phase is LogSearchPhase.Querying or LogSearchPhase.Delivering)
            {
                this.SendError(player, viewer, requestId, AlreadyRunning);
                return;
            }

            if (!parsed)
            {
                this.SendError(player, viewer, requestId, Malformed);
                return;
            }

            if (request!.Action == LogSearchAction.Page)
                this.AdmitPage(player, viewer, request);
            else
                this.AdmitFresh(player, viewer, request);
        }

        private void AdmitFresh(Player player, LogViewerWindow viewer, LogSearchRequest request)
        {
            long now = this.monotonicNow();
            if (this.lastFreshTicksByPlayer.TryGetValue(player.PlayerID, out long last)
                && now - last < OneSecondTicks)
            {
                this.SendError(player, viewer, request.RequestId, RateLimited);
                return;
            }
            if (this.activeDbQueries >= MaxConcurrentDbQueries)
            {
                this.SendError(player, viewer, request.RequestId, TooManyQueries);
                return;
            }

            LogFreshSearchInput input = request.Fresh!;
            viewer.BeginQuery(request.RequestId);
            this.activeDbQueries++;
            try
            {
                this.world.Database.Enqueue(connection =>
                {
                    LogValidationResult validation = LogQueryValidator.ValidateFresh(connection, input);
                    if (!validation.IsSuccess)
                    {
                        this.world.EnqueueCompletion(() =>
                            this.CompleteError(player, viewer, request.RequestId, validation.Message));
                        return;
                    }
                    LogSearchQuery baseQuery = validation.Query!;
                    LogSearchPage page = LogQueryEngine.Execute(connection, baseQuery);
                    this.world.EnqueueCompletion(() =>
                        this.CompleteFresh(player, viewer, request.RequestId, baseQuery, page));
                }, error =>
                {
                    if (error is not null)
                        this.world.EnqueueCompletion(() =>
                            this.CompleteError(player, viewer, request.RequestId, Failed));
                });
            }
            catch (Exception e)
            {
                this.activeDbQueries--;
                viewer.AbortQuery();
                log.Error(e, "Failed to enqueue log search for {0}.", player.Name);
                this.SendError(player, viewer, request.RequestId, Failed);
                return;
            }

            this.RecordFreshAdmission(player, input);
        }

        private void AdmitPage(Player player, LogViewerWindow viewer, LogSearchRequest request)
        {
            string token = request.PageToken!;
            LogViewerSearchSession? session = viewer.Session;
            if (session is null
                || !session.TryResolve(token, out LogSearchQuery? query, out IReadOnlyList<LogQueryRow>? storedRows))
            {
                this.SendError(player, viewer, request.RequestId, InvalidToken);
                return;
            }

            if (storedRows is not null)
            {
                this.DeliverStoredRows(player, viewer, request.RequestId, token, storedRows);
                return;
            }

            if (this.activeDbQueries >= MaxConcurrentDbQueries)
            {
                this.SendError(player, viewer, request.RequestId, TooManyQueries);
                return;
            }

            viewer.BeginQuery(request.RequestId);
            this.activeDbQueries++;
            try
            {
                this.world.Database.Enqueue(connection =>
                {
                    LogSearchPage page = LogQueryEngine.Execute(connection, query!);
                    this.world.EnqueueCompletion(() =>
                        this.CompletePage(player, viewer, request.RequestId, session, token, page));
                }, error =>
                {
                    if (error is not null)
                        this.world.EnqueueCompletion(() =>
                            this.CompleteError(player, viewer, request.RequestId, Failed));
                });
            }
            catch (Exception e)
            {
                this.activeDbQueries--;
                viewer.AbortQuery();
                log.Error(e, "Failed to enqueue log page query for {0}.", player.Name);
                this.SendError(player, viewer, request.RequestId, Failed);
            }
        }

        internal void ReserveDbSlot() => this.activeDbQueries++;

        internal void RecordFreshAdmission(Player player, LogFreshSearchInput input)
        {
            this.lastFreshTicksByPlayer[player.PlayerID] = this.monotonicNow();
            string json = LogSearchAuditFormatter.Format(input);
            this.world.LogHandler.Log(Log.Types.ViewLogs, player.PlayerID, json, 0,
                player.MapID, player.MapX, player.MapY);
            this.world.LogHandler.Pending[^1].Time = this.utcNow().UtcDateTime;
        }

        internal void CompleteFresh(Player player, LogViewerWindow viewer, int requestId,
            LogSearchQuery baseQuery, LogSearchPage page)
        {
            this.activeDbQueries--;
            if (!this.IsCurrent(player, viewer, requestId)) return;

            LogViewerSearchSession candidate = new(baseQuery, viewer.NextSessionGeneration(), this.tokenSource);
            candidate.StoreFirstPage(page.Rows, page.NextCursor);
            string currentToken = candidate.FirstPageToken;
            string nextToken = page.NextCursor is null ? string.Empty : candidate.IssueToken(page.NextCursor);

            if (!TryBuildRows(page.Rows, out List<byte[]> rowJsons, out string? failure)
                || !LogProtocolPackets.TryBuildSearchResponse(viewer.ID, requestId, rowJsons,
                    page.HasMore, currentToken, nextToken, out LogProtocolPackets.SearchResponse? response))
            {
                failure ??= Oversize;
                this.DeliverError(player, viewer, requestId, failure);
                return;
            }

            viewer.CommitSession(candidate);
            this.DeliverResponse(player, viewer, requestId, response!.Packets);
        }

        internal void CompletePage(Player player, LogViewerWindow viewer, int requestId,
            LogViewerSearchSession session, string token, LogSearchPage page)
        {
            this.activeDbQueries--;
            if (!this.IsCurrent(player, viewer, requestId)) return;
            if (viewer.Session != session)
            {
                this.DeliverError(player, viewer, requestId, Failed);
                return;
            }

            string nextToken = page.NextCursor is null ? string.Empty : session.IssueToken(page.NextCursor);
            if (!TryBuildRows(page.Rows, out List<byte[]> rowJsons, out string? failure)
                || !LogProtocolPackets.TryBuildSearchResponse(viewer.ID, requestId, rowJsons,
                    page.HasMore, token, nextToken, out LogProtocolPackets.SearchResponse? response))
            {
                failure ??= Oversize;
                this.DeliverError(player, viewer, requestId, failure);
                return;
            }

            this.DeliverResponse(player, viewer, requestId, response!.Packets);
        }

        internal void CompleteError(Player player, LogViewerWindow viewer, int requestId, string message)
        {
            this.activeDbQueries--;
            if (!this.IsCurrent(player, viewer, requestId)) return;
            this.DeliverError(player, viewer, requestId, message);
        }

        private void DeliverStoredRows(Player player, LogViewerWindow viewer, int requestId,
            string token, IReadOnlyList<LogQueryRow> rows)
        {
            if (!TryBuildRows(rows, out List<byte[]> rowJsons, out string? failure)
                || !LogProtocolPackets.TryBuildSearchResponse(viewer.ID, requestId, rowJsons,
                    false, token, string.Empty, out LogProtocolPackets.SearchResponse? response))
            {
                this.SendError(player, viewer, requestId, failure ?? Oversize);
                return;
            }
            viewer.BeginDelivery(requestId, response!.Packets);
            this.EnqueueDelivery(player, viewer, requestId);
        }

        private void DeliverResponse(Player player, LogViewerWindow viewer, int requestId, IReadOnlyList<string> packets)
        {
            viewer.BeginDelivery(requestId, packets);
            this.EnqueueDelivery(player, viewer, requestId);
        }

        private void DeliverError(Player player, LogViewerWindow viewer, int requestId, string message)
        {
            this.DeliverResponse(player, viewer, requestId,
                [LogProtocolPackets.BuildSearchError(viewer.ID, requestId, message)]);
        }

        private void SendError(Player player, LogViewerWindow viewer, int requestId, string message)
        {
            this.world.Send(player, LogProtocolPackets.BuildSearchError(viewer.ID, requestId, message));
        }

        private bool IsCurrent(Player player, LogViewerWindow viewer, int requestId)
        {
            return player.State == Player.States.Ready
                && player.Windows.Contains(viewer)
                && viewer.Phase == LogSearchPhase.Querying
                && viewer.ActiveRequestId == requestId;
        }

        private static bool TryBuildRows(IReadOnlyList<LogQueryRow> rows, out List<byte[]> rowJsons, out string? failure)
        {
            rowJsons = new List<byte[]>(rows.Count);
            failure = null;
            foreach (LogQueryRow row in rows)
            {
                if (row.UtcTicks < 0 || row.UtcTicks > DateTime.MaxValue.Ticks)
                {
                    failure = Failed;
                    return false;
                }
                if (!LogRowJsonSerializer.TrySerialize(row, out byte[] json))
                {
                    failure = Oversize;
                    return false;
                }
                rowJsons.Add(json);
            }
            return true;
        }

        private void EnqueueDelivery(Player player, LogViewerWindow viewer, int requestId)
        {
            this.world.EnqueueDelivery(() =>
            {
                if (player.State != Player.States.Ready
                    || !player.Windows.Contains(viewer)
                    || viewer.Phase != LogSearchPhase.Delivering
                    || viewer.ActiveRequestId != requestId)
                {
                    viewer.FinishDelivery();
                    return;
                }
                string? packet = viewer.TakeNextDeliveryPacket();
                if (packet is null)
                {
                    viewer.FinishDelivery();
                    return;
                }
                this.world.Send(player, packet);
                if (viewer.DeliveryComplete)
                {
                    viewer.FinishDelivery();
                    return;
                }
                this.EnqueueDelivery(player, viewer, requestId);
            });
        }

        private void PumpDeliveries()
        {
            foreach (Action action in this.world.DrainDeliveries())
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    log.Error(e, "Log search delivery failed.");
                }
            }
        }
    }
}
