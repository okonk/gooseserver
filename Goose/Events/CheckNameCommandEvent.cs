using System.Text;

namespace Goose.Events
{
    public class CheckNameCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string name = ((string)this.Data).Substring(11);
                Player? player = world.PlayerHandler.GetPlayerFromData(name);

                if (player is null)
                {
                    world.Send(this.Player, P.ServerMessage(name + " is currently unused."));
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage(name + " is used."));
                }
            }
        }
    }
}
