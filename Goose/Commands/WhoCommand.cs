namespace Goose.Commands
{
    [Command("/who", Section = "General", Help = "List players on this map or search by name.")]
    public sealed class WhoCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] query)
        {
            var world = ctx.World;
            List<Player> players;
            string? search = null;
            int matches = 0;

            if (query.Length == 0)
            {
                players = ctx.Player.Map.Players;
            }
            else if (query[0].Equals("all"))
            {
                players = world.PlayerHandler.Players;
                if (query.Length > 1)
                    search = string.Join(" ", query[1..]);
            }
            else if (query[0].Equals("guild") && ctx.Player.Guild is not null)
            {
                players = ctx.Player.Guild.OnlineMembers;
                if (query.Length > 1)
                    search = string.Join(" ", query[1..]);
            }
            else
            {
                players = world.PlayerHandler.Players;
                search = string.Join(" ", query);
            }

            foreach (var player in players)
            {
                if (player is Pet) continue;
                if (player.IsGMInvisible) continue;
                if (player.IsWhoInvisible && ctx.Player.Access < player.Access) continue;
                if (search is not null && !MatchesQuery(player, search)) continue;

                if (player.State == Player.States.Ready)
                {
                    world.Send(ctx.Player, P.HashMessage("[" + player.Map.Name + "] " + InvisibleDisplay(player) + (!String.IsNullOrEmpty(player.Title) ? player.Title + " " : "") +
                                            player.Name + (!String.IsNullOrEmpty(player.Surname) ? " " + player.Surname : "") +
                                            " (Level " + player.Level + " " + ClassDisplay(player) + ")"));

                    matches++;
                }
            }

            world.Send(ctx.Player, P.HashMessage("[Matched " + matches + " players]"));
        }

        private bool MatchesQuery(Player player, string query)
        {
            return (!String.IsNullOrEmpty(player.Name) && player.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                   (!String.IsNullOrEmpty(player.Surname) && player.Surname.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        private string InvisibleDisplay(Player player)
        {
            if (player.IsWhoInvisible)
                return "*** Invisible *** ";
            else
                return "";
        }

        private string ClassDisplay(Player player)
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
