using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * /setaccess player level
     * 
     */
    public class SetAccessCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new SetAccessCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.SetAccess))
            {
                string[] tokens = ((string)this.Data).Split(" ".ToCharArray(), 3);
                if (tokens.Length < 3)
                {
                    world.Send(this.Player, P.ServerMessage("/setaccess <playername> <" + string.Join("|", Enum.GetNames(typeof(Player.AccessStatus))).ToLower() + ">"));
                    return;
                }

                string name = tokens[1];
                string access = tokens[2];

                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player != null)
                {
                    try
                    {
                        var accessStatus = Enum.GetValues(typeof(Player.AccessStatus)).Cast<Player.AccessStatus>().Where(y => y.ToString().Equals(access, StringComparison.OrdinalIgnoreCase)).First();
                        player.Access = accessStatus;
                        world.Send(this.Player, P.ServerMessage(string.Format("Set AccessStatus for {0} to {1}.", player.Name, player.Access)));

                        if (player.State == Goose.Player.States.NotLoggedIn)
                        {
                            player.SaveToDatabase(world);
                        }
                    }
                    catch
                    {
                        world.Send(this.Player, P.ServerMessage("Couldn't parse access value."));
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
