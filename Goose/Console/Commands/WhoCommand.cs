
namespace Goose.ConsoleCommands
{
    /**
     * WhoCommand, /who
     *
     * Unlike the in game /who this ignores IsGMInvisible and IsWhoInvisible. The
     * console operator should see everyone who is actually connected.
     *
     */
    public static class WhoCommand
    {
        public const string Usage = "/who";
        public const string Description = "List online players with map, level, and access.";

        public static void Run(GameWorld world, string[] args)
        {
            int matches = 0;

            foreach (Player player in world.PlayerHandler.Players)
            {
                if (player is Pet) continue;
                if (player.State != Player.States.Ready) continue;

                Console.WriteLine("[" + (player.Map?.Name ?? "?") + "] " + player.Name +
                                  " (Level " + player.Level + ", " + player.Access + ")");
                matches++;
            }

            Console.WriteLine(matches + " online.");
        }
    }
}
