namespace Goose.Events
{
    class ToggleBackpackEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                BackpackWindow.Toggle(world, this.Player);
            }
        }
    }
}
