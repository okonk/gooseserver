using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * OpenCombineBagEvent, opens combine bag
     * 
     * Packet: OCB
     * 
     */
    class OpenCombineBagEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new OpenCombineBagEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                foreach (Window window in this.Player.Windows)
                {
                    if (window.Type == Window.WindowTypes.CombineBag)
                    {
                        window.Refresh(this.Player, world);
                        return;
                    }
                }

                Window w = new Window();
                w.Title = "Combine Bag";
                w.Buttons = "1,0,0,0,0";
                w.Type = Window.WindowTypes.CombineBag;

                this.Player.Windows.Add(w);

                w.Create(this.Player, world);
            }
        }
    }
}
