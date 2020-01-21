using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Events
{
    public class PlayerInfoCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PlayerInfoCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.PlayerInfoCheck))
            {
                string name = ((string)this.Data).Substring("/playerinfo ".Length);
                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player != null)
                {
                    PlayerInfoWindow.Open(world, this.Player, player);
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
