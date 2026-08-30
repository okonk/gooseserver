namespace Goose.Commands
{
    [Command("/unban ", AccessPrivilege.Ban, Section = "GM", Help = "Lift a player's ban.")]
    public sealed class UnbanCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name)
        {
            var world = ctx.World;

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is not null)
            {
                player.Access = Player.AccessStatus.Normal;
                player.UnbanDate = null;
                ctx.Send("Unbanned " + name + ".");

                if (player.State == Player.States.NotLoggedIn)
                {
                    player.SaveToDatabase(world);
                }
            }
            else
            {
                ctx.Send("Couldn't find player.");
            }
        }
    }
}
