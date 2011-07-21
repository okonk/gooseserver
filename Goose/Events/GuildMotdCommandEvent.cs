using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GuildMotdCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GuildMotdCommandEvent();
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

                string motd = ((string)this.Data).Substring(10);
                if (motd.Length <= 1) 
                {
                    this.Player.Guild.MOTD = "";
                    this.Player.Guild.Dirty = true;
                }
                else
                {
                    this.Player.Guild.MOTD = motd.Substring(1);
                    this.Player.Guild.Dirty = true;
                }

                this.Player.Guild.SendToGuild("$2[guild-notice] MOTD: " + this.Player.Guild.MOTD, world);
            }
        }
    }
}
