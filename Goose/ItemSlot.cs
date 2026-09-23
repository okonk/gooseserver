using System.ComponentModel;
using System.Text;

namespace Goose
{
    /**
     * ItemSlot, holds an item and stack size
     *
     * Used for inventory, bank, combine bag slots
     *
     */
    public class ItemSlot
    {
        public Item Item { get; set; } = null!;
        [DefaultValue(1)]
        public long Stack { get; set; } = 1;

        /**
         * CanStack, returns true if other slot can stack with this slot
         *
         * Note: Doesn't do null checking
         *
         */
        public bool CanStack(ItemSlot other, long amount)
        {
            if (this.Item.StackSize == 1) return false;
            if (other.Item.StackSize == 1) return false;
            if (this.Item.StackSize != other.Item.StackSize) return false;
            if (!SameStackIdentity(this.Item, other.Item)) return false;
            if (this.Item.StackSize == 0) return true;
            if (this.Stack + amount > this.Item.StackSize) return false;

            return true;
        }

        /// <summary>Same template and the same per-instance fields a merge cannot preserve:
        /// the surviving stack keeps its own name, look and script params, so merging items
        /// that differ in them silently rewrites the other one. One template can carry many
        /// variants (the /hairdye potions are one template with a per-item colour), and every
        /// merge path funnels through CanStack: Inventory.AddItem, Inventory.SwapSlots, the
        /// bank and combine-bag drags (ItemContainerWindow), and ground drops (Map.cs:391).
        ///
        /// Bound state is deliberately not compared - PickupItemEvent.cs:114 stamps IsBound
        /// on the item it just merged away, so a bound/unbound pair must stay mergeable.</summary>
        public static bool SameStackIdentity(Item a, Item b)
        {
            return a.TemplateID == b.TemplateID
                && a.Name == b.Name
                && a.Description == b.Description
                && a.ScriptParams == b.ScriptParams
                && a.GraphicR == b.GraphicR
                && a.GraphicG == b.GraphicG
                && a.GraphicB == b.GraphicB
                && a.GraphicA == b.GraphicA;
        }

        public bool CanStack(ItemSlot other)
        {
            return CanStack(other, other.Stack);
        }

        public static void SwapSlots(ref ItemSlot? from, ref ItemSlot? to)
        {
            if (from == to) return;

            if (from is null && to is null) return;

            if (from is null || to is null)
            {
                ItemSlot? temp = from;
                from = to;
                to = temp;
            }
            // Same base item and they can stack
            else if (from.Item.TemplateID == to.Item.TemplateID && to.CanStack(from))
            {
                to.Stack += from.Stack;
                from = null;
            }
            else
            {
                ItemSlot temp = from;
                from = to;
                to = temp;
            }
        }
    }
}
