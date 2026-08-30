namespace Goose.Commands
{
    [Command("/givecredits ", Section = "GM", Help = "Give donation credits to a player.")]
    public sealed class GiveCreditsCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, int credits)
        {
            var world = ctx.World;

            if (credits <= 0) return;

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is null)
            {
                ctx.Send("Player " + name + " doesn't exist.");
                return;
            }

            if (ctx.Player.Credits < credits)
            {
                ctx.Send("You don't have enough credits.");
                return;
            }

            player.Credits += credits;
            ctx.Player.Credits -= credits;

            if (player.State == Player.States.Ready)
            {
                world.Send(player, P.ServerMessage(ctx.Player.Name + " gave you " + credits + " donation credits."));
            }
            else
            {
                player.SaveToDatabase(world);
            }

            world.LogHandler.Log(Log.Types.GaveCredits,
                ctx.Player.PlayerID, credits.ToString(), player.PlayerID,
                ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
