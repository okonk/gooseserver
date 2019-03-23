using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Goose
{
    public abstract class Event
    {
        public long Ticks { get; set; }
        public Player Player { get; set; }
        public Object Data { get; set; }
        public NPC NPC { get; set; }

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        /**
         * Constructor
         * 
         * Ticks defaults to the current time
         */
        public Event()
        {
            long highres;
            QueryPerformanceCounter(out highres);
            this.Ticks = highres;
            //this.Ticks = DateTime.Now.Ticks;
        }

        public abstract void Ready(GameWorld world);
    }
}
