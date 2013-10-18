using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * GroupRemoveEvent
     * 
     * /groupremove - removes themselves from group
     * /groupremove player - removes player from group
     * 
     */
    public class GroupRemoveEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GroupRemoveEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                string name = ((string)this.Data).Substring(12);

                if (name == "")
                {
                    if (this.Player.Group == null)
                    {
                        world.Send(this.Player, "$3You are not in a group.");
                        return;
                    }
                    else
                    {
                        this.Player.Group.RemovePlayer(this.Player, world, false, this.Player);
                        world.LogHandler.Log(Log.Types.LeaveGroup, this.Player.PlayerID, "", 0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
                        return;
                    }
                }

                name = name.Substring(1);
                if (name.Length <= 0) return;

                Player player = world.PlayerHandler.GetPlayer(name);
                if (player != null)
                {
                    if (player.Group == null)
                    {
                        world.Send(this.Player, "$3Player is not in a group.");
                        return;
                    }

                    if (this.Player.Group == player.Group)
                    {
                        this.Player.Group.RemovePlayer(player, world, (this.Player != player), this.Player);
                        world.LogHandler.Log(Log.Types.LeaveGroup, player.PlayerID, "", this.Player.PlayerID, player.MapID, player.MapX, player.MapY);
                    }
                    else
                    {
                        world.Send(this.Player, "$3Player isn't in your group.");
                        return;
                    }
                }
                else
                {
                    world.Send(this.Player, "$3Couldn't find player.");
                }
            }
        }
    }
}
