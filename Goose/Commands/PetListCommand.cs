namespace Goose.Commands
{
    [Command("/petlist", Section = "Pets", Help = "List your pets with their ID, name and level.")]
    public sealed class PetListCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;

            world.Send(ctx.Player, P.ServerMessage("Listing Pets: <ID> <Name> <Level>"));

            foreach (var pet in ctx.Player.Pets)
            {
                world.Send(ctx.Player, P.ServerMessage(pet.PetID + " " + pet.Name + " " + pet.Level));
            }
        }
    }
}
