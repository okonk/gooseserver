namespace Goose.Commands
{
    [Command("/guildofficer ", Section = "Guild", Help = "Toggle a guild member's officer rank.")]
    public sealed class GuildOfficerCommand : BaseCommand
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
                    switch (player.Guild.GetRank(player))
                    {
                        case Guild.GuildRanks.Officer:
                            player.Guild.ChangeRank(player, Guild.GuildRanks.Member, world);
                            break;
                        case Guild.GuildRanks.Member:
                            player.Guild.ChangeRank(player, Guild.GuildRanks.Officer, world);
                            break;
                    }
                }
            }
            else
            {
                world.Send(ctx.Player, P.ServerMessage("Couldn't find player."));
            }
        }
    }
}
