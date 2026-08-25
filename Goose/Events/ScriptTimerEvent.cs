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
            // H6: clamp to >= 1 tick, TimeSpan.Zero/negative would schedule at now and spin EventHandler.Update
            e.Ticks += Math.Max(1, (long)(world.TimerFrequency * period.TotalSeconds));

            world.EventHandler.AddEvent(e);

            return e;
        }

        public ScriptTimerEvent Reschedule(TimeSpan period, GameWorld world)
        {
            // H6: clamp to >= 1 tick, TimeSpan.Zero/negative would reschedule at now and spin EventHandler.Update
            this.Ticks = world.TimeNow + Math.Max(1, (long)(world.TimerFrequency * period.TotalSeconds));

            world.EventHandler.AddEvent(this);

            return this;
        }
    }
}
