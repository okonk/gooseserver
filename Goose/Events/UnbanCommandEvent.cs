using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class UnbanCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new UnbanCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.Access == Player.AccessStatus.GameMaster)
            {
                string name = ((string)this.Data).Substring(7);
                Player player = world.PlayerHandler.GetPlayerFromData(name);
                if (player != null)
                {
                    player.Access = Player.AccessStatus.Normal;
                    world.Send(this.Player, "$7Unbanned " + name + ".");

                    if (player.State == Goose.Player.States.NotLoggedIn)
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
