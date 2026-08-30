namespace Goose.Commands
{
    [Command("/guildcreate ", Section = "Guild", Help = "Create a new guild.")]
    public sealed class GuildCreateCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] name)
        {
            var world = ctx.World;

            if (ctx.Player.Gold < world.Settings.GuildCreationCost)
            {
                world.Send(ctx.Player,
                    P.ServerMessage("You need " + world.Settings.GuildCreationCost + "gp to create a guild."));
                return;
            }

            string guildName = string.Join(" ", name);
            if (guildName.Length <= 3 || guildName.Length > 128)
            {
                world.Send(ctx.Player, P.ServerMessage("Your guild name needs to be between 3 and 128 characters."));
                return;
            }

            if (ctx.Player.Guild is not null)
            {
                ctx.Player.Guild.LeaveGuild(ctx.Player, world);
            }

            ctx.Player.RemoveGold(world.Settings.GuildCreationCost, world);
            ctx.Player.Guild = new Guild();
            ctx.Player.Guild.ID = 0;
            ctx.Player.Guild.MOTD = world.Settings.DefaultGuildMOTD;
            ctx.Player.Guild.Name = guildName;
            ctx.Player.Guild.AddMember(ctx.Player.PlayerID, Guild.GuildRanks.Leader, true, true);
            ctx.Player.Guild.OnlineMembers.Add(ctx.Player);

            ctx.Player.Guild.SendToGuild(P.GuildMessage("[guild-notice] MOTD: " + world.Settings.DefaultGuildMOTD), world);

            world.GuildHandler.AddGuild(ctx.Player.Guild);
        }
    }
}
