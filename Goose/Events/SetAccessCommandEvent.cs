using System.Text;

namespace Goose.Events
{
    /**
     * /setaccess player level
     * 
     */
    public class SetAccessCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] tokens = ((string)this.Data).Split(' ', 3);
                if (tokens.Length < 3)
                {
                    world.Send(this.Player, P.ServerMessage("/setaccess <playername> <" + string.Join("|", Enum.GetNames(typeof(Player.AccessStatus))).ToLower() + ">"));
                    return;
                }

                string name = tokens[1];
                string access = tokens[2];

                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player != null)
                {
                    try
                    {
                        var accessStatus = Enum.GetValues(typeof(Player.AccessStatus)).Cast<Player.AccessStatus>().Where(y => y.ToString().Equals(access, StringComparison.OrdinalIgnoreCase)).First();
                        player.Access = accessStatus;
                        world.Send(this.Player, P.ServerMessage($"Set AccessStatus for {player.Name} to {player.Access}."));

                        if (player.State == Goose.Player.States.NotLoggedIn)
                        {
                            player.SaveToDatabase(world);
                        }
                    }
                    catch
                    {
                        world.Send(this.Player, P.ServerMessage("Couldn't parse access value."));
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
