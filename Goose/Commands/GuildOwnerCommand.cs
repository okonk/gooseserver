namespace Goose.Commands
{
    [Command("/guildowner ", Section = "Guild", Help = "Transfer guild ownership to another member.")]
    public sealed class GuildOwnerCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player name)
        {
            var world = ctx.World;

            if (ctx.Player.Guild is null) return;
            if (ctx.Player.Guild.GetRank(ctx.Player) < Guild.GuildRanks.Leader) return;

            if (name.State != Player.States.Ready)
            {
                ctx.Send($"Couldn't find player {name.Name}.");
                return;
            }

            if (name.Guild == ctx.Player.Guild && name != ctx.Player)
            {
                ctx.Player.Guild.ChangeOwner(ctx.Player, name, world);
            }
        }
    }
}
