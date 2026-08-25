using System.Text;
using System.Runtime.InteropServices;
using NLog;

namespace Goose
{
    class Program
    {
        /**
         * Just starts our GameServer
         *
         */
        static void Main(string[] args)
        {
            // Resolve paths and logging before anything loads settings. With no --datadir,
            // Paths defaults everything to the app base directory, matching historical behaviour.
            Paths.Initialize(ParseDataDir(args));
            ConfigureLogging();

            var settings = GooseSettingsLoader.Load();
            var server = new GameServer(settings);

            // Without these, Ctrl+C or `systemctl stop` killed the process outright with
            // all authoritative state still in memory. The database is a write behind
            // mirror flushed on PlayerSavePeriod, so that discarded up to that much player
            // progress. Both handlers cancel the default terminate and ask the game loop to
            // stop, which runs the normal save and database drain before Main returns.
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                server.RequestShutdown();
            };

            using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;
                server.RequestShutdown();
            });

            server.Run();

            // Interactive stdin is owned by the console command reader thread (started when
            // input is not redirected). Waiting for a key here would hang after clean exit
            // because that thread permanently blocks on Console.ReadLine().
        }

        /// <summary>
        /// Reads --datadir &lt;path&gt; or --datadir=&lt;path&gt; from the command line. The
        /// GOOSE_DATADIR fallback is handled inside Paths.Initialize.
        /// </summary>
        private static string ParseDataDir(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--datadir" && i + 1 < args.Length)
                    return args[i + 1];
                if (args[i].StartsWith("--datadir=", StringComparison.Ordinal))
                    return args[i].Substring("--datadir=".Length);
            }
            return null;
        }

        /// <summary>
        /// Loads NLog.config (shipped next to the binaries) and points the file target at the
        /// data dir so logs persist wherever the database lives. The config's default
        /// ${basedir}/Logs keeps the file working standalone (e.g. tests).
        /// </summary>
        private static void ConfigureLogging()
        {
            LogManager.Setup().LoadConfigurationFromFile(Paths.ResolveBase("NLog.config"));
            LogManager.Configuration.Variables["datadir"] = Paths.ResolveData("Logs");
        }
    }
}
