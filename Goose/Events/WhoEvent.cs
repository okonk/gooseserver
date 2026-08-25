using System.Text;

namespace Goose.Events
{
    /**
     * WhoEvent, event for "who" packet
     * 
     * Called when someone types /who [all|guild] [name] or /who name
     * Packet format: /who [all|guild] [name]
     * A name argument (with or without a scope) does a case-insensitive
     * substring match against the player's name and surname.
     * 
     * Server responds: #[Mapname] Playername (Level lvl class)
     * 
     */
    class WhoEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string packet = (string)this.Data;
                List<Player> players;
                string query = null;
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
                            if (search.Length > 2)
                                query = string.Join(" ", search, 2, search.Length - 2);
                        }
                        else if (search[1].Equals("guild") && this.Player.Guild != null)
                        {
                            players = this.Player.Guild.OnlineMembers;
                            if (search.Length > 2)
                                query = string.Join(" ", search, 2, search.Length - 2);
                        }
                        else
                        {
                            players = world.PlayerHandler.Players;
                            query = string.Join(" ", search, 1, search.Length - 1);
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
                    if (player.IsWhoInvisible && this.Player.Access < player.Access) continue;
                    if (query != null && !MatchesQuery(player, query)) continue;

                    if (player.State == Player.States.Ready)
                    {
                        world.Send(this.Player, P.HashMessage("[" + player.Map.Name + "] " + InvisibleDisplay(player) + (!String.IsNullOrEmpty(player.Title) ? player.Title + " " : "") +
                                                player.Name + (!String.IsNullOrEmpty(player.Surname) ? " " + player.Surname : "") +
                                                " (Level " + player.Level + " " + ClassDisplay(player) + ")"));

                        matches++;
                    }
                }

                world.Send(this.Player, P.HashMessage("[Matched " + matches + " players]"));
            }
        }

        private bool MatchesQuery(Player player, string query)
        {
            return (!String.IsNullOrEmpty(player.Name) && player.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                   (!String.IsNullOrEmpty(player.Surname) && player.Surname.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        public string InvisibleDisplay(Player player)
        {
            if (player.IsWhoInvisible)
                return "*** Invisible *** ";
            else
                return "";
        }

        public string ClassDisplay(Player player)
        {
            if (player.Access > Player.AccessStatus.Normal)
            {
                return player.Access.ToString().Replace("Master", " Master");
            }
            else
            {
                return player.Class.ClassName;
            }
        }
    }
}
