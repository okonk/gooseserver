using System.Text;

namespace Goose.Events
{
    /**
     * InventorySplitEvent, event for "SPLIT" packet
     *
     * Called when someone splits an item stack in their inventory
     * Packet format: SPLITslotid1,slotid2,stackSize
     *
     * StackSize isn't part of official illutia, so should be optional and default to 1 if not specified
     *
     */
    public class InventorySplitEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                int id1 = 0;
                int id2 = 0;
                int amount = 1;
                string[] tokens = ((string)this.Data).Substring(5).Split(',');

                try
                {
                    id1 = Convert.ToInt32(tokens[0]);
                    id2 = Convert.ToInt32(tokens[1]);

                    if (tokens.Length == 3)
                        amount = Convert.ToInt32(tokens[2]);
                }
                catch (Exception)
                {
                    id1 = 0;
                    id2 = 0;
                    amount = 1;
                }

                if (id1 <= 0 || id2 <= 0 ||
                    id1 > world.Settings.InventorySize || id2 > world.Settings.InventorySize)
                {
                    // log something bad about packet
                    return;
                }

                this.Player.Inventory.SplitSlots(id1, id2, amount, world);
            }
        }
    }
}
