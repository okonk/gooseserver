using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GuildOwnerCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GuildOwnerCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.Guild == null) return;
                if (this.Player.Guild.GetRank(this.Player) < Guild.GuildRanks.Leader) return;

                string name = ((string)this.Data).Substring(12);
                Player player = world.PlayerHandler.GetPlayer(name);
                if (player != null && player.State == Player.States.Ready)
                {
                    if (player.Guild == this.Player.Guild && player != this.Player)
                    {
                        this.Player.Guild.ChangeOwner(this.Player, player, world);
                    }
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
