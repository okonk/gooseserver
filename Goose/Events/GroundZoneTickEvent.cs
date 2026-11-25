namespace Goose.Events
{
    public class GroundZoneTickEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            var zone = (GroundZone)this.Data;
            zone.Map.GroundZones.Tick(zone, world.TimeNow, world);
        }
    }
}
