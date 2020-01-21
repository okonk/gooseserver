using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /summon playername
     * 
     */
    public class SummonEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SummonEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.Summon) ||
                (this.Player.HasPrivilege(AccessPrivilege.Warp) && (this.Player.Map.ID == 28 || this.Player.Map.ID == 30)))
            {
                string name = ((string)this.Data).Substring(8);
                Player player = world.PlayerHandler.GetPlayer(name);
                if (player != null)
                {
                    if (player.State != Player.States.Ready)
                    {
                        world.Send(this.Player, P.ServerMessage("Player is still loading a map."));
                        return;
                    }

                    player.WarpTo(world, this.Player.Map, this.Player.MapX, this.Player.MapY);
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
