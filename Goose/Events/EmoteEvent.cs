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

                string[] tokens = data.Split(',');
                if (tokens.Length != 2) return;

                int animId = 0;
                if (!int.TryParse(tokens[0], out animId) || animId < 1080 || animId > 1091) return;

                int sheetNum = 0;
                if (!int.TryParse(tokens[1], out sheetNum) || sheetNum < 8 || sheetNum > 10) return;

                string packet = string.Format("EMOT{0},{1},{2}", this.Player.LoginID, animId, sheetNum);
                world.Send(this.Player, packet);
                foreach (Player player in this.Player.Map.GetPlayersInRange(this.Player))
                {
                    world.Send(player, packet);
                }
            }
        }
    }
}
