namespace Goose.Commands
{
    [Command("/summon ", AccessPrivilege.Summon, Section = "GM", Help = "Warp another player to your location.")]
    public sealed class SummonCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player target)
        {
            if (target.State != Player.States.Ready)
            {
                ctx.Send("Player is still loading a map.");
                return;
            }

            target.WarpTo(ctx.World, ctx.Player.Map, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
