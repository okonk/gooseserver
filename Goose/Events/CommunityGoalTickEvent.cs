namespace Goose.Events
{
    class CommunityGoalTickEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            world.CommunityGoals.Tick(world);
        }
    }
}
