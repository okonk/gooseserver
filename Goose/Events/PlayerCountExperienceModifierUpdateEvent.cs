using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose.Events
{
    public class PlayerCountExperienceModifierUpdateEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            var uniquenonafkips = new HashSet<string>();

            foreach (Player player in world.PlayerHandler.Players)
            {
                if (!player.IsIdle(world))
                {
                    if (player.State != Goose.Player.States.NotLoggedIn)
                    {
                        try
                        {
                            string IP = player.Sock.RemoteEndPoint.ToString();
                            IP = IP.Substring(0, IP.IndexOf(":"));

                            uniquenonafkips.Add(IP);
                        }
                        catch (ObjectDisposedException)
                        {
                            // eat exception to stop crash
                            // TODO: figure out how to solve the problem in a better way?
                        }
                    }
                }
            }

            decimal oldModifier = world.ExperienceModifier;

            decimal experiencemodifier = uniquenonafkips.Count / GameWorld.Settings.PlayerCountExperienceModifierInterval;
            experiencemodifier *= GameWorld.Settings.PlayerCountExperienceModifier;

            experiencemodifier += GameWorld.Settings.ExperienceModifier;

            world.ExperienceModifier = experiencemodifier;

            if (oldModifier != world.ExperienceModifier)
            {
                world.SendToAll(P.ServerMessage("Experience modifier is now " + world.ExperienceModifier + "x because of " + uniquenonafkips.Count + " active players."));
            }

            this.Ticks += world.TimerFrequency * GameWorld.Settings.IdleTimeout;
            world.EventHandler.AddEvent(this);

            world.LogHandler.Save(world);
        }
    }
}
