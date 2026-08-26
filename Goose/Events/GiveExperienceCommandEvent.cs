using System.Text;

namespace Goose.Events
{
    class GiveExperienceCommandEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string packet = (string)this.Data;
                string[] tokens = packet.Split(' ');
                if (tokens.Length < 3) return;

                string name = tokens[1];
                long exp = 0;

                try
                {
                    exp = Convert.ToInt64(tokens[2]);
                }
                catch (Exception)
                {
                    exp = 0;
                }

                Player? player = world.PlayerHandler.GetPlayerFromData(name);
                if (player is null)
                {
                    world.Send(this.Player, P.ServerMessage("Player " + name + " doesn't exist."));
                    return;
                }

                // Grant exact amount (no exp modifiers/caps) then run level-up pipeline
                player.Experience += exp;
                player.ProcessLevelUp(world);

                world.Send(this.Player, P.ServerMessage("Added experience successfully."));

                if (player.State != Goose.Player.States.NotLoggedIn)
                {
                    world.Send(player, P.StatusInfo(player));
                    world.Send(player, P.ExpBar(player));
                }
                else
                {
                    player.SaveToDatabase(world);
                }

                world.LogHandler.Log(Log.Types.GiveExperience,
                    this.Player.PlayerID, exp.ToString() + " to " + player.PlayerID,
                    player.PlayerID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
