namespace Goose.Commands
{
    [Command("/approach ", AccessPrivilege.Approach, Section = "GM", Help = "Warp yourself to another player's location.")]
    public sealed class ApproachCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player target)
        {
            if (target.State != Player.States.Ready)
            {
                ctx.Send("Player is still loading a map.");
                return;
            }

            ctx.Player.WarpTo(ctx.World, target.Map, target.MapX, target.MapY);
        }
    }
}
