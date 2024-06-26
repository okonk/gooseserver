﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * WindowButtonClickEvent
     * 
     * Player clicked a button in a window
     * 
     * Format: WBCbuttonid,windowid,npcid,0,0
     * 
     * 0s are currently unknown
     * 
     */
    public class WindowButtonClickEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new WindowButtonClickEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int buttonid = 0;
                int windowid = 0;
                int npcid = 0;
                int id2 = 0;
                int id3 = 0;

                string[] t = ((string)this.Data).Substring(3).Split(",".ToCharArray());

                // log bad packet
                if (t.Length != 5) return;

                try
                {
                    buttonid = Convert.ToInt32(t[0]);
                    windowid = Convert.ToInt32(t[1]);
                    npcid = Convert.ToInt32(t[2]);
                    id2 = Convert.ToInt32(t[3]);
                    id3 = Convert.ToInt32(t[4]);
                }
                catch (Exception)
                {
                    buttonid = -1;
                }

                if (buttonid <= -1 || buttonid >= Enum.GetValues(typeof(Window.ButtonTypes)).Length) return;

                foreach (Window window in this.Player.Windows)
                {
                    if (window.ID == windowid)
                    {
                        window.Clicked((Window.ButtonTypes)buttonid, npcid, id2, id3, this.Player, world);

                        return;
                    }
                }
            }
        }
    }
}
