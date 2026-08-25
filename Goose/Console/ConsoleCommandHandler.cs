using System.Collections.Concurrent;

namespace Goose.ConsoleCommands
{
    /**
     * ConsoleCommand, a dispatch table entry
     *
     */
    public sealed class ConsoleCommand
    {
        public Action<GameWorld, string[]> Run;
        public string Usage;
        public string Description;
    }

    /**
     * ConsoleCommandHandler, commands typed at the server console
     *
     * The game loop is single threaded and owns all player state, and
     * Console.ReadLine blocks. So a background thread does nothing but read lines
     * onto a queue, and the game loop drains that queue on its own thread. Nothing
     * here mutates game state off the game thread.
     *
     * There is no privilege model: physical access to the server console is the
     * authorization, which is the point of the feature. The in game /setaccess
     * requires GameMaster, so on a fresh server nobody can grant it.
     *
     */
    public class ConsoleCommandHandler
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private readonly ConcurrentQueue<string> pending = new();
        private readonly Dictionary<string, ConsoleCommand> commands;

        public ConsoleCommandHandler()
        {
            this.commands = new Dictionary<string, ConsoleCommand>
            {
                { "setaccess", new ConsoleCommand {
                    Run = SetAccessCommand.Run,
                    Usage = SetAccessCommand.Usage,
                    Description = SetAccessCommand.Description } },
                { "who", new ConsoleCommand {
                    Run = WhoCommand.Run,
                    Usage = WhoCommand.Usage,
                    Description = WhoCommand.Description } },
                { "shutdown", new ConsoleCommand {
                    Run = ShutdownCommand.Run,
                    Usage = ShutdownCommand.Usage,
                    Description = ShutdownCommand.Description } },
                { "help", new ConsoleCommand {
                    Run = (world, args) => HelpCommand.Run(this.commands.Values),
                    Usage = HelpCommand.Usage,
                    Description = HelpCommand.Description } }
            };
        }

        /**
         * Start, spawns the reader thread
         *
         * Must be called once, from GameServer.Run before its restart loop. Calling
         * it per restart would leave two threads blocked on stdin, splitting typed
         * input between them at random.
         *
         */
        public void Start()
        {
            // Under systemd or Docker stdin is redirected, ReadLine returns null
            // immediately, and the read loop would spin hot forever. Program.cs makes
            // the same check before Console.ReadKey.
            if (Console.IsInputRedirected)
            {
                log.Info("Console commands disabled: stdin is redirected.");
                return;
            }

            var thread = new Thread(this.ReadLoop)
            {
                IsBackground = true,
                Name = "ConsoleCommands"
            };

            thread.Start();

            log.Info("Console commands enabled. Type /help.");
        }

        /**
         * ReadLoop, the reader thread body
         *
         */
        private void ReadLoop()
        {
            try
            {
                while (true)
                {
                    string line = Console.ReadLine();

                    if (line is null) break; // EOF, nothing more is coming

                    if (!string.IsNullOrWhiteSpace(line)) this.pending.Enqueue(line);
                }
            }
            catch (Exception e)
            {
                // Only console input dies here, not the server.
                log.Error(e, "Console reader stopped.");
            }
        }

        /**
         * Update, runs queued commands on the game thread
         *
         * Called once per tick from GameServer.GameLoop.
         *
         */
        public void Update(GameWorld world)
        {
            while (this.pending.TryDequeue(out string line))
            {
                ParsedCommand parsed = ConsoleCommandParser.Parse(line);

                if (parsed is null) continue;

                if (!this.commands.TryGetValue(parsed.Name, out ConsoleCommand command))
                {
                    Console.WriteLine("Unknown command '" + parsed.Name + "'. Type /help.");
                    continue;
                }

                try
                {
                    command.Run(world, parsed.Args);
                }
                catch (Exception e)
                {
                    // A bad command must not take down the tick.
                    log.Error(e, "Error running console command '{0}'.", parsed.Name);
                    Console.WriteLine("Command failed, see log.");
                }
            }
        }
    }
}
