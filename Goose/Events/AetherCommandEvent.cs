using System.Text;

namespace Goose.Events
{
    public class AetherCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string data = ((string)this.Data).Substring(8);
                if (data.Length <= 0) return;

                decimal thres = 0;

                try
                {
                    thres = Convert.ToDecimal(data);
                }
                catch (Exception)
                {
                    thres = 0;
                }

                this.Player.AetherThreshold = thres;
            }
        }
    }
}
