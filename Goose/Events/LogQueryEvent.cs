namespace Goose.Events
{
    public class LogQueryEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player is not Player player) return;
            if (player.State != Player.States.Ready) return;
            if (!player.HasPrivilege(AccessPrivilege.ViewLogs)) return;
            world.LogSearches.Admit(player, (string)this.Data);
        }
    }
}
