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

        public virtual void OnSpawnEvent(NPC npc, GameWorld world)
        {

        }

        public virtual void OnMoveEvent(NPC npc, GameWorld world)
        {
            npc.HandleMoveEvent(world);
        }

        public virtual void OnAttackEvent(NPC npc, GameWorld world)
        {
            npc.HandleAttackEvent(world);
        }

        public virtual void OnAttackedEvent(NPC npc, ICharacter attacker, long damage, GameWorld world)
        {

        }

        public virtual void OnKilledEvent(NPC npc, ICharacter killer, GameWorld world)
        {

        }

        public virtual void OnPlayerChatEvent(NPC npc, Player player, string message, GameWorld world)
        {

        }
    }
}
