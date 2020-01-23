using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    /**
     * LoginContinuedEvent, event for LCNT
     * 
     * Called in response to LOKServername
     * Packet format: LCNT
     * 
     * Server responds: SCMMapId,MapVersion,MapName
     * Send Current Map
     * 
     */
    class LoginContinuedEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new LoginContinuedEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.LoadingGame)
            {
                Map map = world.MapHandler.GetMap(this.Player.MapID);

                this.Player.State = Player.States.LoadingMap;
                
                world.Send(this.Player, P.SendCurrentMap(map));

                // send classes
                foreach (Class @class in world.ClassHandler.Classes)
                {
                    world.Send(this.Player, P.ClassUpdate(@class));
                }

                if (GameWorld.Settings.MOTD.Length > 0) 
                {
                    world.Send(this.Player, P.ServerMessage(GameWorld.Settings.MOTD));
                }
                world.Send(this.Player, P.ServerMessage("There are currently " + 
                                        world.PlayerHandler.PlayerCount + 
                                        " players online."));
                if (GameWorld.Settings.ExperienceModifier != 1)
                {
                    world.Send(this.Player, P.ServerMessage("Current experience rate is " + 
                        world.ExperienceModifier + "x."));
                }
                world.Send(this.Player, P.StatusInfo(this.Player));
                this.Player.AddRegenEvent(world);
                this.Player.SendInventory(world);
                this.Player.SendSpellbook(world);
                this.Player.SendBuffBar(world);

                if (this.Player.Guild != null)
                {
                    this.Player.Guild.OnlineMembers.Add(this.Player);
                    if (this.Player.Guild.MOTD != "")
                    {
                        world.Send(this.Player, P.GuildMessage("[guild-notice] MOTD: " + this.Player.Guild.MOTD));
                    }
                }

                if (this.Player.Credits > 0)
                {
                    world.Send(this.Player, P.ServerMessage("You have " + this.Player.Credits + " donation credits."));
                }
            }
        }
    }
}
