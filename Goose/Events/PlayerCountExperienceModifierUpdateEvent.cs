using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Goose.Events
{
    public class PlayerCountExperienceModifierUpdateEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            Hashtable uniquenonafkips = new Hashtable();

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

                            uniquenonafkips[IP] = null;
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

            decimal experiencemodifier = uniquenonafkips.Count / GameSettings.Default.PlayerCountExperienceModifierInterval;
            experiencemodifier *= GameSettings.Default.PlayerCountExperienceModifier;

            experiencemodifier += GameSettings.Default.ExperienceModifier;

            world.ExperienceModifier = experiencemodifier;

            if (oldModifier != world.ExperienceModifier)
            {
                world.SendToAll(P.ServerMessage("Experience modifier is now " + world.ExperienceModifier + "x because of " + uniquenonafkips.Count + " active players."));
            }

            this.Ticks += world.TimerFrequency * GameSettings.Default.IdleTimeout;
            world.EventHandler.AddEvent(this);

            world.LogHandler.Save(world);
        }
    }
}
