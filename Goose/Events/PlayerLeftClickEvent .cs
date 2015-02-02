using Goose.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * PlayerLeftClickEvent, Player left clicked
     * 
     * Packet format: LCx,y
     * 
     */
    public class PlayerLeftClickEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PlayerLeftClickEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int x = 0;
                int y = 0;

                string data = ((string)this.Data).Substring(2);
                if (data.Length >= 3)
                {
                    string[] t = data.Split(",".ToCharArray());

                    if (t.Length == 2)
                    {
                        try
                        {
                            x = Convert.ToInt32(t[0]);
                            y = Convert.ToInt32(t[1]);
                        }
                        catch (Exception)
                        {
                            x = 0; 
                            y = 0;
                        }
                    }
                }

                if (x <= 0 || y <= 0 || x > this.Player.Map.Width || y > this.Player.Map.Height)
                {
                    // log bad left click
                    return;
                }

                // Look for any vendors
                List<NPC> range = this.Player.Map.GetNPCsInRange(this.Player);
                foreach (NPC npc in range)
                {
                    if (npc.MapX == x && npc.MapY == y && npc.Quests.Any())
                    {
                        QuestWindow.Handle(npc, this.Player, world);
                        return;
                    }
                }
            }
        }
    }
}
