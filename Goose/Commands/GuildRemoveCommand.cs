namespace Goose.Commands
{
    [Command("/guildremove", Section = "Guild", Help = "Leave your guild or remove a player from it.")]
    public sealed class GuildRemoveCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;

            if (ctx.Player.Guild is null) return;

            // Legacy Split(' ') without RemoveEmptyEntries: no token means leave guild,
            // an empty token falls through to the rank check and GetPlayer("").
            if (ctx.Remainder.Length <= 0)
            {
                world.LogHandler.Log(Log.Types.LeaveGuild, ctx.Player.PlayerID, ctx.Player.Guild.ID.ToString());
                ctx.Player.Guild.LeaveGuild(ctx.Player, world);
            }
            else
            {
                if (ctx.Player.Guild.GetRank(ctx.Player) < Guild.GuildRanks.Officer) return;

                string name = ctx.Remainder.Substring(1);
                Player? player = world.PlayerHandler.GetPlayer(name);
                if (player is not null && player.State == Player.States.Ready)
                {
                    if (player.Guild is not null &&
                        player.Guild == ctx.Player.Guild &&
                        ctx.Player.Guild.GetRank(ctx.Player) > ctx.Player.Guild.GetRank(player))
                    {
                        ctx.Player.Guild.LeaveGuild(player, world, true);
                        world.LogHandler.Log(Log.Types.LeaveGuild, player.PlayerID, ctx.Player.Guild.ID.ToString(), ctx.Player.PlayerID);
                    }
                }
                else
                {
                    world.Send(ctx.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
