namespace Goose.Commands
{
    [Command("/petdamage ", Section = "Pets", Help = "Buy weapon damage for one of your pets with experience.")]
    public sealed class PetDamageCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int petid, int buys = 1)
        {
            var world = ctx.World;

            if (buys <= 0)
            {
                world.Send(ctx.Player, P.ServerMessage("Invalid buy amount."));
                return;
            }
            if (petid <= 0)
            {
                world.Send(ctx.Player, P.ServerMessage("Invalid pet id."));
                return;
            }

            var match = PetOperations.Find(ctx.Player, petid);

            if (match is null)
            {
                world.Send(ctx.Player, P.ServerMessage("Couldn't find pet matching ID."));
                return;
            }

            var result = PetOperations.BuyDamage(match, buys, world);
            world.Send(ctx.Player, P.ServerMessage(result.Message));
        }
    }
}
