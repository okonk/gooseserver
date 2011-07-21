using System;
using System.Collections.Generic;
using System.Linq;
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
