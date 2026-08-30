namespace Goose.Commands
{
    [Command("/giveexperience ", AccessPrivilege.GiveExperience, Section = "GM", Help = "Give experience to a player.")]
    public sealed class GiveExperienceCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, long exp)
        {
            var world = ctx.World;

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is null)
            {
                ctx.Send("Player " + name + " doesn't exist.");
                return;
            }

            // Grant exact amount (no exp modifiers/caps) then run level-up pipeline
            player.Experience += exp;
            player.ProcessLevelUp(world);

            ctx.Send("Added experience successfully.");

            if (player.State != Player.States.NotLoggedIn)
            {
                world.Send(player, P.StatusInfo(player));
                world.Send(player, P.ExpBar(player));
            }
            else
            {
                player.SaveToDatabase(world);
            }

            world.LogHandler.Log(Log.Types.GiveExperience,
                ctx.Player.PlayerID, exp.ToString() + " to " + player.PlayerID,
                player.PlayerID, ctx.Player.Map.ID, ctx.Player.MapX, ctx.Player.MapY);
        }
    }
}
