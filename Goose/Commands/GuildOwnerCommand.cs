namespace Goose.Commands
{
    [Command("/guildowner ", Section = "Guild", Help = "Transfer guild ownership to another member.")]
    public sealed class GuildOwnerCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name)
        {
            var world = ctx.World;

            if (ctx.Player.Guild is null) return;
            if (ctx.Player.Guild.GetRank(ctx.Player) < Guild.GuildRanks.Leader) return;

            Player? player = world.PlayerHandler.GetPlayer(name);
            if (player is not null && player.State == Player.States.Ready)
            {
                if (player.Guild == ctx.Player.Guild && player != ctx.Player)
                {
                    ctx.Player.Guild.ChangeOwner(ctx.Player, player, world);
                }
            }
            else
            {
                world.Send(ctx.Player, P.ServerMessage("Couldn't find player."));
            }
        }
    }
}
