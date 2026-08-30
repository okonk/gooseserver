namespace Goose.Commands
{
    [Command("/help", Section = "General", Help = "Show command help.")]
    public sealed class HelpCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string? name = null)
        {
            var pages = HelpFormatter.BuildPages(ctx.Player, ctx.Registry, name);
            if (pages is not null)
                HelpWindow.Open(ctx.World, ctx.Player, pages);
        }
    }
}
