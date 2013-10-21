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
                
                world.Send(this.Player, "SCM" + map.FileName + ",1," + map.Name + ",0");

                if (GameSettings.Default.MOTD.Length > 0) 
                {
                    world.Send(this.Player, "$7" + GameSettings.Default.MOTD);
                }
                world.Send(this.Player, "$7There are currently " + 
                                        world.PlayerHandler.PlayerCount + 
                                        " players online.");
                if (GameSettings.Default.ExperienceModifier != 1)
                {
                    world.Send(this.Player, "$7Current experience rate is " + 
                        world.ExperienceModifier + "x.");
                }
                world.Send(this.Player, this.Player.SNFString());
                this.Player.AddRegenEvent(world);
                this.Player.SendInventory(world);
                this.Player.SendSpellbook(world);
                this.Player.SendBuffBar(world);

                if (this.Player.Guild != null)
                {
                    this.Player.Guild.OnlineMembers.Add(this.Player);
                    if (this.Player.Guild.MOTD != "")
                    {
                        world.Send(this.Player, "$2[guild-notice] MOTD: " + this.Player.Guild.MOTD);
                    }
                }

                if (this.Player.Credits > 0)
                {
                    world.Send(this.Player, "$7You have " + this.Player.Credits + " donation credits.");
                }
            }
        }
    }
}
