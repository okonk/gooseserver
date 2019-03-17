using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /ban player
     * 
     * Bans player from server
     * 
     */
    public class BanCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new BanCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.Ban))
            {
                string name = ((string)this.Data).Substring(5);
                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player != null)
                {
                    player.Access = Player.AccessStatus.Banned;
                    world.Send(this.Player, "$7Banned " + name + ".");

                    if (player.State != Goose.Player.States.NotLoggedIn)
                    {
                        world.LostConnection(player.Sock);
                    }
                    else
                    {
                        player.SaveToDatabase(world);
                    }
                }
                else
                {
                    world.Send(this.Player, "$7Couldn't find player.");
                }
            }
        }
    }
}
