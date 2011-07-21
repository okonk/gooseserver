using System;
using System.Collections.Generic;
using System.Linq;
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
        public static Event Create(Player player, Object data)
        {
            Event e = new ToggleGroupCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.GroupInvitesEnabled = !this.Player.GroupInvitesEnabled;

                if (this.Player.GroupInvitesEnabled)
                {
                    world.Send(this.Player, "$3Group invitations are now enabled.");
                }
                else
                {
                    world.Send(this.Player, "$3Group invitations have been disabled.");
                }
            }
        }
    }
}
