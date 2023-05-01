using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Goose.Events
{
    public class GMSetPasswordCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMSetPasswordCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.SetPassword))
            {
                string[] tokens = ((string)this.Data).Split(" ".ToCharArray(), 3);

                if (tokens.Length != 3)
                {
                    world.Send(this.Player, P.ServerMessage("/setpassword name password"));
                    return;
                }

                Player player = world.PlayerHandler.GetPlayerFromData(tokens[1]);
                if (player == null)
                {
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                    return;
                }

                string password = tokens[2];
                if (password.Length < 3)
                {
                    world.Send(this.Player, P.ServerMessage("Password needs to be more than 3 characters long."));
                    return;
                }
                if (password.Length > 10)
                {
                    world.Send(this.Player, P.ServerMessage("Password needs to be less than 10 characters long."));
                    return;
                }

                player.SetPassword(password);

                world.Send(this.Player, P.ServerMessage("Password has been changed."));

                world.LogHandler.Log(Log.Types.SetPassword,
                    this.Player.PlayerID, $"Set password of {player.Name}",
                    player.PlayerID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}