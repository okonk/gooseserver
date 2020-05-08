using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public interface IItemModifierScript
    {
        void OnExecuteEvent(ItemModifier modifier, Item item, GameWorld world);
    }
}
