namespace Goose.Commands
{
    [Command("/guildadd ", Section = "Guild", Help = "Add a player to your guild.")]
    public sealed class GuildAddCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] name)
        {
            var world = ctx.World;

            if (ctx.Player.Guild is null) return;
            if (ctx.Player.Guild.GetRank(ctx.Player) < Guild.GuildRanks.Officer) return;

            string lookup = string.Join(" ", name);
            Player? player = world.PlayerHandler.GetPlayer(lookup);
            if (player is not null && player.State == Player.States.Ready)
            {
                if (player.Guild is null)
                {
                    ctx.Player.Guild.JoinGuild(player, world);
                    world.LogHandler.Log(Log.Types.JoinGuild, player.PlayerID, ctx.Player.Guild.ID.ToString(), ctx.Player.PlayerID);
                }
            }
            else
            {
                world.Send(ctx.Player, P.ServerMessage("Couldn't find player."));
            }
        }
    }
}
