using System.Text;

namespace Goose.Events
{
    /**
     * /ban player
     * 
     * Bans player from server
     * 
     */
    public class BanCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] tokens = ((string)this.Data).Split(' ', 3);

                Player player = world.PlayerHandler.GetPlayerFromData(tokens[1]);
                if (player is not null)
                {
                    int daysToBan;
                    if (tokens.Length <= 2 || !int.TryParse(tokens[2], out daysToBan))
                        daysToBan = 1000;

                    player.Access = Player.AccessStatus.Banned;
                    player.UnbanDate = DateTime.Now.AddDays(daysToBan);

                    world.Send(this.Player, P.ServerMessage("Banned " + tokens[1] + " for " + daysToBan + " days."));

                    world.LogHandler.Log(Log.Types.Ban, this.Player.PlayerID, "", player.PlayerID);

                    if (player.State != Goose.Player.States.NotLoggedIn)
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
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
