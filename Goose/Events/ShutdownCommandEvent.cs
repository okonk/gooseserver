using System.Text;

namespace Goose.Events
{
    /**
     * Called when GM types /shutdown
     * 
     */
    public class ShutdownCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Running = false;
            }
        }
    }
}
