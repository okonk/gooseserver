using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class ItemSaveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            world.ItemHandler.Save(world);
        }
    }
}
