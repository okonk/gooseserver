using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Goose
{
    public abstract class Event
    {
        public long Ticks { get; set; }
        public Player Player { get; set; } = null!;
        public Object Data { get; set; } = null!;
        public NPC NPC { get; set; } = null!;

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
