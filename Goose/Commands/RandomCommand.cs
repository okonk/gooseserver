namespace Goose.Commands
{
    [Command("/random", Section = "General", Help = "Roll a random number for everyone nearby.")]
    public sealed class RandomCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int? max = null)
        {
            var world = ctx.World;

            if ((!ctx.Player.Map.CanChat || ctx.Player.Map.Muted) && !ctx.Player.HasPrivilege(AccessPrivilege.TalkWhileMuted))
            {
                world.Send(ctx.Player, P.HashMessage("Chat is disabled in this map."));
                return;
            }

            int upper = max is null ? 1001 : max.Value + 1;
            if (upper <= 0) upper = 1001;

            int rnd = world.Random.Next(1, upper);
            string packet = P.ServerMessage(ctx.Player.Name + " rolls " + rnd + " out of " + (upper - 1) + ".");

            world.Send(ctx.Player, packet);
            foreach (var player in ctx.Player.Map.GetPlayersInRange(ctx.Player))
            {
                world.Send(player, packet);
            }
        }
    }
}
