using System.Text;

namespace Goose
{
    public class LogHandler
    {
        List<Log> logs;

        /// <summary>Entries buffered since the last Save, read-only. The log buffers in
        /// memory and is flushed on a timer (GameWorld.cs:416), so tests (and audits) read
        /// this rather than reflecting into the list.</summary>
        public IReadOnlyList<Log> Pending => this.logs;

        public LogHandler()
        {
            this.logs = [];
        }

        public void Save(GameWorld world)
        {
            foreach (var log in this.logs)
            {
                log.SaveToDatabase(world);
            }

            this.logs.Clear();
        }

        public void Log(Log.Types type, int playerid, string text, int otherid = 0, int mapid = 0, int mapx = 0, int mapy = 0)
        {
            this.logs.Add(new Log(type, playerid, text, otherid, mapid, mapx, mapy));
        }

        public void Log(Log.Types type, Player player, string text, int otherid = 0)
        {
            this.logs.Add(new Log(type, player.PlayerID, text, otherid, player.MapID, player.MapX, player.MapY));
        }
    }
}
