namespace Goose.Events
{
    public class RespawnRequestEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.Ready || !this.Player.IsDead) return;

            this.Player.Respawn(world);
        }
    }
}
