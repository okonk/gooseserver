using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * TellEvent, event for "/tell " packet
     * 
     * Called when someone types /tell name message
     * Packet format: /tell name message
     * 
     * Server responds: $6[tell to] name: message, to sender and
     * $6[tell from] sender: message, to name
     * 
     * 
     */
    class TellEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new TellEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready)
            {
                this.Player.UpdateIdleStatus(world);

                string info = ((string)this.Data).Substring(6);
                int index = info.IndexOf(' ');
                if (index != -1)
                {
                    string name = info.Substring(0, info.IndexOf(' '));
                    string message = info.Substring(name.Length + 1);

                    if (message.Length > 0)
                    {
                        Player recipient = world.PlayerHandler.GetPlayer(name);
                        if (recipient != null && recipient.State > Player.States.LoadingGame)
                        {
                            world.LogHandler.Log(Log.Types.Tell, this.Player.PlayerID, message, recipient.PlayerID, this.Player.Map.ID, this.Player.MapX, this.Player.MapY);

                            if ((recipient.ToggleSettings & Player.ToggleSetting.Tell) == 0)
                            {
                                world.Send(this.Player, P.TellMessage("[tell to] " + recipient.Name + ": " + message));
                                if (recipient.ChatFilterEnabled) message = world.ChatFilter.Filter(message);
                                
                                world.Send(recipient, P.Tell(this.Player, message));

                                if (recipient.IsIdle(world))
                                {
                                    world.Send(this.Player, P.ServerMessage(recipient.Name + " is AFK."));
                                }
                            }
                            else
                            {
                                world.Send(this.Player, P.ServerMessage(recipient.Name + " has tells disabled."));
                            }
                        }
                        else
                        {
                            world.Send(this.Player, P.TellMessage(name + " is not online."));
                        }
                    }
                }
            }
        }
    }
}
