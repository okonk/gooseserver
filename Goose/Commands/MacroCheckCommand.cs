using Goose.Events;

namespace Goose.Commands
{
    [Command("/macrocheck ", AccessPrivilege.MacroCheck, Section = "GM", Help = "Start a macro check on a player.")]
    public sealed class MacroCheckCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, Player name)
        {
            var world = ctx.World;

            if (name.State != Player.States.Ready)
            {
                ctx.Send("Player is still loading a map or not logged in.");
                return;
            }

            if (name.MacroCheckEvent is not null)
            {
                ctx.Send("Player has already has an active macrocheck.");
                return;
            }

            long timeNow = world.TimeNow;
            long timeSinceLastCheck = (timeNow - name.LastMacroCheckTime) / world.TimerFrequency;
            if (timeSinceLastCheck <= TimeSpan.FromHours(2).TotalSeconds)
            {
                ctx.Send("Player has already been macrochecked recently.");
                return;
            }

            world.LogHandler.Log(Log.Types.MacroCheck, ctx.Player.PlayerID, "", name.PlayerID, name.MapID, name.MapX, name.MapY);

            string code = GenerateMacroCheckCode(world);
            name.LastMacroCheckTime = timeNow;
            name.MacroCheckEvent = new MacroCheckEvent();
            name.MacroCheckEvent.Ticks += (long)(300 * world.TimerFrequency);
            name.MacroCheckEvent.Player = name;
            name.MacroCheckEvent.Code = code;
            world.EventHandler.AddEvent(name.MacroCheckEvent);

            MacroCheckWindow.Open(world, name, code);
        }

        private static readonly char[] characters = new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '2', '3', '4', '5', '6', '7', '8', '9' };

        private string GenerateMacroCheckCode(GameWorld world)
        {
            return new string(Enumerable.Range(0, 10).Select(i => characters[world.Random.Next(0, characters.Length)]).ToArray());
        }
    }
}
