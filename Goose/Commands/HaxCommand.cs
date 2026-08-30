namespace Goose.Commands
{
    [Command("/hax ", AccessPrivilege.Debug, Section = "Admin", Help = "Send a raw packet to yourself.")]
    public sealed class HaxCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            // Raw remainder on purpose: tokenizing and re-joining would normalize
            // doubled/trailing spaces and corrupt the injected packet.
            ctx.World.Send(ctx.Player, ctx.Remainder);
        }
    }
}
