using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * PlayerPongEvent, event for PONG packet
     * 
     */
    public class PlayerPongEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PlayerPongEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.NotLoggedIn)
            {
                this.Player.LastPing = world.TimeNow;
            }
        }
    }
}
