using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public class BaseItemModifierScript : IItemModifierScript
    {
        public BaseItemModifierScript() { }

        public virtual void OnExecuteEvent(ItemModifier modifier, Item item, GameWorld world)
        {

        }
    }
}
