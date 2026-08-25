using System.Text;

namespace Goose.Events
{
    /**
     * ToggleGroupCommandEvent
     * 
     * Event for /togglegroup command
     * 
     * /togglegroup enables/disables allowing players to add you to a group
     * 
     */
    public class ToggleGroupCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.GroupInvitesEnabled = !this.Player.GroupInvitesEnabled;

                if (this.Player.GroupInvitesEnabled)
                {
                    world.Send(this.Player, P.GroupMessage("Group invitations are now enabled."));
                }
                else
                {
                    world.Send(this.Player, P.GroupMessage("Group invitations have been disabled."));
                }
            }
        }
    }
}
