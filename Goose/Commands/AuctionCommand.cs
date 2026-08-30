namespace Goose.Commands
{
    [Command("/auction ", Section = "General", Help = "Post a message to the map auction.")]
    public sealed class AuctionCommand : BaseCommand
    {
        private const int MaxMessageLength = 300;

        public void Execute(CommandContext ctx, string[] message)
        {
            var world = ctx.World;

            ctx.Player.UpdateIdleStatus(world);

            string data = string.Join(" ", message);

            if (data.Length > MaxMessageLength) return;

            if (data.Length <= 0) return;

            if ((!ctx.Player.Map.CanAuction || ctx.Player.Map.Muted) && !ctx.Player.HasPrivilege(AccessPrivilege.TalkWhileMuted))
            {
                world.Send(ctx.Player, P.HashMessage("Auction is disabled in this map."));
                return;
            }

            string packet = P.ServerMessage("<Auction> " + ctx.Player.Name + ": " + data);
            string filteredpacket = P.ServerMessage("<Auction> " + ctx.Player.Name + ": ");
            bool filtered = false;

            world.LogHandler.Log(Log.Types.Auction, ctx.Player.PlayerID, data, 0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);

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
