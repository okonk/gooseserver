namespace Goose.Commands
{
    [Command("/mc ", Section = "General", Help = "Confirm a macro check code.")]
    public sealed class MacroConfirmCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] code)
        {
            var world = ctx.World;
            var codeString = string.Join(" ", code);

            if (ctx.Player.MacroCheckEvent is null)
            {
                world.Send(ctx.Player, P.ServerMessage("You don't have a current macrocheck to do."));
                return;
            }

            if (ctx.Player.MacroCheckEvent.Code != codeString)
            {
                world.Send(ctx.Player, P.ServerMessage("Macrocheck code doesn't match.. try again."));
                return;
            }

            world.LogHandler.Log(Log.Types.MacroCheckConfirm, ctx.Player.PlayerID, "", 0, ctx.Player.MapID, ctx.Player.MapX, ctx.Player.MapY);

            ctx.Player.MacroCheckEvent.Passed = true;
            ctx.Player.MacroCheckEvent = null;

            ctx.Player.Experience += 1000000;
            world.Send(ctx.Player, P.ServerMessage("Macrocheck passed. You earned 1mil experience."));
            world.Send(ctx.Player, P.StatusInfo(ctx.Player));
            world.Send(ctx.Player, P.ExpBar(ctx.Player));
        }
    }
}
