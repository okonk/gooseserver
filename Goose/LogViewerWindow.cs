using Goose.Logs;

namespace Goose
{
    internal enum LogSearchPhase
    {
        Idle,
        Querying,
        Delivering,
    }

    public class LogViewerWindow : Window
    {
        public const string TitleText = "GM Log Viewer";
        private const long PreviousDayUnixMs = 86_400_000;

        internal LogViewerSearchSession? Session { get; set; }
        internal LogSearchPhase Phase { get; private set; } = LogSearchPhase.Idle;
        internal int ActiveRequestId { get; private set; }
        internal int SessionGeneration { get; private set; }
        internal IReadOnlyList<string>? DeliveryPackets { get; private set; }
        internal int DeliveryIndex { get; private set; }

        internal string? TakeNextDeliveryPacket()
        {
            if (this.DeliveryPackets is null || this.DeliveryIndex >= this.DeliveryPackets.Count) return null;
            string packet = this.DeliveryPackets[this.DeliveryIndex];
            this.DeliveryIndex++;
            return packet;
        }

        internal bool DeliveryComplete =>
            this.DeliveryPackets is null || this.DeliveryIndex >= this.DeliveryPackets.Count;

        private LogViewerWindow()
        {
            this.Title = TitleText;
            this.Buttons = "0,1,0,0,0";
            this.Frame = WindowFrames.LogViewer;
            this.Type = WindowTypes.LogViewer;
        }

        public static bool Open(Player player, GameWorld world)
        {
            if (player.State != Player.States.Ready)
                return false;
            if (!player.HasPrivilege(AccessPrivilege.ViewLogs))
                return false;

            foreach (var old in player.Windows.OfType<LogViewerWindow>().ToList())
            {
                old.InvalidateSearchState();
                old.Close(player, world);
            }

            var window = new LogViewerWindow();
            player.Windows.Add(window);
            window.Create(player, world);
            return true;
        }

        internal int NextSessionGeneration() => ++this.SessionGeneration;

        internal void BeginQuery(int requestId)
        {
            this.ActiveRequestId = requestId;
            this.Phase = LogSearchPhase.Querying;
        }

        internal void AbortQuery()
        {
            this.ActiveRequestId = 0;
            this.Phase = LogSearchPhase.Idle;
        }

        internal void BeginDelivery(int requestId, IReadOnlyList<string> packets)
        {
            this.ActiveRequestId = requestId;
            this.Phase = LogSearchPhase.Delivering;
            this.DeliveryPackets = packets;
            this.DeliveryIndex = 0;
        }

        internal void FinishDelivery()
        {
            this.ActiveRequestId = 0;
            this.Phase = LogSearchPhase.Idle;
            this.DeliveryPackets = null;
            this.DeliveryIndex = 0;
        }

        internal void CommitSession(LogViewerSearchSession session)
        {
            this.Session?.Clear();
            this.Session = session;
        }

        private void InvalidateSearchState()
        {
            this.Session?.Clear();
            this.Session = null;
            this.FinishDelivery();
        }

        public override void Populate(Player player, GameWorld world)
        {
            foreach (var descriptor in LogEventRegistry.Known.OrderBy(d => d.Id))
                world.Send(player, LogProtocolPackets.BuildTypeMetadata(this.ID, descriptor.Id,
                    GroupDisplay(descriptor.Group), descriptor.Label));

            foreach (var map in world.MapHandler.Maps.OrderBy(m => m.Key))
                world.Send(player, LogProtocolPackets.BuildMapMetadata(this.ID, map.Key, map.Value.Name));

            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            world.Send(player, LogProtocolPackets.BuildDefaultRange(this.ID, end - PreviousDayUnixMs, end));
        }

        public override void Clicked(ButtonTypes buttonid, int npcid, int id2, int id3, Player player, GameWorld world)
        {
            switch (buttonid)
            {
                case ButtonTypes.Exit:
                case ButtonTypes.Close:
                    this.InvalidateSearchState();
                    player.Windows.Remove(this);
                    break;
            }
        }

        private static string GroupDisplay(LogEventGroup group) => group switch
        {
            LogEventGroup.Communication => "Communication",
            LogEventGroup.SessionsSecurity => "Sessions/Security",
            LogEventGroup.Social => "Social",
            LogEventGroup.ItemsEconomy => "Items/Economy",
            LogEventGroup.GmActions => "GM Actions",
            _ => "Other/Retired",
        };
    }
}
