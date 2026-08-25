using System.Text;

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
