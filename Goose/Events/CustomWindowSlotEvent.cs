namespace Goose.Events
{
    /**
     * CustomWindowSlotEvent
     *
     * Player dropped or cleared a slot in the custom item window
     *
     * Format: CWSlookslotid,statsslotid
     *
     */
    public class CustomWindowSlotEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] t = ((string)this.Data).Substring(3).Split(',');
                if (t.Length != 2) return;

                int lookSlotId = 0;
                int statsSlotId = 0;

                try
                {
                    lookSlotId = Convert.ToInt32(t[0]);
                    statsSlotId = Convert.ToInt32(t[1]);
                }
                catch (Exception)
                {
                    return;
                }

                CustomWindow.HandleSlot(world, this.Player, lookSlotId, statsSlotId);
            }
        }
    }
}
