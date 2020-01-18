using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    public class LogHandler
    {
        List<Log> logs;

        public LogHandler()
        {
            this.logs = new List<Log>();
        }

        public void Save(GameWorld world)
        {
            foreach (Log log in this.logs)
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
