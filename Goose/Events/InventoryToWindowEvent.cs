using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * InventoryToWindowEvent
     * 
     * 
     */
    class InventoryToWindowEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new InventoryToWindowEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int invslot, windowid, windowslot;
                string data = ((string)this.Data).Substring(3);

                try
                {
                    string[] t = data.Split(",".ToCharArray());
                    invslot = Convert.ToInt32(t[0]);
                    windowid = Convert.ToInt32(t[1]);
                    windowslot = Convert.ToInt32(t[2]);
                }
                catch (Exception)
                {
                    invslot = 0;
                    windowid = 0;
                    windowslot = 0;
                }

                if (invslot <= 0 || invslot > GameWorld.Settings.InventorySize) return;
                if (windowid <= 0) return;

                foreach (Window window in this.Player.Windows)
                {
                    if (window.ID == windowid)
                    {
                        window.InventoryToWindow(this.Player, invslot, windowslot, world);
                        return;
                    }
                }
            }
        }
    }
}
