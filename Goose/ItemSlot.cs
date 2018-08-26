using System;
using System.Collections.Generic;
using System.Linq;
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
        public Item Item { get; set; }
        public long Stack { get; set; }

        /**
         * CanStack, returns true if other slot can stack with this slot
         * 
         * Note: Doesn't do null checking
         * 
         */
        public bool CanStack(ItemSlot other)
        {
            if (this.Item.TemplateID != other.Item.TemplateID) return false;
            if (this.Item.StackSize == 1) return false;
            if (other.Item.StackSize == 1) return false;
            if (this.Item.StackSize != other.Item.StackSize) return false;
            if (this.Item.StackSize == 0) return true;
            if (this.Stack + other.Stack > this.Item.StackSize) return false;

            return true;
        }

        public static void SwapSlots(ref ItemSlot from, ref ItemSlot to)
        {
            if (from == null && to == null) return;

            if (from == null || to == null)
            {
                ItemSlot temp = from;
                from = to;
                to = temp;
            }
            // Same base item and they can stack
            else if (from.Item.TemplateID == to.Item.TemplateID && to.CanStack(from))
            {
                to.Stack += from.Stack;
                from.Item.Delete = true;
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
