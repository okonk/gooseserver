using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class ScriptTimerEvent : Event
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public override void Ready(GameWorld world)
        {
            try
            {
                var fun = (Action)this.Data;
                fun();
            }
            catch (Exception e)
            {
                log.Error(e, "Script Timer Exception");
            }
        }

        public static ScriptTimerEvent Create(Action action, TimeSpan period, GameWorld world)
        {
            var e = new ScriptTimerEvent();
            e.Data = action;
            e.Ticks += (long)(world.TimerFrequency * period.TotalSeconds);

            world.EventHandler.AddEvent(e);

            return e;
        }

        public ScriptTimerEvent Reschedule(TimeSpan period, GameWorld world)
        {
            this.Ticks = world.TimeNow + (long)(world.TimerFrequency * period.TotalSeconds);

            world.EventHandler.AddEvent(this);

            return this;
        }
    }
}
