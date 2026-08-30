namespace Goose.Commands
{
    [Command("/invite ", "/groupadd ", Section = "Party", Help = "Invite a player to your group.")]
    public sealed class GroupAddCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player name)
        {
            var world = ctx.World;

            if (name.State != Player.States.Ready)
            {
                ctx.Send($"Couldn't find player {name.Name}.");
                return;
            }

            if (name == ctx.Player)
            {
                world.Send(ctx.Player, P.GroupMessage("You can't group with yourself."));
                return;
            }
            if (name.Group is not null)
            {
                world.Send(ctx.Player, P.GroupMessage("Player is already in a group."));
                return;
            }
            if (!name.GroupInvitesEnabled)
            {
                world.Send(ctx.Player, P.GroupMessage("Player is not accepting group invitations."));
                return;
            }

            if (ctx.Player.Group is null)
            {
                ctx.Player.Group = new Group();
                ctx.Player.Group.Players.Add(ctx.Player);
            }

            ctx.Player.Group.AddPlayer(name, world, ctx.Player);

            world.LogHandler.Log(Log.Types.JoinGroup, ctx.Player.PlayerID, "", name.PlayerID, name.Map.ID, name.MapX, name.MapY);
        }
    }
}
