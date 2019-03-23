using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Events
{
    public class MacroCheckCommandEvent : Event
    {
        public static Event Create(Player player, Object data)
        {
            Event e = new MacroCheckCommandEvent();
            e.Player = player;
            e.Data = data;

            return e;
        }

        public override void Ready(GameWorld world)
        {
            if (this.Player.State == Player.States.Ready &&
                this.Player.HasPrivilege(AccessPrivilege.MacroCheck))
            {
                string name = ((string)this.Data).Substring("/macrocheck ".Length);
                Player player = world.PlayerHandler.GetPlayer(name);
                if (player != null)
                {
                    if (player.State != Player.States.Ready)
                    {
                        world.Send(this.Player, "$7Player is still loading a map or not logged in.");
                        return;
                    }

                    if (player.MacroCheckEvent != null)
                    {
                        world.Send(this.Player, "$7Player has already has an active macrocheck.");
                        return;
                    }

                    long timeNow = world.TimeNow;
                    long timeSinceLastCheck = (timeNow - player.LastMacroCheckTime) / world.TimerFrequency;
                    if (timeSinceLastCheck <= TimeSpan.FromHours(2).TotalSeconds)
                    {
                        world.Send(this.Player, "$7Player has already been macrochecked recently.");
                        return;
                    }

                    world.LogHandler.Log(Log.Types.MacroCheck, this.Player.PlayerID, "", player.PlayerID, player.MapID, player.MapX, player.MapY);

                    string code = GenerateMacroCheckCode(world);
                    player.LastMacroCheckTime = timeNow;
                    player.MacroCheckEvent = new MacroCheckEvent();
                    player.MacroCheckEvent.Ticks += (long)(600 * world.TimerFrequency);
                    player.MacroCheckEvent.Player = player;
                    player.MacroCheckEvent.Code = code;
                    world.EventHandler.AddEvent(player.MacroCheckEvent);

                    MacroCheckWindow.Open(world, player, code);
                }
                else
                {
                    world.Send(this.Player, "$7Couldn't find player.");
                }
            }
        }

        private static readonly char[] characters = new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '2', '3', '4', '5', '6', '7', '8', '9' };

        private string GenerateMacroCheckCode(GameWorld world)
        {
            return new string(Enumerable.Range(0, 10).Select(i => characters[world.Random.Next(0, characters.Length)]).ToArray());
        }
    }
}
