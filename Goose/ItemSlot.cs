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
    }
}
