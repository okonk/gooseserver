using System.Text;

namespace Goose.Scripting
{
    public interface IItemModifierScript
    {
        void OnExecuteEvent(ItemModifier modifier, Item item, GameWorld world);
    }
}
