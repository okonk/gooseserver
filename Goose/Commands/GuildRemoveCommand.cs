namespace Goose.Commands
{
    [Command("/guildremove", Section = "Guild", Help = "Leave your guild or remove a player from it.")]
    public sealed class GuildRemoveCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player? name = null)
        {
            var world = ctx.World;

            if (ctx.Player.Guild is null) return;

            if (name is null)
            {
                world.LogHandler.Log(Log.Types.LeaveGuild, ctx.Player.PlayerID, ctx.Player.Guild.ID.ToString());
                ctx.Player.Guild.LeaveGuild(ctx.Player, world);
                return;
            }

            if (ctx.Player.Guild.GetRank(ctx.Player) < Guild.GuildRanks.Officer) return;

            if (name.State != Player.States.Ready)
            {
                ctx.Send($"Couldn't find player {name.Name}.");
                return;
            }

            if (name.Guild is not null &&
                name.Guild == ctx.Player.Guild &&
                ctx.Player.Guild.GetRank(ctx.Player) > ctx.Player.Guild.GetRank(name))
            {
                ctx.Player.Guild.LeaveGuild(name, world, true);
                world.LogHandler.Log(Log.Types.LeaveGuild, name.PlayerID, ctx.Player.Guild.ID.ToString(), ctx.Player.PlayerID);
            }
        }
    }
}
