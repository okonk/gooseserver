namespace Goose.Commands
{
    [Command("/shout ", Section = "General", Help = "Shout a message to everyone on this map.")]
    public sealed class ShoutCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] message)
        {
            var world = ctx.World;

            ctx.Player.UpdateIdleStatus(world);

            string data = string.Join(" ", message);

            if (data.Length <= 0) return;

            if ((!ctx.Player.Map.CanShout || ctx.Player.Map.Muted) && !ctx.Player.HasPrivilege(AccessPrivilege.TalkWhileMuted))
            {
                world.Send(ctx.Player, P.HashMessage("Shouting is disabled in this map."));
                return;
            }

            string packet = P.HashMessage(ctx.Player.Name + " shouts: " + data);
            string filteredpacket = P.HashMessage(ctx.Player.Name + " shouts: ");
            bool filtered = false;

            world.LogHandler.Log(Log.Types.Shout, ctx.Player.PlayerID, data, 0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);

            List<Player> range = ctx.Player.Map.Players;
            foreach (var player in range)
            {
                if (player.ChatFilterEnabled)
                {
                    if (!filtered)
                    {
                        filteredpacket += world.ChatFilter.Filter(data);
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
