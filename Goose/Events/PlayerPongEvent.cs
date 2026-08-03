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
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.NotLoggedIn)
            {
                this.Player.LastPing = world.TimeNow;
            }
        }
    }
}
