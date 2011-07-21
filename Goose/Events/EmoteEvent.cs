using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * EmoteEvent
     * 
     * Format: EMOTn
     * 
     * n is 1-12
     * 
     */
    public class EmoteEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new EmoteEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                const int MAX_EMOTES = 12;

                string data = ((string)this.Data).Substring(4);
                if (data.Length <= 0) return;

                int emot = 0;

                try
                {
                    emot = Convert.ToInt32(data);
                }
                catch (Exception)
                {
                    emot = 0;
                }

                if (emot <= 0 || emot > MAX_EMOTES) return;

                string packet = "EMOT" + this.Player.LoginID + "," + emot;
                world.Send(this.Player, packet);
                foreach (Player player in this.Player.Map.GetPlayersInRange(this.Player))
                {
                    world.Send(player, packet);
                }
            }
        }
    }
}
