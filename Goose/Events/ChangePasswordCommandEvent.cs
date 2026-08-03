using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Goose.Events
{
    public class ChangePasswordCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string password = ((string)this.Data).Substring(16);

                if (password.Length < 3)
                {
                    world.Send(this.Player, P.ServerMessage("Your password needs to be more than 3 characters long."));
                    return;
                }
                if (password.Length > 16)
                {
                    world.Send(this.Player, P.ServerMessage("Your password needs to be 16 characters or fewer."));
                    return;
                }

                this.Player.SetPassword(password);

                world.Send(this.Player, P.ServerMessage("Your password has been changed."));
            }
        }
    }
}