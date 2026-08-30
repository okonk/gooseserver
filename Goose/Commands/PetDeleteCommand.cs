namespace Goose.Commands
{
    [Command("/petdelete ", Section = "Pets", Help = "Permanently delete one of your pets.")]
    public sealed class PetDeleteCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int id)
        {
            var world = ctx.World;

            if (id <= 0)
            {
                world.Send(ctx.Player, P.ServerMessage("Invalid pet ID."));
                return;
            }

            Pet? match = null;
            foreach (var pet in ctx.Player.Pets)
            {
                if (pet.PetID == id)
                {
                    match = pet;
                    break;
                }
            }

            if (match is null)
            {
                world.Send(ctx.Player, P.ServerMessage("Couldn't find pet matching ID."));
                return;
            }

            match.Destroy(world);
            ctx.Player.Pets.Remove(match);
            match.Delete = true;
            match.SaveToDatabase(world);
        }
    }
}
