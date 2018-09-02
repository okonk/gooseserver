using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public interface INPCScript
    {
        void OnMoveEvent(NPC npc, GameWorld world);

        void OnAttackEvent(NPC npc, GameWorld world);
    }
}
