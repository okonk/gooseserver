namespace Goose.Commands
{
    [Command("/petinfo ", Section = "Pets", Help = "Show the status window for one of your pets.")]
    public sealed class PetInfoCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int id)
        {
            var world = ctx.World;

            if (id <= 0)
            {
                world.Send(ctx.Player, P.ServerMessage("Invalid pet ID."));
                return;
            }

            Pet? match = PetOperations.Find(ctx.Player, id);

            if (match is null)
            {
                world.Send(ctx.Player, P.ServerMessage("Couldn't find pet matching ID."));
                return;
            }

            PetOperations.ShowInfo(ctx.Player, match, world);
        }
    }
}
