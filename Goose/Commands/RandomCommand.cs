namespace Goose.Commands
{
    [Command("/random", Section = "General", Help = "Roll a random number for everyone nearby.")]
    public sealed class RandomCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;

            if ((!ctx.Player.Map.CanChat || ctx.Player.Map.Muted) && !ctx.Player.HasPrivilege(AccessPrivilege.TalkWhileMuted))
            {
                world.Send(ctx.Player, P.HashMessage("Chat is disabled in this map."));
                return;
            }

            int max = 1001;

            int rnd = world.Random.Next(1, max);
            string packet = P.ServerMessage(ctx.Player.Name + " rolls " + rnd + " out of " + (max - 1) + ".");

            world.Send(ctx.Player, packet);
            foreach (var player in ctx.Player.Map.GetPlayersInRange(ctx.Player))
            {
                world.Send(player, packet);
            }
        }
    }
}
