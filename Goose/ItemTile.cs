using System.Text;

namespace Goose
{
    /**
     * ItemTile
     * 
     * Holds an x,y and an ItemSlot
     * 
     */
    public class ItemTile : ITile
    {
        public int X { get; set; }
        public int Y { get; set; }

        public ItemSlot ItemSlot { get; set; } = null!;

        /**
         * If dropped by NPC only this player can pick up until pickuptime below
         * 
         */
        public Player Owner { get; set; } = null!;
        public long PickupTime { get; set; }

        public long DroppedTime { get; set; }
    }
}
