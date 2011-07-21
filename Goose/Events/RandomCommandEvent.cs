using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{

    public class RandomCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new RandomCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (!this.Player.Map.CanChat && this.Player.Access != Player.AccessStatus.GameMaster)
                {
                    world.Send(this.Player, "#Chat is disabled in this map.");
                    return;
                }

                int max = 0;
                string data = ((string)this.Data).Substring(7);

                if (data.Length > 0)
                {
                    try
                    {
                        max = Convert.ToInt32(data) + 1;
                    }
                    catch (Exception)
                    {
                        max = 0;
                    }
                }

                if (max <= 0) max = 1001;

                int rnd = world.Random.Next(1, max);
                string packet = "$7" + this.Player.Name + " rolls " + rnd + " out of " + (max-1) + ".";

                world.Send(this.Player, packet);
                foreach (Player player in this.Player.Map.GetPlayersInRange(this.Player))
                {
                    world.Send(player, packet);
                }
            }
        }
    }
}
