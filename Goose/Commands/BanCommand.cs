namespace Goose.Commands
{
    [Command("/ban ", AccessPrivilege.Ban, Section = "GM", Help = "Ban a player from the server.")]
    public sealed class BanCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string name, int? days = null)
        {
            var world = ctx.World;

            Player? player = world.PlayerHandler.GetPlayerFromData(name);
            if (player is not null)
            {
                int daysToBan = days ?? 1000;

                player.Access = Player.AccessStatus.Banned;
                player.UnbanDate = DateTime.Now.AddDays(daysToBan);

                ctx.Send("Banned " + name + " for " + daysToBan + " days.");

                world.LogHandler.Log(Log.Types.Ban, ctx.Player.PlayerID, "", player.PlayerID);

                if (player.State != Player.States.NotLoggedIn)
                {
                    world.LostConnection(player.Sock);
                }
                else
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
