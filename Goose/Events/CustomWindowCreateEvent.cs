namespace Goose.Events
{
    /**
     * CustomWindowCreateEvent
     *
     * Player requested creation of a custom item
     *
     * Format: CWClookslotid,statsslotid,r,g,b,a,name
     *
     * The name is the raw tail after the sixth comma: it may contain spaces
     * (commas are stripped by CustomItem.SanitizeName).
     *
     */
    public class CustomWindowCreateEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] t = ((string)this.Data).Substring(3).Split(',', 7);
                if (t.Length != 7) return;

                int lookSlotId = 0;
                int statsSlotId = 0;
                int r = 0;
                int g = 0;
                int b = 0;
                int a = 0;

                try
                {
                    lookSlotId = Convert.ToInt32(t[0]);
                    statsSlotId = Convert.ToInt32(t[1]);
                    r = Convert.ToInt32(t[2]);
                    g = Convert.ToInt32(t[3]);
                    b = Convert.ToInt32(t[4]);
                    a = Convert.ToInt32(t[5]);
                }
                catch (Exception)
                {
                    return;
                }

                CustomWindow.Create(world, this.Player, lookSlotId, statsSlotId, r, g, b, a, t[6]);
            }
        }
    }
}
