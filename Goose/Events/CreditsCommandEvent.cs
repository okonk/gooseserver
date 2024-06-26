﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class CreditsCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new CreditsCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Send(this.Player, P.ServerMessage("You have " + this.Player.Credits + " donation credits."));
            }
        }
    }
}
