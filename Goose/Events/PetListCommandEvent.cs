using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /// <summary>
    /// Command that lists all of the player's current pets
    /// </summary>
    public class PetListCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PetListCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                world.Send(this.Player, "$7Listing Pets: <ID> <Name> <Level>");

                foreach (Pet pet in this.Player.Pets)
                {
                    world.Send(this.Player, "$7" + pet.PetID + " " + pet.Name + " " + pet.Level);
                }
            }
        }
    }
}
