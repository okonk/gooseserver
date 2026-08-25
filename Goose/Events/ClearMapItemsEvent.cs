using System.Text;

namespace Goose.Events
{
    /**
     * Clears all items that have existed longer than world.Settings.ItemGroundExistTime seconds.
     * Checks every world.Settings.ItemGroundSweepTime seconds.
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
                if (existedfor < world.Settings.ItemGroundExistTime) continue;

                remove.Add(item);
            }

            foreach (ItemTile item in remove)
            {
                map.RemoveItem(item, world);
            }

            // H6: clamp to >= 1, a 0/negative sweep time re-enqueues at now and spins EventHandler.Update
            this.Ticks = world.TimeNow + world.TimerFrequency * Math.Max(1, world.Settings.ItemGroundSweepTime);

            world.EventHandler.AddEvent(this);
        }
    }
}
