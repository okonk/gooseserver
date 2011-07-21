using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class InstaLevelCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new InstaLevelCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.Level < 30 && !this.Player.Class.ClassName.Equals("Commoner"))
                {
                    long exp = 1000000 - this.Player.Experience;
                    this.Player.AddExperience((int)(exp / world.ExperienceModifier), world, Player.ExperienceMessage.Normal);
                }
            }
        }
    }
}
