namespace Goose.Commands
{
    [Command("/togglegroup", Section = "Party", Help = "Toggle whether other players can invite you to their groups.")]
    public sealed class ToggleGroupCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;

            ctx.Player.GroupInvitesEnabled = !ctx.Player.GroupInvitesEnabled;

            if (ctx.Player.GroupInvitesEnabled)
            {
                world.Send(ctx.Player, P.GroupMessage("Group invitations are now enabled."));
            }
            else
            {
                world.Send(ctx.Player, P.GroupMessage("Group invitations have been disabled."));
            }
        }
    }
}
