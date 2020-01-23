using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class SaveConfigCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SaveConfigCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.Access == Player.AccessStatus.GameMaster)
            {
                // Commented out because this is bad, it saves the settings to some random path in appdata
                //GameWorld.Settings.Save();
                //world.Send(this.Player, "$7Game Settings Saved.");
            }
        }
    }
}