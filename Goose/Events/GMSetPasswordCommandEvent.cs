using System.Text;
using System.Security.Cryptography;

namespace Goose.Events
{
    public class GMSetPasswordCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string[] tokens = ((string)this.Data).Split(' ', 3);

                if (tokens.Length != 3)
                {
                    world.Send(this.Player, P.ServerMessage("/setpassword name password"));
                    return;
                }

                Player player = world.PlayerHandler.GetPlayerFromData(tokens[1]);
                if (player is null)
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
                if (password.Length > 16)
                {
                    world.Send(this.Player, P.ServerMessage("Password needs to be 16 characters or fewer."));
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