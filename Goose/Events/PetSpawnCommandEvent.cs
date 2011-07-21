using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /// <summary>
    /// Spawns given pet
    /// 
    /// Syntax: /petspawn <id>
    /// </summary>
    class PetSpawnCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PetSpawnCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (!this.Player.Map.CanSpawnPets)
                {
                    world.Send(this.Player, "$7Pets are disabled in this map.");
                    return;
                }

                string data = ((string)this.Data).Substring(10);
                int id = 0;

                try
                {
                    id = Convert.ToInt32(data);
                }
                catch (Exception)
                {
                    id = 0;
                }

                if (id <= 0)
                {
                    world.Send(this.Player, "$7Invalid pet ID.");
                    return;
                }

                Pet match = null;
                foreach (Pet pet in this.Player.Pets)
                {
                    if (pet.PetID == id)
                    {
                        match = pet;
                        break;
                    }
                }

                if (match == null)
                {
                    world.Send(this.Player, "$7Couldn't find pet matching ID.");
                    return;
                }

                if (match.NextRespawnTime > world.TimeNow)
                {
                    decimal wait = ((decimal)(world.TimeNow - match.NextRespawnTime) / world.TimerFrequency);
                    wait = Math.Round(wait, 2);
                
                    world.Send(this.Player, "$7You must wait " + wait + " seconds to spawn this pet.");
                    return;
                }

                match.Spawn(world);
            }
        }
    }
}
