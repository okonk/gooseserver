using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Goose
{
    public abstract class Event
    {
        public long Ticks { get; set; }
        public Player Player { get; set; }
        public Object Data { get; set; }
        public NPC NPC { get; set; }

        /**
         * Constructor
         * 
         * Ticks defaults to the current time
         */
        public Event()
        {
            this.Ticks = Stopwatch.GetTimestamp();
        }

        public abstract void Ready(GameWorld world);
    }
}
