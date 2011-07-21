using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /// <summary>
    /// Clears the account limits once a day
    /// </summary>
    public class ClearCreatedHistoryEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            world.CharactersCreatedPerIP.Clear();

            this.Ticks += world.TimerFrequency * 24 * 60 * 60;

            world.EventHandler.AddEvent(this);
        }
    }
}
