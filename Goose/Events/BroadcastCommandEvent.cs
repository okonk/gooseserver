using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class BroadcastCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string data = ((string)this.Data).Substring(11);

                if (data.Length <= 0) return;

                world.SendToAll(P.ServerMessage(string.Format("[{0}]: {1}", this.Player.Access.ToString().Replace("Master", " Master"), data)));
            }
        }
    }
}
