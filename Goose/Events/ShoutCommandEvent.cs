using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class ShoutCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ShoutCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.UpdateIdleStatus(world);

                string data = ((string)this.Data).Substring(7);

                if (data.Length <= 0) return;

                if ((!this.Player.Map.CanShout || this.Player.Map.Muted) && !this.Player.HasPrivilege(AccessPrivilege.TalkWhileMuted))
                {
                    world.Send(this.Player, P.HashMessage("Shouting is disabled in this map."));
                    return;
                }

                string packet = P.HashMessage(this.Player.Name + " shouts: " + data);
                string filteredpacket = P.HashMessage(this.Player.Name + " shouts: ");
                bool filtered = false;

                world.LogHandler.Log(Log.Types.Shout, this.Player.PlayerID, data, 0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);

                List<Player> range = this.Player.Map.Players;
                foreach (Player player in range)
                {
                    if (player.ChatFilterEnabled)
                    {
                        if (!filtered) 
                        {
                            filteredpacket += world.ChatFilter.Filter(data);
                            filtered = true;
                        }
                        world.Send(player, filteredpacket);
                    }
                    else
                    {
                        world.Send(player, packet);
                    }
                }
            }
        }
    }
}
