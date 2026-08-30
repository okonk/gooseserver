namespace Goose.Commands
{
    [Command("/invite ", "/groupadd ", Section = "Party", Help = "Invite a player to your group.")]
    public sealed class GroupAddCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] name)
        {
            var world = ctx.World;

            string lookup = string.Join(" ", name);
            Player? player = world.PlayerHandler.GetPlayer(lookup);
            if (player is not null && player.State == Player.States.Ready)
            {
                if (player == ctx.Player)
                {
                    world.Send(ctx.Player, P.GroupMessage("You can't group with yourself."));
                    return;
                }
                if (player.Group is not null)
                {
                    world.Send(ctx.Player, P.GroupMessage("Player is already in a group."));
                    return;
                }
                if (!player.GroupInvitesEnabled)
                {
                    world.Send(ctx.Player, P.GroupMessage("Player is not accepting group invitations."));
                    return;
                }

                if (ctx.Player.Group is null)
                {
                    ctx.Player.Group = new Group();
                    ctx.Player.Group.Players.Add(ctx.Player);
                }

                ctx.Player.Group.AddPlayer(player, world, ctx.Player);

                world.LogHandler.Log(Log.Types.JoinGroup, ctx.Player.PlayerID, "", player.PlayerID, player.Map.ID, player.MapX, player.MapY);
            }
            else
            {
                world.Send(ctx.Player, P.GroupMessage("Couldn't find player."));
            }
        }
    }
}
