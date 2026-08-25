using System.Text;

namespace Goose.Events
{
    public class HaxCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Send(this.Player, ((string)this.Data).Substring(5));
            }
        }
    }
}
