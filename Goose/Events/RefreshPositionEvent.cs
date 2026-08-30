using System.Text;

namespace Goose.Events
{
    /**
     * Called when someone types /refresh
     * 
     */
    public class RefreshPositionEvent : Event
    {
        public static void Refresh(Player player, GameWorld world)
        {
            // Fix the clients position
            world.Send(player, P.SetYourPosition(player));
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                Refresh(this.Player, world);
            }
        }
    }
}
