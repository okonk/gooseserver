using System.Text;

namespace Goose.Events
{
    /**
     * Called when someone types /refresh
     * 
     */
    public class RefreshPositionEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                // Fix the clients position
                world.Send(this.Player, P.SetYourPosition(this.Player));
            }
        }
    }
}
