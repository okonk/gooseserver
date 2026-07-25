using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GiveGoldCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GiveGoldCommandEvent();
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
                long gold = 0;

                try
                {
                    gold = Convert.ToInt64(tokens[2]);
                }
                catch (Exception)
                {
                    gold = 0;
                }

                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player == null)
                {
                    world.Send(this.Player, P.ServerMessage("Player " + name + " doesn't exist."));
                    return;
                }

                if (player.State != Goose.Player.States.NotLoggedIn)
                {
                    player.AddGold(gold, world);
                    world.Send(player, P.ServerMessage(this.Player.Name + " gave you " + gold + " gold."));
                }
                else
                {
                    player.Gold += gold;
                    player.SaveToDatabase(world);
                }

                world.Send(this.Player, P.ServerMessage("Gave " + gold + " gold to " + player.Name + "."));

                world.LogHandler.Log(Log.Types.GiveGold,
                    this.Player.PlayerID, gold.ToString() + " to " + player.PlayerID,
                    player.PlayerID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
            }
        }
    }
}
