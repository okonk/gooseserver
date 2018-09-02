using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class NPCAttackEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            this.NPC.AttackEvent = null;

            if (this.NPC.State == NPC.States.Dead) return;
            if (this.NPC.AggroTarget == null) return;

            this.NPC.OnAttackEvent(world);
        }
    }
}
