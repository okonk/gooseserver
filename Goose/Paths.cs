
namespace Goose
{
    /// <summary>
    /// Central path resolution. Nothing in the server reads paths relative to the process
    /// working directory anymore; every path resolves against one of these two roots.
    ///
    /// BaseDir: the directory the server binaries were launched from. Ships the read-only
    ///          data (Data/, sql/, NLog.config, the default GooseSettings.json) and is
    ///          treated as immutable at runtime (in a container this is the image layer).
    ///
    /// DataDir: where mutable state lives: the SQLite database, Logs/, crashlog.txt, and the
    ///          operator's GooseSettings.json. Defaults to BaseDir so a bare `dotnet run`
    ///          behaves exactly as before; point it elsewhere with --datadir &lt;path&gt; or the
    ///          GOOSE_DATADIR environment variable (the argument wins). In a container this
    ///          is the persistent volume.
    /// </summary>
    public static class Paths
    {
        public static string BaseDir { get; private set; }
        public static string DataDir { get; private set; }

        static Paths()
        {
            BaseDir = AppDomain.CurrentDomain.BaseDirectory;
            DataDir = BaseDir;
        }

        /// <summary>
        /// Called once from Program.Main before anything touches GameWorld. With no argument
        /// the GOOSE_DATADIR environment variable is used; with neither, everything stays
        /// next to the binaries (the historical behaviour).
        /// </summary>
        public static void Initialize(string dataDir = null)
        {
            BaseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (string.IsNullOrWhiteSpace(dataDir))
                dataDir = Environment.GetEnvironmentVariable("GOOSE_DATADIR");

            DataDir = string.IsNullOrWhiteSpace(dataDir)
                ? BaseDir
                : Path.GetFullPath(dataDir);
        }

        /// <summary>Resolve a shipped, read-only path (Data/, sql/, ...) against the install dir.</summary>
        public static string ResolveBase(string relativePath) => Path.Combine(BaseDir, relativePath);

        /// <summary>Resolve a mutable path (database, logs, settings) against the data dir.</summary>
        public static string ResolveData(string relativePath) => Path.Combine(DataDir, relativePath);
    }
}
