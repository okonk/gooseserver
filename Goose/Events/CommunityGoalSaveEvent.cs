namespace Goose.Events
{
    class CommunityGoalSaveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            world.CommunityGoals.Save(world);
        }
    }
}
