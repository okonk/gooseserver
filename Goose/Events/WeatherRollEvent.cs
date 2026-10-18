namespace Goose.Events
{
    public class WeatherRollEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Map map = (Map)this.Data;
            MapAmbience amb = map.Ambience;

            if (!ReferenceEquals(amb.PendingRoll, this)) return;

            if (amb.TryRollWeather())
            {
                amb.Broadcast(map, world);
            }

            this.Ticks = world.TimeNow + world.TimerFrequency * MapAmbience.WeatherRollSeconds;
            world.EventHandler.AddEvent(this);
        }
    }
}
