using System.Text;

namespace Goose.Events
{
    /**
     * Event for /location command
     * 
     */
    public class LocationEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Send(this.Player, P.ServerMessage("You are in " +
                    this.Player.Map.Name + " at " + this.Player.MapX + "," + this.Player.MapY + "."));
            }
        }
    }
}
