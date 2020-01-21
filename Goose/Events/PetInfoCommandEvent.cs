using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class PetInfoCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new PetInfoCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string data = ((string)this.Data).Substring(9);
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

                foreach (Window window in this.Player.Windows)
                {
                    if (window.Type == Window.WindowTypes.PetInfo && window.Data == match)
                    {
                        window.Refresh(this.Player, world);
                        return;
                    }
                }

                Window w = new Window();
                w.Title = "Pet Info For ID " + match.PetID;
                w.Type = Window.WindowTypes.PetInfo;
                w.Buttons = "0,0,0,0,0";
                w.Data = match;

                this.Player.Windows.Add(w);
                w.Create(this.Player, world);
            }
        }
    }
}
