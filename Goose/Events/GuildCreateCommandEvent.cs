using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * GuildCreateCommandEvent
     * 
     * Event for /guildcreate
     * Packet: /guildcreate <guild name here>
     * 
     * 
     */
    class GuildCreateCommandEvent : Event
    {
       public static Event Create(Player player, Object data)
        {
            Event e = new GuildCreateCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

       public override void Ready(GameWorld world)
       {
           if (this.Player.State == Player.States.Ready)
           {
               if (this.Player.Gold < GameSettings.Default.GuildCreationCost)
               {
                   world.Send(this.Player, 
                       P.ServerMessage("You need " + GameSettings.Default.GuildCreationCost + "gp to create a guild."));
                   return;
               }

               string name = ((string)this.Data).Substring(13);
               if (name.Length <= 3 || name.Length > 128)
               {
                   world.Send(this.Player, P.ServerMessage("Your guild name needs to be between 3 and 128 characters."));
                   return;
               }

               if (this.Player.Guild != null)
               {
                   this.Player.Guild.LeaveGuild(this.Player, world);
               }

               this.Player.RemoveGold(GameSettings.Default.GuildCreationCost, world);
               this.Player.Guild = new Guild();
               this.Player.Guild.ID = 0;
               this.Player.Guild.MOTD = GameSettings.Default.DefaultGuildMOTD;
               this.Player.Guild.Name = name;
               this.Player.Guild.AddMember(this.Player.PlayerID, Guild.GuildRanks.Leader, true, true);
               this.Player.Guild.OnlineMembers.Add(this.Player);

               this.Player.Guild.SendToGuild(P.GuildMessage("[guild-notice] MOTD: " + GameSettings.Default.DefaultGuildMOTD), world);

               world.GuildHandler.AddGuild(this.Player.Guild);
           }
       }
    }
}
