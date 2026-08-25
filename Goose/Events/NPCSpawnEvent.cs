using System.Text;

namespace Goose.Events
{
    public class NPCSpawnEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.NPC.State == NPC.States.Dead) this.NPC.Spawn(world);
        }
    }
}
