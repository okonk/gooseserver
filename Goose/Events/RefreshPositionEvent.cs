using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * Called when someone types /refresh
     * 
     */
    public class RefreshPositionEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new RefreshPositionEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                // Fix the clients position
                world.Send(this.Player, "SUP" + this.Player.MapX + "," + this.Player.MapY);
            }
        }
    }
}
