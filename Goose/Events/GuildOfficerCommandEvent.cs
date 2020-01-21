using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GuildOfficerCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GuildOfficerCommandEvent();
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

                string name = ((string)this.Data).Substring(14);

                Player player = world.PlayerHandler.GetPlayer(name);
                if (player != null && player.State == Player.States.Ready)
                {
                    if (player.Guild == this.Player.Guild && player != this.Player)
                    {
                        switch (player.Guild.GetRank(player))
                        {
                            case Guild.GuildRanks.Officer:
                                player.Guild.ChangeRank(player, Guild.GuildRanks.Member, world);
                                break;
                            case Guild.GuildRanks.Member:
                                player.Guild.ChangeRank(player, Guild.GuildRanks.Officer, world);
                                break;
                        }
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
