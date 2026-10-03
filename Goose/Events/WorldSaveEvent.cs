namespace Goose.Events
{
    class WorldSaveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            world.WorldState.Save(world);
        }
    }
}
