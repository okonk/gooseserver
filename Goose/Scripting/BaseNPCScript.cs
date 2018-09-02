using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public class BaseNPCScript : INPCScript
    {
        public BaseNPCScript() { }

        public virtual void OnMoveEvent(NPC npc, GameWorld world)
        {
            npc.HandleMoveEvent(world);
        }

        public virtual void OnAttackEvent(NPC npc, GameWorld world)
        {
            npc.HandleAttackEvent(world);
        }
    }
}
