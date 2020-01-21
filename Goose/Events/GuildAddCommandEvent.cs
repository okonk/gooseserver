using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GuildAddCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GuildAddCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.Guild == null) return;
                if (this.Player.Guild.GetRank(this.Player) < Guild.GuildRanks.Officer) return;

                string name = ((string)this.Data).Substring(10);
                Player player = world.PlayerHandler.GetPlayer(name);
                if (player != null && player.State == Player.States.Ready)
                {
                    if (player.Guild == null)
                    {
                        this.Player.Guild.JoinGuild(player, world);
                        world.LogHandler.Log(Log.Types.JoinGuild, player.PlayerID, this.Player.Guild.ID.ToString(), this.Player.PlayerID);
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
