namespace Goose.Commands
{
    [Command("/pets", Section = "Pets", Help = "Open the pet menu.")]
    public sealed class PetsCommand : BaseCommand
    {
        public void Execute(CommandContext ctx) => PetMenu.Open(ctx.Player, ctx.World);
    }
}
