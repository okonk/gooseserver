using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public interface IMapScript
    {
        void OnLoad(Map map, GameWorld world);
        void OnLoadTile(Map map, int x, int y, int layerNumber, int graphic, short sheet, int flags, GameWorld world);
        void OnFinishedLoad(Map map, GameWorld world);
        void OnPlayerEntered(Map map, Player player, GameWorld world);
        void OnPlayerLeft(Map map, Player player, GameWorld world);
        void OnPlayerMove(Map map, Player player, GameWorld world);
        void OnPlayerChatEvent(Map map, Player player, string message, GameWorld world);
        void OnNPCKilledEvent(Map map, NPC npc, ICharacter killer, GameWorld world);
        void OnNPCSpawnEvent(Map map, NPC npc, GameWorld world);
        void OnPetMove(Map map, Pet pet, GameWorld world);
    }
}
