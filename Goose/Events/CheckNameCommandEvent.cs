using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class CheckNameCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new CheckNameCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.Access == Player.AccessStatus.GameMaster)
            {
                string name = ((string)this.Data).Substring(11);
                Player player = world.PlayerHandler.GetPlayerFromData(name);

                if (player == null)
                {
                    world.Send(this.Player, "$7" + name + " is currently unused.");
                }
                else
                {
                    world.Send(this.Player, "$7" + name + " is used.");
                }
            }
        }
    }
}
