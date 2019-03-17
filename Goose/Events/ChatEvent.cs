using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * ChatEvent, event for ";" packet
     * 
     * Called when someone types a message
     * Packet format: ;Message
     * 
     * Server responds: ^LoginID, Name: Message
     * Server sends the response to everyone in the area including the player
     * 
     */
    class ChatEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new ChatEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.UpdateIdleStatus(world);

                if ((!this.Player.Map.CanChat || !this.Player.Map.Muted) && this.Player.HasPrivilege(AccessPrivilege.TalkWhileMuted))
                {
                    world.Send(this.Player, "#Chat is disabled in this map.");
                    return;
                }

                string message = (string)this.Data;
                if (message.Length == 1) return; // log bad chat event
                message = message.Substring(1, message.Length - 1);

                string packet = "^" + this.Player.LoginID + "," + this.Player.Name + ": " + message;
                string filteredpacket = "^" + this.Player.LoginID + "," + this.Player.Name + ": ";
                bool filtered = false;

                world.LogHandler.Log(Log.Types.Chat, this.Player.PlayerID, message, 0, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);

                world.Send(this.Player, packet);
                List<Player> range = this.Player.Map.GetPlayersInRange(this.Player);
                foreach (Player player in range)
                {
                    if (player.ChatFilterEnabled)
                    {
                        if (!filtered) 
                        {
                            filteredpacket += world.ChatFilter.Filter(message);
                            filtered = true;
                        }
                        world.Send(player, filteredpacket);
                    }
                    else
                    {
                        world.Send(player, packet);
                    }
                }
            }
        }
    }
}
