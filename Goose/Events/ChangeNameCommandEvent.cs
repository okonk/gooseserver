using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class ChangeNameCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ChangeNameCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.ChangeName))
            {
                string[] tokens = ((string)this.Data).Split(' ');

                if (tokens.Length < 3)
                {
                    world.Send(this.Player, "$7/changename <oldname> <newname>: not enough parameters.");
                    return;
                }

                string oldname = tokens[1];
                string newname = tokens[2];

                Player playerCheck = world.PlayerHandler.GetPlayerFromData(newname);
                if (playerCheck != null)
                {
                    world.Send(this.Player, "$7New name " + newname + " is already used.");
                    return;
                }

                Player player = world.PlayerHandler.GetPlayerFromData(oldname);
                if (player == null)
                {
                    world.Send(this.Player, "$7Old name " + oldname + " doesn't exist.");
                    return;
                }

                world.PlayerHandler.RemovePlayerFromData(player);
                player.Name = newname;
                world.PlayerHandler.AddPlayerToData(player);

                world.Send(this.Player, "$7Changed name successfully.");

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
        }
    }
}
