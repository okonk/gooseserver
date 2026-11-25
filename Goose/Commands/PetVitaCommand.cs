namespace Goose.Commands
{
    [Command("/petvita ", Section = "Pets", Help = "Buy hit points for one of your pets with experience.")]
    public sealed class PetVitaCommand : BaseCommand
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

            var result = PetOperations.BuyVita(match, buys, world);
            world.Send(ctx.Player, P.ServerMessage(result.Message));
        }
    }
}
