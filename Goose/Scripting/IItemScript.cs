using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public interface IItemScript
    {
        void OnCreateEvent(Item item, GameWorld world);

        bool OnUseConsumableEvent(Player player, Item item, GameWorld world);

        void OnMeleeEvent(Player player, Item item, GameWorld world);
    }
}
