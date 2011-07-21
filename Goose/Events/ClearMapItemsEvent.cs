using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * Clears all items that have existed longer than GameSettings.Default.ItemGroundExistTime seconds.
     * Checks every GameSettings.Default.ItemGroundSweepTime seconds.
     * 
     */
    public class ClearMapItemsEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Map map = (Map)this.Data;
            long existedfor; // how long the item has been on the map in seconds
            List<ItemTile> remove = new List<ItemTile>();

            foreach (ItemTile item in map.Items) {
                existedfor = ((world.TimeNow - item.DroppedTime) / world.TimerFrequency);
                if (existedfor < GameSettings.Default.ItemGroundExistTime) continue;

                if (item.ItemSlot.Item.ItemID != 
                    GameSettings.Default.ItemIDStartpoint + GameSettings.Default.GoldItemID)
                {
                    item.ItemSlot.Item.Delete = true;
                }
                remove.Add(item);
            }

            foreach (ItemTile item in remove)
            {
                map.RemoveItem(item, world);
            }

            this.Ticks = world.TimeNow + world.TimerFrequency * GameSettings.Default.ItemGroundSweepTime;

            world.EventHandler.AddEvent(this);
        }
    }
}
