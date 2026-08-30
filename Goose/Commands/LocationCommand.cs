namespace Goose.Commands
{
    [Command("/location", Section = "General", Help = "Show your current map and coordinates.")]
    public sealed class LocationCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            ctx.World.Send(ctx.Player, P.ServerMessage("You are in " +
                ctx.Player.Map.Name + " at " + ctx.Player.MapX + "," + ctx.Player.MapY + "."));
        }
    }
}
