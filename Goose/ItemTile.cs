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
        public string MOBString()
        {
            string rgba = "*";
            if (this.ItemSlot.Item.GraphicA != 0)
            {
                rgba = this.ItemSlot.Item.GraphicR + "," +
                this.ItemSlot.Item.GraphicG + "," +
                this.ItemSlot.Item.GraphicB + "," +
                this.ItemSlot.Item.GraphicA;
            }

            return "MOB" +
                   this.ItemSlot.Item.GraphicTile + "," +
                   this.X + "," +
                   this.Y + "," +
                   this.ItemSlot.Item.Name + (this.ItemSlot.Item.IsBindOnPickup ? " (BoP)" : "") + "," +
                   this.ItemSlot.Stack + "," +
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
