using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class PlayerSaveEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State != Player.States.NotLoggedIn)
            {
                this.Player.UpdatePlayTime(world);
                this.Player.SaveToDatabase(world);
                this.Player.AddSaveEvent(world);
            }
        }
    }
}
