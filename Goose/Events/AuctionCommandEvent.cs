using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class AuctionCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new AuctionCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.UpdateIdleStatus(world);

                string data = ((string)this.Data).Substring(9);

                if (data.Length <= 0) return;

                if ((!this.Player.Map.CanAuction || this.Player.Map.Muted) && !this.Player.HasPrivilege(AccessPrivilege.TalkWhileMuted))
                {
                    world.Send(this.Player, P.HashMessage("Auction is disabled in this map."));
                    return;
                }

                string packet = P.ServerMessage("<Auction> " + this.Player.Name + ": " + data);
                string filteredpacket = P.ServerMessage("<Auction> " + this.Player.Name + ": ");
                bool filtered = false;

                world.LogHandler.Log(Log.Types.Auction, this.Player.PlayerID, data, 0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);

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