namespace Goose.Commands
{
    [Command("/aether ", Section = "General", Help = "Set the aether threshold.")]
    public sealed class AetherCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] thres)
        {
            if (thres.Length == 0) return;

            string data = string.Join(" ", thres);
            decimal value = 0;

            try
            {
                value = Convert.ToDecimal(data);
            }
            catch (Exception)
            {
                value = 0;
            }

            ctx.Player.AetherThreshold = value;
        }
    }
}
