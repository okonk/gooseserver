using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * Called when GM types /shutdown
     * 
     */
    public class ShutdownCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ShutdownCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.HasPrivilege(AccessPrivilege.Shutdown))
                {
                    world.Running = false;
                }
            }
        }
    }
}
