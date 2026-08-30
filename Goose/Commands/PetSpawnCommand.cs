namespace Goose.Commands
{
    [Command("/petspawn ", Section = "Pets", Help = "Spawn one of your pets.")]
    public sealed class PetSpawnCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, int id)
        {
            var world = ctx.World;

            if (!ctx.Player.Map.CanSpawnPets)
            {
                world.Send(ctx.Player, P.ServerMessage("Pets are disabled in this map."));
                return;
            }

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

            if (match.NextRespawnTime > world.TimeNow)
            {
                decimal wait = ((decimal)(world.TimeNow - match.NextRespawnTime) / world.TimerFrequency);
                wait = Math.Round(wait, 2);

                world.Send(ctx.Player, P.ServerMessage("You must wait " + wait + " seconds to spawn this pet."));
                return;
            }

            match.Spawn(world);
        }
    }
}
