using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * GroupChatEvent
     * 
     * /group message
     * 
     * Sends message to everyone in group.
     * 
     */
    public class GroupChatEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new GroupChatEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                if (this.Player.Group == null) return;

                this.Player.UpdateIdleStatus(world);

                string message = ((string)this.Data).Substring(7);
                if (message.Length >= 1)
                {
                    this.Player.Group.Chat(this.Player, message, world);
                    world.LogHandler.Log(Log.Types.GroupChat, this.Player.PlayerID, message, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);
                }
            }
        }
    }
}
