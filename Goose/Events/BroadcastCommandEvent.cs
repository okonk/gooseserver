using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class BroadcastCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new BroadcastCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.Broadcast))
            {
                string data = ((string)this.Data).Substring(11);

                if (data.Length <= 0) return;

                world.SendToAll(P.ServerMessage(string.Format("[{0}]: {1}", this.Player.Access.ToString().Replace("Master", " Master"), data)));
            }
        }
    }
}
