namespace Goose.Events
{
    public class LogSaveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            world.ScheduleLogSave();
            world.LogHandler.Save(world);
        }
    }
}
