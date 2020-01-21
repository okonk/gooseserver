using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class PetDeleteCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PetDeleteCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string data = ((string)this.Data).Substring(11);
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
                    world.Send(this.Player, P.ServerMessage("Invalid pet ID."));
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
                    world.Send(this.Player, P.ServerMessage("Couldn't find pet matching ID."));
                    return;
                }

                match.Destroy(world);
                this.Player.Pets.Remove(match);
                match.Delete = true;
                match.SaveToDatabase(world);

            }
        }
    }
}
