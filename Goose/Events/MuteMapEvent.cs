using System.Text;

namespace Goose.Events
{
    /**
     * /mutemap
     * 
     */
    public class MuteMapEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.Map.Muted = !this.Player.Map.Muted;

                world.SendToMap(this.Player.Map, P.ServerMessage($"Chat is now {(this.Player.Map.Muted ? "muted" : "unmuted")}."));
            }
        }
    }
}
