using Goose.Logs;

namespace Goose
{
    public class LogViewerWindow : Window
    {
        public const string TitleText = "GM Log Viewer";
        private const long PreviousDayUnixMs = 86_400_000;

        internal LogViewerSearchSession? Session { get; set; }

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
                old.Session?.Clear();
                old.Close(player, world);
            }

            var window = new LogViewerWindow();
            player.Windows.Add(window);
            window.Create(player, world);
            return true;
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
                    this.Session?.Clear();
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
