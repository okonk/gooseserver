namespace Goose.Events
{
    public class StylistWindowChangeEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] t = ((string)this.Data).Substring(3).Split(',');
                if (t.Length != 2) return;

                int hair = 0;
                int face = 0;

                try
                {
                    hair = Convert.ToInt32(t[0]);
                    face = Convert.ToInt32(t[1]);
                }
                catch (Exception)
                {
                    return;
                }

                StylistWindow.Change(world, this.Player, hair, face);
            }
        }
    }
}
