using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class ToggleCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ToggleCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string packet = (string)this.Data;
                string[] tokens = packet.Split(" ".ToCharArray());
                if (tokens.Length < 2) return;

                string cl = tokens[1];

                switch (cl.ToLower())
                {
                    case "exp":
                    case "experience":
                        this.Player.ToggleSettings ^= Player.ToggleSetting.Experience;
                        if ((this.Player.ToggleSettings & Player.ToggleSetting.Experience) == 0)
                        {
                            world.Send(this.Player, "$7Experience display is enabled.");
                        }
                        else
                        {
                            world.Send(this.Player, "$7Experience display is disabled.");
                        }
                        break;
                    case "tell":
                        this.Player.ToggleSettings ^= Player.ToggleSetting.Tell;
                        if ((this.Player.ToggleSettings & Player.ToggleSetting.Tell) == 0)
                        {
                            world.Send(this.Player, "$7Tells are enabled.");
                        }
                        else
                        {
                            world.Send(this.Player, "$7Tells are disabled.");
                        }
                        break;
                    case "swear":
                    case "word":
                    case "curse":
                        this.Player.ToggleSettings ^= Player.ToggleSetting.WordFilter;
                        if ((this.Player.ToggleSettings & Player.ToggleSetting.WordFilter) == 0)
                        {
                            world.Send(this.Player, "$7Word filter is enabled.");
                        }
                        else
                        {
                            world.Send(this.Player, "$7Word filter is disabled.");
                        }
                        break;
                    default:
                        world.Send(this.Player, "$7/toggle [experience|tell|curse]");
                        break;
                }
            }
        }
    }
}
