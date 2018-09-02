using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * NPCMoveEvent
     * 
     */
    public class NPCMoveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            this.NPC.MoveEvent = null;

            if (this.NPC.State != NPC.States.Alive) return;

            this.NPC.OnMoveEvent(world);
        }
    }
}
