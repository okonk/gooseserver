using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * WhoEvent, event for "who" packet
     * 
     * Called when someone types /who [all|guild] [name]
     * Packet format: /who [all|guild] [name]
     * 
     * Server responds: #[Mapname] Playername (Level lvl class)
     * 
     */
    class WhoEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new WhoEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string packet = (string)this.Data;
                List<Player> players;
                int matches = 0;

                if (packet.Equals("/who"))
                {
                    players = this.Player.Map.Players;
                }
                else
                {
                    string[] search = packet.Split(" ".ToCharArray());
                    if (search.Length > 1)
                    {
                        if (search[1].Equals("all"))
                        {
                            players = world.PlayerHandler.Players;
                        }
                        else if (search[1].Equals("guild") && this.Player.Guild != null)
                        {
                            players = this.Player.Guild.OnlineMembers;
                        }
                        else 
                        {
                            players = new List<Player>();
                        }
                    }
                    else
                    {
                        players = this.Player.Map.Players;
                    }
                }

                foreach (Player player in players)
                {
                    if (player is Pet) continue;
                    if (player.IsGMInvisible) continue;

                    if (player.State == Player.States.Ready)
                    {
                        world.Send(this.Player, "#[" + player.Map.Name + "] " + (!String.IsNullOrEmpty(player.Title) ? player.Title + " " : "") +
                                                player.Name + (!String.IsNullOrEmpty(player.Surname) ? " " + player.Surname : "") +
                                                " (Level " + player.Level + " " + player.Class.ClassName + ")");

                        matches++;
                    }
                }

                world.Send(this.Player, "#[Matched " + matches + " players]");
            }
        }
    }
}
