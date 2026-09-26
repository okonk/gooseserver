namespace Goose.Commands
{
    [Command("/logs", AccessPrivilege.ViewLogs, Section = "GM", Help = "Open the GM log viewer.")]
    public sealed class LogsCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            if (ctx.Player.State != Player.States.Ready)
                return;
            if (!ctx.Player.HasPrivilege(AccessPrivilege.ViewLogs))
                return;
            LogViewerWindow.Open(ctx.Player, ctx.World);
        }
    }
}
