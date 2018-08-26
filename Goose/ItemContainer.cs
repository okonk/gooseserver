using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose
{
    public class ItemContainer : IEnumerable<ItemSlot>
    {
        private ItemSlot[] slots;

        public int MaxSlots { get { return slots.Length; } }

        public ItemContainer(int size)
        {
            slots = new ItemSlot[size];
        }

        public void SetSlot(int slot, ItemSlot itemSlot)
        {
            this.slots[slot] = itemSlot;
        }

        public ItemSlot GetSlot(int slot)
        {
            return this.slots[slot];
        }

        public IEnumerator<ItemSlot> GetEnumerator()
        {
            return slots.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return slots.GetEnumerator();
        }
    }
}
