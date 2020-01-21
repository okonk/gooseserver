using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * EmoteEvent
     * 
     * Format: EMOTanimId,sheetNum
     * 
     * animId is 1080-1091, sheetNum 8-10
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
                string data = ((string)this.Data).Substring(4);
                if (data.Length <= 0) return;

                string packet = P.Emote(this.Player, data);
                if (packet == null) return;

                world.Send(this.Player, packet);
                foreach (Player player in this.Player.Map.GetPlayersInRange(this.Player))
                {
                    world.Send(player, packet);
                }
            }
        }
    }
}
