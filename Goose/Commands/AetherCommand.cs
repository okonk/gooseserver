namespace Goose.Commands
{
    [Command("/aether ", Section = "General", Help = "Set the aether threshold.")]
    public sealed class AetherCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, double thres)
        {
            ctx.Player.AetherThreshold = thres;
        }
    }
}
