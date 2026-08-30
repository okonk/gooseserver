using Goose.Events;

namespace Goose.Commands
{
    [Command("/macrocheck ", AccessPrivilege.MacroCheck, Section = "GM", Help = "Start a macro check on a player.")]
    public sealed class MacroCheckCommand : BaseCommand
    {
        public void Execute(CommandContext ctx, string[] name)
        {
            var world = ctx.World;

            string nameText = string.Join(" ", name);
            Player? player = world.PlayerHandler.GetPlayer(nameText);
            if (player is not null)
            {
                if (player.State != Player.States.Ready)
                {
                    ctx.Send("Player is still loading a map or not logged in.");
                    return;
                }

                if (player.MacroCheckEvent is not null)
                {
                    ctx.Send("Player has already has an active macrocheck.");
                    return;
                }

                long timeNow = world.TimeNow;
                long timeSinceLastCheck = (timeNow - player.LastMacroCheckTime) / world.TimerFrequency;
                if (timeSinceLastCheck <= TimeSpan.FromHours(2).TotalSeconds)
                {
                    ctx.Send("Player has already been macrochecked recently.");
                    return;
                }

                world.LogHandler.Log(Log.Types.MacroCheck, ctx.Player.PlayerID, "", player.PlayerID, player.MapID, player.MapX, player.MapY);

                string code = GenerateMacroCheckCode(world);
                player.LastMacroCheckTime = timeNow;
                player.MacroCheckEvent = new MacroCheckEvent();
                player.MacroCheckEvent.Ticks += (long)(300 * world.TimerFrequency);
                player.MacroCheckEvent.Player = player;
                player.MacroCheckEvent.Code = code;
                world.EventHandler.AddEvent(player.MacroCheckEvent);

                MacroCheckWindow.Open(world, player, code);
            }
            else
            {
                ctx.Send("Couldn't find player.");
            }
        }

        private static readonly char[] characters = new[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '2', '3', '4', '5', '6', '7', '8', '9' };

        private string GenerateMacroCheckCode(GameWorld world)
        {
            return new string(Enumerable.Range(0, 10).Select(i => characters[world.Random.Next(0, characters.Length)]).ToArray());
        }
    }
}
