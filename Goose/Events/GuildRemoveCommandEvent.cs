using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GuildRemoveCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GuildRemoveCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.Guild == null) return;

                string name = ((string)this.Data).Substring(12);

                if (name.Length <= 0)
                {
                    world.LogHandler.Log(Log.Types.LeaveGuild, this.Player.PlayerID, this.Player.Guild.ID.ToString());
                    this.Player.Guild.LeaveGuild(this.Player, world);
                }
                else
                {
                    if (this.Player.Guild.GetRank(this.Player) < Guild.GuildRanks.Officer) return;

                    name = name.Substring(1);
                    Player player = world.PlayerHandler.GetPlayer(name);
                    if (player != null && player.State == Player.States.Ready)
                    {
                        if (player.Guild != null && 
                            player.Guild == this.Player.Guild && 
                            this.Player.Guild.GetRank(this.Player) > this.Player.Guild.GetRank(player))
                        {
                            this.Player.Guild.LeaveGuild(player, world, true);
                            world.LogHandler.Log(Log.Types.LeaveGuild, player.PlayerID, this.Player.Guild.ID.ToString(), this.Player.PlayerID);
                        }
                    }
                    else
                    {
                        world.Send(this.Player, "$7Couldn't find player.");
                    }
                }
            }
        }
    }
}
