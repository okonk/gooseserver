using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /mutemap
     * 
     */
    public class MuteMapEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new MuteMapEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.MuteMap))
            {
                this.Player.Map.Muted = !this.Player.Map.Muted;

                world.SendToMap(this.Player.Map, string.Format("$7Chat is now {0}.", (this.Player.Map.Muted ? "muted" : "unmuted")));
            }
        }
    }
}
