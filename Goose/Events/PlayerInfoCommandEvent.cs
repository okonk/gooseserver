using System.Text;

namespace Goose.Events
{
    public class PlayerInfoCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string name = ((string)this.Data).Substring("/playerinfo ".Length);
                Player? player = world.PlayerHandler.GetPlayerFromData(name);
                if (player is not null)
                {
                    PlayerInfoWindow.Open(world, this.Player, player);
                }
                else
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
