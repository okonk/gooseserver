using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GMGiveExperienceCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMGiveExperienceCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.Access == Player.AccessStatus.GameMaster)
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
                    world.Send(this.Player, "$7Player " + name + " doesn't exist.");
                    return;
                }

                player.Experience += exp;

                world.Send(this.Player, "$7Added experience successfully.");

                if (player.State != Goose.Player.States.NotLoggedIn)
                {
                    world.Send(player, player.SNFString());
                    world.Send(player, player.TNLString());
                }
                else
                {
                    player.SaveToDatabase(world);
                }

                world.LogHandler.Log(Log.Types.GiveExperience,
                    this.Player.PlayerID, exp.ToString() + " to " + player.PlayerID,
                    this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
