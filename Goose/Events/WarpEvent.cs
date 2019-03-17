using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /warp mapid mapx mapy
     * 
     */
    public class WarpEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new WarpEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready && 
                this.Player.HasPrivilege(AccessPrivilege.Warp))
            {
                string[] tokens = ((string)this.Data).Split(" ".ToCharArray());
                int mapid = 1;
                int mapx = 50;
                int mapy = 50;
                try
                {
                    mapid = Convert.ToInt32(tokens[1]);
                    mapx = Convert.ToInt32(tokens[2]);
                    mapy = Convert.ToInt32(tokens[3]);
                }
                catch (Exception)
                {
                    return;
                }

                if (tokens.Length == 4)
                {
                    Map map = world.MapHandler.GetMap(mapid);
                    if (map != null)
                    {
                        // invalid coordinates
                        if (mapx < 1 || mapx >= map.Width + 1 || mapy < 1 || mapy >= map.Height + 1) return;

                        this.Player.WarpTo(world, map, mapx, mapy);
                    }
                }
            }
        }
    }
}
