using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class AetherCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new AetherCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string data = ((string)this.Data).Substring(8);
                if (data.Length <= 0) return;

                decimal thres = 0;

                try
                {
                    thres = Convert.ToDecimal(data);
                }
                catch (Exception)
                {
                    thres = 0;
                }

                this.Player.AetherThreshold = thres;
            }
        }
    }
}
