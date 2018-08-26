using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * OpenCombineBagEvent, opens combine bag
     * 
     * Packet: OCB
     * 
     */
    class OpenCombineBagEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new OpenCombineBagEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                CombineBagWindow.Open(world, this.Player);
            }
        }
    }
}
