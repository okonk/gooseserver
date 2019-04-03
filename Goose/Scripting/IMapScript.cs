using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public interface IMapScript
    {
        void OnPlayerEntered(Map map, Player player, GameWorld world);
        void OnPlayerLeft(Map map, Player player, GameWorld world);
        void OnPlayerMove(Map map, Player player, GameWorld world);
        void OnPlayerChatEvent(Map map, Player player, string message, GameWorld world);
    }
}
