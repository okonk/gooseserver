using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class GMBroadcastCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMBroadcastCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.Access == Player.AccessStatus.GameMaster)
            {
                string data = ((string)this.Data).Substring(11);

                if (data.Length <= 0) return;

                world.SendToAll("$7" + data);
            }
        }
    }
}
