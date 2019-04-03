using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public class BaseMapScript : IMapScript
    {
        public BaseMapScript() { }

        public virtual void OnPlayerEntered(Map map, Player player, GameWorld world)
        {

        }

        public virtual void OnPlayerLeft(Map map, Player player, GameWorld world)
        {

        }

        public virtual void OnPlayerMove(Map map, Player player, GameWorld world)
        {

        }

        public virtual void OnPlayerChatEvent(Map map, Player player, string message, GameWorld world)
        {

        }
    }
}
