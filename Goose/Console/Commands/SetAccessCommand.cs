
namespace Goose.ConsoleCommands
{
    /**
     * SetAccessRequest, a validated /setaccess argument list
     *
     * Holds only what the line said. Whether a player by that name exists is the
     * handler's problem, since answering that needs the world.
     *
     */
    public sealed class SetAccessRequest
    {
        public string Name = null!;
        public Player.AccessStatus Level;
    }

    /**
     * SetAccessCommand, /setaccess <playername> [level]
     *
     * Exists so a server where nobody holds GameMaster can grant it. The in game
     * /setaccess requires AccessPrivilege.SetAccess, which only GameMaster has, so
     * on a fresh server it cannot be used at all.
     *
     */
    public static class SetAccessCommand
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public const string Usage = "/setaccess <playername> [level]";
        public const string Description =
            "Set a player's access level. Defaults to GameMaster. Works on offline players.";

        /**
         * TryParse, validates the argument shape
         *
         * The level defaults to GameMaster, which is the case this command exists
         * for. Extra arguments beyond the level are ignored rather than refused,
         * matching how the in game command splits its input.
         *
         */
        public static bool TryParse(string[] args, out SetAccessRequest? request, out string? error)
        {
            request = null;
            error = null;

            if (args.Length < 1)
            {
                error = "Usage: " + Usage + ". Levels: " + LevelNames();
                return false;
            }

            var level = Player.AccessStatus.GameMaster;

            if (args.Length > 1 && !TryParseLevel(args[1], out level))
            {
                error = "Unknown access level '" + args[1] + "'. Valid: " + LevelNames();
                return false;
            }

            request = new SetAccessRequest { Name = args[0], Level = level };
            return true;
        }

        /**
         * Run, resolves the player and applies the access change
         *
         */
        public static void Run(GameWorld world, string[] args)
        {
            if (!TryParse(args, out SetAccessRequest? request, out string? error))
            {
                Console.WriteLine(error);
                return;
            }

            Player? player = world.PlayerHandler.GetPlayerFromData(request!.Name);
            if (player is null)
            {
                Console.WriteLine("No player named '" + request.Name + "'.");
                log.Info("Console /setaccess failed: no player named {0}.", request.Name);
                return;
            }

            Player.AccessStatus previous = player.Access;
            player.Access = request.Level;

            // Logged in players are covered by the periodic save. An offline one
            // would otherwise hold the change in memory until something else wrote
            // the row, and a restart before then would silently lose the grant.
            if (player.State == Player.States.NotLoggedIn)
            {
                player.SaveToDatabase(world);
            }

            Console.WriteLine("Set " + player.Name + " from " + previous + " to " + player.Access + ".");
            log.Info("Console /setaccess: {0} {1} -> {2}", player.Name, previous, player.Access);
        }

        /**
         * TryParseLevel, matches an access level by name, case insensitively
         *
         * Deliberately not Enum.TryParse: that also accepts numeric strings, so "9"
         * and even undefined values like "42" would parse. Matching the in game
         * /setaccess in Commands/SetAccessCommand.cs means names only.
         *
         */
        public static bool TryParseLevel(string text, out Player.AccessStatus level)
        {
            foreach (var value in Enum.GetValues<Player.AccessStatus>())
            {
                if (value.ToString().Equals(text, StringComparison.OrdinalIgnoreCase))
                {
                    level = value;
                    return true;
                }
            }

            level = Player.AccessStatus.Normal;
            return false;
        }

        private static string LevelNames()
        {
            return string.Join("|", Enum.GetNames<Player.AccessStatus>());
        }
    }
}
