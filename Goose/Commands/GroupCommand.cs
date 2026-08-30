namespace Goose.Commands
{
    [Command("/group ", Section = "Party", Help = "Send a message to your group.")]
    public sealed class GroupCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] message)
        {
            var world = ctx.World;

            if (ctx.Player.Group is null) return;

            ctx.Player.UpdateIdleStatus(world);

            string text = string.Join(" ", message);
            if (text.Length >= 1)
            {
                ctx.Player.Group.Chat(ctx.Player, text, world);
                world.LogHandler.Log(Log.Types.GroupChat, ctx.Player.PlayerID, text, 0, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
            }
        }
    }
}
