namespace Goose.Commands
{
    [Command("/petspawn ", Section = "Pets", Help = "Spawn one of your pets.")]
    public sealed class PetSpawnCommand : BaseCommand
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

            var result = PetOperations.TrySpawn(ctx.Player, match, world);
            if (!result.Ok)
            {
                world.Send(ctx.Player, P.ServerMessage(result.Message));
            }
        }
    }
}
