using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public class BaseItemScript : IItemScript
    {
        public BaseItemScript() { }

        public virtual void OnCreateEvent(Item item, GameWorld world)
        {

        }

        public virtual bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
        {
            return true;
        }

        public virtual void OnMeleeEvent(Player player, Item item, GameWorld world)
        {

        }

        public virtual string CanPickup(Player player, Item item, GameWorld world)
        {
            return null;
        }

        public virtual bool OnRollModifiersEvent(Item item, GameWorld world)
        {
            return false;
        }

        public virtual bool OnRerollModifiersEvent(Item item, GameWorld world)
        {
            return false;
        }

        public virtual void OnEquipEvent(Player player, Item item, GameWorld world)
        {

        }

        public virtual void OnUnequipEvent(Player player, Item item, GameWorld world)
        {

        }
    }
}
