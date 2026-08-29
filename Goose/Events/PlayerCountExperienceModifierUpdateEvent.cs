using System.Text;

namespace Goose.Events
{
    public class PlayerCountExperienceModifierUpdateEvent : Event
    {
        public override void Ready(GameWorld world)
        {
            var uniquenonafkips = new HashSet<string>();

            foreach (var player in world.PlayerHandler.Players)
            {
                if (!player.IsIdle(world))
                {
                    if (player.State != Goose.Player.States.NotLoggedIn)
                    {
                        try
                        {
                            string IP = player.Sock.RemoteEndPoint!.ToString()!;
                            IP = IP.Substring(0, IP.IndexOf(":"));

                            uniquenonafkips.Add(IP);
                        }
                        catch (ObjectDisposedException)
                        {
                            // Socket may be disposed mid-enumeration as a player
                            // disconnects; that player simply isn't counted this tick.
                        }
                    }
                }
            }

            decimal oldModifier = world.ExperienceModifier;

            decimal experiencemodifier = uniquenonafkips.Count / world.Settings.PlayerCountExperienceModifierInterval;
            experiencemodifier *= world.Settings.PlayerCountExperienceModifier;

            experiencemodifier += world.Settings.ExperienceModifier;

            world.ExperienceModifier = experiencemodifier;

            if (oldModifier != world.ExperienceModifier)
            {
                world.SendToAll(P.ServerMessage("Experience modifier is now " + world.ExperienceModifier + "x because of " + uniquenonafkips.Count + " active players."));
            }

            // H6: clamp to >= 1, a 0/negative IdleTimeout re-enqueues at now and spins EventHandler.Update
            this.Ticks += world.TimerFrequency * Math.Max(1, world.Settings.IdleTimeout);
            world.EventHandler.AddEvent(this);

            world.LogHandler.Save(world);
        }
    }
}
