using System.Collections;
using System.Text;

namespace Goose
{
    public class ItemContainer : IEnumerable<ItemSlot?>
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private ItemSlot?[] slots;

        public event Action<int, ItemSlot?, ItemSlot?>? SlotChanged;

        public int MaxSlots { get => slots.Length; }

        public ItemContainer(int size)
        {
            slots = new ItemSlot[size];
        }

        public void Resize(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size));
            Array.Resize(ref this.slots, size);
        }

        public void SetSlot(int slot, ItemSlot? itemSlot)
        {
            if (slot < 0 || slot >= this.slots.Length)
            {
                log.Error("SetSlot called with out of range slot {0} (container size {1})", slot, this.slots.Length);
                return;
            }

            ItemSlot? existing = this.slots[slot];
            this.slots[slot] = itemSlot;

            if (!ReferenceEquals(existing, itemSlot))
            {
                SlotChanged?.Invoke(slot, existing, itemSlot);
            }
        }

        public void NotifySlotChanged(int index)
        {
            if (index < 0 || index >= this.slots.Length)
            {
                log.Error("NotifySlotChanged called with out of range slot {0} (container size {1})", index, this.slots.Length);
                return;
            }

            SlotChanged?.Invoke(index, slots[index], slots[index]);
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
