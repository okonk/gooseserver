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
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.LoadingGame)
            {
                Map? map = world.MapHandler.GetMap(this.Player.MapID);
                if (map is null)
                {
                    log.Error("Player {0}: saved map {1} not found; falling back to starting map {2}",
                        this.Player.Name, this.Player.MapID, world.Settings.StartingMapID);
                    this.Player.MapID = world.Settings.StartingMapID;
                    this.Player.MapX = world.Settings.StartingMapX;
                    this.Player.MapY = world.Settings.StartingMapY;
                    map = world.MapHandler.GetMap(world.Settings.StartingMapID);
                }
                if (map is null)
                {
                    world.Send(this.Player, P.LoginDenied("Server maps are unavailable."));
                    world.GameServer!.Disconnect(this.Player.Sock);
                    return;
                }

                this.Player.State = Player.States.LoadingMap;
                
                world.Send(this.Player, P.SendMapFlags(map));
                world.Send(this.Player, P.SendCurrentMap(map));

                // send classes
                foreach (Class @class in world.ClassHandler.Classes)
                {
                    world.Send(this.Player, P.ClassUpdate(@class));
                }

                if (world.Settings.MOTD.Length > 0)
                {
                    world.Send(this.Player, P.ServerMessage(world.Settings.MOTD));
                }
                world.Send(this.Player, P.ServerMessage("There are currently " + 
                                        world.PlayerHandler.PlayerCount + 
                                        " players online."));
                if (world.Settings.ExperienceModifier != 1)
                {
                    world.Send(this.Player, P.ServerMessage("Current experience rate is " + 
                        world.ExperienceModifier + "x."));
                }
                world.Send(this.Player, P.StatusInfo(this.Player));
                this.Player.AddRegenEvent(world);
                this.Player.SendInventory(world);
                this.Player.SendSpellbook(world);
                this.Player.SendBuffBar(world);

                if (this.Player.Guild is not null)
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
