namespace Goose.Commands
{
    [Command("/shutdown", AccessPrivilege.Shutdown, Section = "Admin", Help = "Shut down the server.")]
    public sealed class ShutdownCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            ctx.World.Running = false;
        }
    }
}
