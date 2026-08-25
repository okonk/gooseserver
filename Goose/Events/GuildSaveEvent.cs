using System.Text;

namespace Goose.Events
{
    class GuildSaveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            world.GuildHandler.Save(world);
        }
    }
}
