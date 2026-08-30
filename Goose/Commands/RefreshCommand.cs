using Goose.Events;

namespace Goose.Commands
{
    [Command("/refresh", Section = "General", Help = "Resynchronize your position with the server.")]
    public sealed class RefreshCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            RefreshPositionEvent.Refresh(ctx.Player, ctx.World);
        }
    }
}
