using System.Collections;
using System.Text;

namespace Goose
{
    public class ItemContainer : IEnumerable<ItemSlot?>
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private ItemSlot?[] slots;

        public int MaxSlots { get => slots.Length; }

        public ItemContainer(int size)
        {
            slots = new ItemSlot[size];
        }

        public void SetSlot(int slot, ItemSlot? itemSlot)
        {
            if (slot < 0 || slot >= this.slots.Length)
            {
                log.Error("SetSlot called with out of range slot {0} (container size {1})", slot, this.slots.Length);
                return;
            }

            this.slots[slot] = itemSlot;
        }

        public ItemSlot? GetSlot(int slot)
        {
            if (slot < 0 || slot >= this.slots.Length)
            {
                log.Error("GetSlot called with out of range slot {0} (container size {1})", slot, this.slots.Length);
                return null;
            }

            return this.slots[slot];
        }

        public IEnumerator<ItemSlot?> GetEnumerator()
        {
            return slots.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return slots.GetEnumerator();
        }
    }
}
