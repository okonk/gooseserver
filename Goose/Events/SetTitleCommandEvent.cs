using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    class SetTitleCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SetTitleCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready && 
                this.Player.HasPrivilege(AccessPrivilege.SetTitle))
            {
                string[] tokens = ((string)this.Data).Split(" ".ToCharArray(), 3);
                string name, title;
                if (tokens.Length < 2)
                {
                    world.Send(this.Player, P.ServerMessage("/settitle <name> <title>"));
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
                    world.Send(this.Player, P.ServerMessage("Changed title successfully."));

                    if (player.State != Goose.Player.States.NotLoggedIn)
                    {
                        world.Send(player, P.StatusInfo(player));

                        if (player.Map != null)
                        {
                            List<Player> range = player.Map.GetPlayersInRange(player);

                            string packet = P.EraseCharacter(player.LoginID);
                            string packet2 = P.MakeCharacter(player);

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
                    world.Send(this.Player, P.ServerMessage("Couldn't find player."));
                }
            }
        }
    }
}
