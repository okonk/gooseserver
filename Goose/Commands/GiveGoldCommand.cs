namespace Goose.Commands
{
    [Command("/givegold ", AccessPrivilege.GiveGold, Section = "GM", Help = "Give gold to a player.")]
    public sealed class GiveGoldCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, long gold)
        {
            var world = ctx.World;

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is null)
            {
                ctx.Send("Player " + name + " doesn't exist.");
                return;
            }

            if (player.State != Player.States.NotLoggedIn)
            {
                player.AddGold(gold, world);
                world.Send(player, P.ServerMessage(ctx.Player.Name + " gave you " + gold + " gold."));
            }
            else
            {
                player.Gold += gold;
                player.SaveToDatabase(world);
            }

            ctx.Send("Gave " + gold + " gold to " + player.Name + ".");

            world.LogHandler.Log(Log.Types.GiveGold,
                ctx.Player.PlayerID, gold.ToString() + " to " + player.PlayerID,
                player.PlayerID, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
