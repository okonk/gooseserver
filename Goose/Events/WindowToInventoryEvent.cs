using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * WindowToInventoryEvent
     * 
     * 
     */
    class WindowToInventoryEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new WindowToInventoryEvent();
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
                    windowid = Convert.ToInt32(t[0]);
                    windowslot = Convert.ToInt32(t[1]);
                    invslot = Convert.ToInt32(t[2]);
                }
                catch (Exception)
                {
                    invslot = 0;
                    windowid = 0;
                    windowslot = 0;
                }

                if (invslot <= 0 || invslot > GameSettings.Default.InventorySize) return;
                if (windowid <= 0) return;

                foreach (Window window in this.Player.Windows)
                {
                    if (window.ID == windowid)
                    {
                        window.WindowToInventory(this.Player, windowslot, invslot, world);
                        return;
                    }
                }
            }
        }
    }
}
