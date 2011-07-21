using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class GMSetTitleCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GMSetTitleCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready && 
                this.Player.Access == Goose.Player.AccessStatus.GameMaster)
            {
                string[] tokens = ((string)this.Data).Split(" ".ToCharArray(), 3);
                string name, title;
                if (tokens.Length < 2)
                {
                    world.Send(this.Player, "$7/settitle <name> <title>");
                    return;
                }
                if (tokens.Length == 2)
                {
                    name = tokens[1];
                    title = "";
                }
                else
                {
                    name = tokens[1];
                    title = tokens[2];
                }

                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player != null)
                {
                    player.Title = title;
                    world.Send(this.Player, "$7Changed title successfully.");

                    if (player.State != Goose.Player.States.NotLoggedIn)
                    {
                        world.Send(player, player.SNFString());

                        if (player.Map != null)
                        {
                            List<Player> range = player.Map.GetPlayersInRange(player);

                            string packet = "ERC" + player.LoginID;
                            string packet2 = player.MKCString();

                            foreach (Player p in range)
                            {
                                world.Send(p, packet);
                                world.Send(p, packet2);
                            }
                        }
                    }
                    else
                    {
                        player.SaveToDatabase(world);
                    }
                }
                else
                {
                    world.Send(this.Player, "$7Couldn't find player.");
                }
            }
        }
    }
}
