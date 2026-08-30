namespace Goose.Commands
{
    [Command("/disband", "/groupremove", Section = "Party", Help = "Leave your group or remove a player from it.")]
    public sealed class GroupRemoveCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player? name = null)
        {
            var world = ctx.World;

            if (name is null)
            {
                if (ctx.Player.Group is null)
                {
                    world.Send(ctx.Player, P.GroupMessage("You are not in a group."));
                    return;
                }

                ctx.Player.Group.RemovePlayer(ctx.Player, world, false, ctx.Player);
                world.LogHandler.Log(Log.Types.LeaveGroup, ctx.Player.PlayerID, "", 0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
                return;
            }

            if (name.Group is null)
            {
                world.Send(ctx.Player, P.GroupMessage("Player is not in a group."));
                return;
            }

            if (ctx.Player.Group == name.Group)
            {
                ctx.Player.Group.RemovePlayer(name, world, (ctx.Player != name), ctx.Player);
                world.LogHandler.Log(Log.Types.LeaveGroup, name.PlayerID, "", ctx.Player.PlayerID, ctx.Player.MapID, ctx.Player.MapX, ctx.Player.MapY);
            }
            else
            {
                world.Send(ctx.Player, P.GroupMessage("Player isn't in your group."));
            }
        }
    }
}
