namespace Goose.Commands
{
    [Command("/guild ", Section = "Guild", Help = "Send a message to your guild.")]
    public sealed class GuildCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] message)
        {
            var world = ctx.World;

            if (ctx.Player.Guild is null) return;

            ctx.Player.UpdateIdleStatus(world);

            string text = string.Join(" ", message);
            if (text.Length <= 0) return;

            string packet = P.GuildMessage("[guild] " + ctx.Player.Name + ": " + text);
            string filteredpacket = P.GuildMessage("[guild] " + ctx.Player.Name + ": ");
            bool filtered = false;

            world.LogHandler.Log(Log.Types.GuildChat, ctx.Player.PlayerID, text, ctx.Player.Guild.ID, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);

            List<Player> range = ctx.Player.Guild.OnlineMembers;
            foreach (var player in range)
            {
                if (player.ChatFilterEnabled)
                {
                    if (!filtered)
                    {
                        filteredpacket += world.ChatFilter.Filter(text);
                        filtered = true;
                    }
                    world.Send(player, filteredpacket);
                }
                else
                {
                    world.Send(player, packet);
                }
            }
        }
    }
}
