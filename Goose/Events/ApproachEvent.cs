using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /approach playername
     * 
     */
    public class ApproachEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string name = ((string)this.Data).Substring(10);
                Player player = world.PlayerHandler.GetPlayer(name);
                if (player != null)
                {
                    if (player.State != Player.States.Ready)
                    {
                        world.Send(this.Player, P.ServerMessage("Player is still loading a map."));
                        return;
                    }

                    this.Player.WarpTo(world, player.Map, player.MapX, player.MapY);
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
