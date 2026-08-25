using System.Text;

namespace Goose.Events
{
    public class UnbanCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string name = ((string)this.Data).Substring(7);
                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player != null)
                {
                    player.Access = Player.AccessStatus.Normal;
                    player.UnbanDate = null;
                    world.Send(this.Player, P.ServerMessage("Unbanned " + name + "."));

                    if (player.State == Goose.Player.States.NotLoggedIn)
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
