namespace Goose.Commands
{
    [Command("/guildmotd", Section = "Guild", Help = "Set or clear your guild's MOTD.")]
    public sealed class GuildMotdCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] message)
        {
            var world = ctx.World;

            if (ctx.Player.Guild is null) return;
            if (ctx.Player.Guild.GetRank(ctx.Player) < Guild.GuildRanks.Officer) return;

            string motd = string.Join(" ", message);
            if (motd.Length <= 0)
            {
                ctx.Player.Guild.MOTD = "";
                ctx.Player.Guild.Dirty = true;
            }
            else
            {
                ctx.Player.Guild.MOTD = motd;
                ctx.Player.Guild.Dirty = true;
            }

            ctx.Player.Guild.SendToGuild(P.GuildMessage("[guild-notice] MOTD: " + ctx.Player.Guild.MOTD), world);
        }
    }
}
