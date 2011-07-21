﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class RespawnMapCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new RespawnMapCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready && 
                this.Player.Access == Player.AccessStatus.GameMaster)
            {
                foreach (NPC npc in this.Player.Map.NPCs) {
                    if (npc.State == NPC.States.Dead)
                    {
                        npc.Spawn(world);
                    }
                }

                world.SendToMap(this.Player.Map, "$7Respawned all NPCs.");
            }
        }
    }
}