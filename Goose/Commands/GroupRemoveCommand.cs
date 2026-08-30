namespace Goose.Commands
{
    [Command("/disband", "/groupremove", Section = "Party", Help = "Leave your group or remove a player from it.")]
    public sealed class GroupRemoveCommand : BaseCommand
    {
        public void Execute(CommandContext ctx)
        {
            var world = ctx.World;

            // Legacy Split(' ') without RemoveEmptyEntries: no token means leave group,
            // an empty token is a silent no-op.
            if (ctx.Remainder.Length == 0)
            {
                if (ctx.Player.Group is null)
                {
                    world.Send(ctx.Player, P.GroupMessage("You are not in a group."));
                    return;
                }
                else
                {
                    ctx.Player.Group.RemovePlayer(ctx.Player, world, false, ctx.Player);
                    world.LogHandler.Log(Log.Types.LeaveGroup, ctx.Player.PlayerID, "", 0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
                    return;
                }
            }

            string name = ctx.Remainder.Trim();
            if (name.Length <= 0) return;

            Player? player = world.PlayerHandler.GetPlayer(name);
            if (player is not null)
            {
                if (player.Group is null)
                {
                    world.Send(ctx.Player, P.GroupMessage("Player is not in a group."));
                    return;
                }

                if (ctx.Player.Group == player.Group)
                {
                    ctx.Player.Group.RemovePlayer(player, world, (ctx.Player != player), ctx.Player);
                    world.LogHandler.Log(Log.Types.LeaveGroup, player.PlayerID, "", ctx.Player.PlayerID, player.MapID, player.MapX, player.MapY);
                }
                else
                {
                    world.Send(ctx.Player, P.GroupMessage("Player isn't in your group."));
                    return;
                }
            }
            else
            {
                world.Send(ctx.Player, P.GroupMessage("Couldn't find player."));
            }
        }
    }
}
