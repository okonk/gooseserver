using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Goose.Events
{
    public class ChangePasswordCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ChangePasswordCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

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
                if (password.Length > 10)
                {
                    world.Send(this.Player, P.ServerMessage("Your password needs to be less than 10 characters long."));
                    return;
                }

                byte[] saltBytes = new byte[16];
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                rng.GetNonZeroBytes(saltBytes);

                string salt = Encoding.ASCII.GetString(saltBytes);
                string base64Salt = Convert.ToBase64String(saltBytes);

                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                byte[] data = Encoding.ASCII.GetBytes(salt + password + GameSettings.Default.ServerName);
                data = md5.ComputeHash(data);

                string passwordHash = BitConverter.ToString(data).Replace("-", "").ToLower();

                this.Player.PasswordHash = passwordHash;
                this.Player.PasswordSalt = base64Salt;

                world.Send(this.Player, P.ServerMessage("Your password has been changed."));
            }
        }
    }
}