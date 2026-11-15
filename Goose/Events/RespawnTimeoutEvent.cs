namespace Goose.Events
{
    public class RespawnTimeoutEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Player player = (Player)this.Data;
            if (!player.IsDead || player.PendingRespawnTimeout != this) return;

            player.Respawn(world);
        }
    }
}
