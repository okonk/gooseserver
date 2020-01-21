using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GiveCreditsCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GiveCreditsCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string packet = (string)this.Data;
                string[] tokens = packet.Split(" ".ToCharArray());
                if (tokens.Length < 3) return;

                string name = tokens[1];
                int credits = 0;

                try
                {
                    credits = Convert.ToInt32(tokens[2]);
                }
                catch (Exception)
                {
                    credits = 0;
                }

                if (credits <= 0) return;

                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player == null)
                {
                    world.Send(this.Player, P.ServerMessage("Player " + name + " doesn't exist."));
                    return;
                }

                if (this.Player.Credits < credits)
                {
                    world.Send(this.Player, P.ServerMessage("You don't have enough credits."));
                    return;
                }

                player.Credits += credits;
                this.Player.Credits -= credits;

                if (player.State == Player.States.Ready)
                {
                    world.Send(player, P.ServerMessage(this.Player.Name + " gave you " + credits + " donation credits."));
                }
                else
                {
                    player.SaveToDatabase(world);
                }

                world.LogHandler.Log(Log.Types.GaveCredits,
                    this.Player.PlayerID, credits.ToString(), player.PlayerID,
                    this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
