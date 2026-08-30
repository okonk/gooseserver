namespace Goose.Commands
{
    [Command("/tell ", Section = "General", Help = "Send a private message to another player.", Usage = "/tell <target> <message...>")]
    public sealed class TellCommand : BaseCommand
    {
        private const int MaxMessageLength = 300;

        public void Execute(CommandContext ctx, Player target, string[] message)
        {
            var world = ctx.World;

            ctx.Player.UpdateIdleStatus(world);

            string text = string.Join(" ", message);

            if (text.Length > MaxMessageLength) return;

            if (text.Length > 0)
            {
                if (target.State > Player.States.LoadingGame)
                {
                    world.LogHandler.Log(Log.Types.Tell, ctx.Player.PlayerID, text, target.PlayerID, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);

                    if ((target.ToggleSettings & Player.ToggleSetting.Tell) == 0)
                    {
                        world.Send(ctx.Player, P.TellMessage("[tell to] " + target.Name + ": " + text));
                        if (target.ChatFilterEnabled) text = world.ChatFilter.Filter(text);

                        world.Send(target, P.Tell(ctx.Player, text));

                        if (target.IsIdle(world))
                        {
                            world.Send(ctx.Player, P.ServerMessage(target.Name + " is AFK."));
                        }
                    }
                    else
                    {
                        world.Send(ctx.Player, P.ServerMessage(target.Name + " has tells disabled."));
                    }
                }
                else
                {
                    world.Send(ctx.Player, P.TellMessage(target.Name + " is not online."));
                }
            }
        }
    }
}
