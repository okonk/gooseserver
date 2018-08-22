using System;
using System.Collections.Generic;
using System.Linq;
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

        public ItemSlot ItemSlot { get; set; }

        /**
         * If dropped by NPC only this player can pick up until pickuptime below
         * 
         */
        public Player Owner { get; set; }
        public long PickupTime { get; set; }

        public long DroppedTime { get; set; }

        /**
         * MOBString, create Map Object string
         * 
         */
        public string MOBString(bool dropAnimation = false)
        {
            string rgba = "*";
            if (this.ItemSlot.Item.GraphicA != 0)
            {
                rgba = this.ItemSlot.Item.GraphicR + "," +
                this.ItemSlot.Item.GraphicG + "," +
                this.ItemSlot.Item.GraphicB + "," +
                this.ItemSlot.Item.GraphicA;
            }

            return "DOB" +
                   this.ItemSlot.Item.GraphicTile + "," +
                   this.ItemSlot.Item.GraphicFile + "," +
                   0 + "," + // sound id
                   0 + "," + // sound file
                   this.X + "," +
                   this.Y + "," +
                   "," + // title
                   this.ItemSlot.Item.Name + "," +
                   "," + // surname
                   this.ItemSlot.Stack + "," +
                   0 + "," + // not sure
                   this.ItemSlot.Item.Flags + "," +
                   (dropAnimation ? 1 : 0) + "," + // drop animation if 1
                   rgba;
        }

        /**
         * EOBString, erase map object string
         * 
         */
        public string EOBString()
        {
            return "EOB" + this.X + "," + this.Y;
        }
    }
}
