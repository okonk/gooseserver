namespace Goose.Commands
{
    [Command("/guildadd ", Section = "Guild", Help = "Add a player to your guild.")]
    public sealed class GuildAddCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player name)
        {
            var world = ctx.World;

            if (ctx.Player.Guild is null) return;
            if (ctx.Player.Guild.GetRank(ctx.Player) < Guild.GuildRanks.Officer) return;

            if (name.State != Player.States.Ready)
            {
                ctx.Send($"Couldn't find player {name.Name}.");
                return;
            }

            if (name.Guild is null)
            {
                ctx.Player.Guild.JoinGuild(name, world);
                world.LogHandler.Log(Log.Types.JoinGuild, name.PlayerID, ctx.Player.Guild.ID.ToString(), ctx.Player.PlayerID);
            }
        }
    }
}
