namespace Goose.Commands
{
    [Command("/guildofficer ", Section = "Guild", Help = "Toggle a guild member's officer rank.")]
    public sealed class GuildOfficerCommand : BaseCommand
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
                switch (name.Guild.GetRank(name))
                {
                    case Guild.GuildRanks.Officer:
                        name.Guild.ChangeRank(name, Guild.GuildRanks.Member, world);
                        break;
                    case Guild.GuildRanks.Member:
                        name.Guild.ChangeRank(name, Guild.GuildRanks.Officer, world);
                        break;
                }
            }
        }
    }
}
