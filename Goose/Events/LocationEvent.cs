using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * Event for /location command
     * 
     */
    public class LocationEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new LocationEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Send(this.Player, "$7You are in " +
                    this.Player.Map.Name + " at " + this.Player.MapX + "," + this.Player.MapY + ".");
            }
        }
    }
}
