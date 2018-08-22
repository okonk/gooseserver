using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class GMHaxCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMHaxCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready && this.Player.Access == Player.AccessStatus.GameMaster)
            {
                string data = ((string)this.Data).Substring(5);
                world.Send(this.Player, data);
                foreach (var player in this.Player.Map.GetPlayersInRange(this.Player))
                {
                    world.Send(player, data);
                }
            }
        }
    }
}
