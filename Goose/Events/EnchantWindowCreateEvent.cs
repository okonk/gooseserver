namespace Goose.Events
{
    public class EnchantWindowCreateEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] t = ((string)this.Data).Substring(3).Split(',');
                if (t.Length != 3) return;

                int itemSlotId = 0;
                int essenceSlotId = 0;
                int protectionSlotId = 0;

                try
                {
                    itemSlotId = Convert.ToInt32(t[0]);
                    essenceSlotId = Convert.ToInt32(t[1]);
                    protectionSlotId = Convert.ToInt32(t[2]);
                }
                catch (Exception)
                {
                    return;
                }

                EnchantWindow.Confirm(world, this.Player, itemSlotId, essenceSlotId, protectionSlotId);
            }
        }
    }
}
