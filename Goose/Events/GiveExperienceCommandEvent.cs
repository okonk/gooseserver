using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GiveExperienceCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GiveExperienceCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.GiveExperience))
            {
                string packet = (string)this.Data;
                string[] tokens = packet.Split(" ".ToCharArray());
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

                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player == null)
                {
                    world.Send(this.Player, P.ServerMessage("Player " + name + " doesn't exist."));
                    return;
                }

                player.Experience += exp;

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
