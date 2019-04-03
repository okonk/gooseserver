using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public interface INPCScript
    {
        void OnSpawnEvent(NPC npc, GameWorld world);

        void OnMoveEvent(NPC npc, GameWorld world);

        void OnAttackEvent(NPC npc, GameWorld world);

        void OnAttackedEvent(NPC npc, ICharacter attacker, long damage, GameWorld world);

        void OnKilledEvent(NPC npc, ICharacter killer, GameWorld world);

        void OnPlayerChatEvent(NPC npc, Player player, string message, GameWorld world);
    }
}
