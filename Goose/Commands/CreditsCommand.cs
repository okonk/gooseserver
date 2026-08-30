namespace Goose.Commands
{
    [Command("/credits", Section = "General", Help = "Show your donation credit balance.")]
    public sealed class CreditsCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            ctx.World.Send(ctx.Player, P.ServerMessage("You have " + ctx.Player.Credits + " donation credits."));
        }
    }
}
