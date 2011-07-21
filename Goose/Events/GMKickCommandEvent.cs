﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /kick player
     * 
     * kicks player from server
     * 
     */
    public class GMKickCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMKickCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.Access == Player.AccessStatus.GameMaster)
            {
                string name = ((string)this.Data).Substring(6);
                Player player = world.PlayerHandler.GetPlayer(name);
                if (player != null)
                {
                    world.LostConnection(player.Sock);
                }
                else
                {
                    world.Send(this.Player, "$7Couldn't find player.");
                }
            }
        }
    }
}
