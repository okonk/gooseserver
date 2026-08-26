using System.Text;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using Goose.Events;
using Goose.Quests;
using Goose.Scripting;
using System.Data.SQLite;
using System.Diagnostics;

namespace Goose
{
    /**
     * GameWorld, this is where all the game-related stuff will go
     *
     * Currently holds the PlayerHandler but eventually will hold
     * - EventHandler
     * - LogHandler
     * - NPCHandler
     * - MapHandler
     * ... etc
     *
     */
    public class GameWorld
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public PlayerHandler PlayerHandler { get; set; }
        public EventHandler EventHandler { get; set; }
        public MapHandler MapHandler { get; set; }
        public NPCHandler NPCHandler { get; set; }
        public ClassHandler ClassHandler { get; set; }
        public GameServer? GameServer { get; set; }
        public ItemHandler ItemHandler { get; set; }
        public SpellHandler SpellHandler { get; set; }
        public GuildHandler GuildHandler { get; set; }
        public RankHandler RankHandler { get; set; }
        public CombinationHandler CombinationHandler { get; set; }
        public ChatFilter ChatFilter { get; set; }
        public LogHandler LogHandler { get; set; }
        public QuestHandler QuestHandler { get; set; }
        public ScriptHandler ScriptHandler { get; set; }
        public CurrencyHandler CurrencyHandler { get; set; }
        public Database Database { get; private set; }
        public GooseSettings Settings { get; }

        public Dictionary<string, int> CharactersCreatedPerIP { get; set; } = null!;

        /**
         * Rate limits failed login attempts by source IP and by account name.
         */
        public LoginThrottle LoginThrottle { get; set; } = null!;

        /**
         * Largest amount of unparsed data held for a single connection before it is
         * dropped. Packets are far smaller than this; the cap exists to stop a client
         * from exhausting memory by never sending a packet delimiter.
         */
        private const int MaxReceiveBufferSize = 64 * 1024;

        // H1: pre-login packets (no Player yet) must be reassembled across TCP
        // segments; the cap bounds a stalled/attacking pre-login socket.
        private const int MaxPreLoginBufferSize = 4096;

        // Illutia login wire format: 2 header bytes + 69 body bytes (LoginEvent.cs)
        private const int MinIllutiaLoginLength = 71;
        private readonly Dictionary<Socket, StringBuilder> preLoginBuffers = new();

        internal string? PreLoginPending(Socket sock)
        {
            return preLoginBuffers.TryGetValue(sock, out StringBuilder? sb) ? sb.ToString() : null;
        }

        long timerfreq;
        public long TimerFrequency { get => this.timerfreq; }
        Random rng;
        public Random Random
        {
            get => this.rng;
        }

        public long TimeNow
        {
            get => Stopwatch.GetTimestamp();
        }

        public bool Running { get; set; }

        public decimal ExperienceModifier { get; set; }

        /**
         * Constructor
         *
         * Constructs all of our Handler objects
         *
         */
        public GameWorld(GooseSettings settings, GameServer? server = null)
        {
            this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.timerfreq = Stopwatch.Frequency;
            this.rng = new Random();

            this.GameServer = server;
            this.PlayerHandler = new PlayerHandler(this.Settings);
            this.EventHandler = new EventHandler();
            this.MapHandler = new MapHandler();
            this.NPCHandler = new NPCHandler();
            this.ClassHandler = new ClassHandler();
            this.ItemHandler = new ItemHandler();
            this.SpellHandler = new SpellHandler();
            this.GuildHandler = new GuildHandler();
            this.RankHandler = new RankHandler();
            this.CombinationHandler = new CombinationHandler();
            this.ChatFilter = new ChatFilter();
            this.LogHandler = new LogHandler();
            this.QuestHandler = new QuestHandler();
            this.ScriptHandler = new ScriptHandler(this.Settings);
            this.CurrencyHandler = new CurrencyHandler();
            // Before LoadGlobalScripts (:355), so scripts can register their own currencies
            // from OnLoaded and resolve against these.
            this.CurrencyHandler.Register(new GoldCurrency());
            this.CurrencyHandler.Register(new CreditsCurrency());
            this.Database = new Database();

            this.ExperienceModifier = this.Settings.ExperienceModifier;
        }

        private void CreateDatabaseSchema()
        {
            this.Database.Execute(conn =>
            {
                foreach (var schemaFile in new[]
                {
                    "items", "maps", "classes", "npcs", "players", "spells", "banks",
                    "quests", "combinations", "logs", "pets", "guilds", "warptiles",
                    "wordfilter", "paypal",
                })
                {
                    ExecuteSql(conn, File.ReadAllText(Paths.ResolveBase("sql/" + schemaFile + ".sql"), Encoding.UTF8));
                }
            });

            if (!string.IsNullOrEmpty(this.Settings.DataLinkId))
            {
                log.Info("Importing data from Google Docs");
                string sql = CsvToSql.Core.CsvToSqlConverter.Convert(this.Settings.DataLinkId);
                File.WriteAllText(Paths.ResolveData("GooseData.sql"), sql);
                this.Database.Execute(conn => ExecuteSql(conn, sql));
            }
        }

        private static void ExecuteSql(SQLiteConnection connection, string sqlFile)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sqlFile;
            command.ExecuteNonQuery();
        }

        /// <summary>Runs on every startup, not just on a fresh database. `players` holds live
        /// data and is never dropped, so new columns on it have to arrive this way.</summary>
        private void MigrateDatabaseSchema()
        {
            this.Database.Execute(conn =>
            {
                AddColumnIfMissing(conn, "players", "player_properties", "TEXT DEFAULT '' NOT NULL");
            });
        }

        internal static bool ColumnExists(SQLiteConnection connection, string table, string column)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA table_info(" + table + ")";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString("name"), column, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        internal static void AddColumnIfMissing(SQLiteConnection connection, string table, string column, string definition)
        {
            if (ColumnExists(connection, table, column)) return;

            using var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE " + table + " ADD COLUMN " + column + " " + definition;
            command.ExecuteNonQuery();
        }

        /**
         * Start, game startup
         *
         * Loads all of the required information for the game
         *
         */
        public void Start()
        {
            this.Running = false;

            log.Info("Starting Goose Private Server v" + this.Settings.ServerVersion);
            var databasePath = Paths.ResolveData(this.Settings.DatabaseName + ".db");
            log.Info("Opening Database ({0}): ", databasePath);

            if (!this.LoadStep("Database", () =>
            {
                bool createNew = !File.Exists(databasePath);
                this.Database.Start(databasePath);
                if (createNew)
                {
                    log.Info("DB file not found, creating...");
                    CreateDatabaseSchema();
                }
                MigrateDatabaseSchema();   // <-- new: runs for fresh and existing databases alike
                log.Info("Connected.");
            })) return;

            if (Environment.GetCommandLineArgs().Contains("updatesql"))
            {
                log.Info("Updating SQL:");
                try
                {
                    var sqlData = CsvToSql.Core.CsvToSqlConverter.Convert(this.Settings.DataLinkId);
                    this.Database.Execute(conn =>
                    {
                        using var command = conn.CreateCommand();
                        command.CommandText = sqlData;
                        command.ExecuteNonQuery();
                    });
                }
                catch (Exception e)
                {
                    log.Error(e, "Failed updating sql");
                    Environment.Exit(1);
                }

                log.Info("Updated");

                // One-shot mode: updatesql only imports data, it does not start the
                // game. Drain queued writes, close the database, flush logs, and exit
                // so the caller (Docker, scripts) can proceed when the command returns.
                this.Database.Stop();
                NLog.LogManager.Shutdown();
                Environment.Exit(0);
            }

            if (!this.LoadStep("Guilds", () =>
            {
                this.GuildHandler.LoadGuilds(this);
                this.GuildHandler.AddSaveEvent(this);
            }, () => this.GuildHandler.Count)) return;

            if (!this.LoadStep("Spell Effects", () => this.SpellHandler.LoadSpellEffects(this),
                () => this.SpellHandler.EffectCount)) return;

            if (!this.LoadStep("Spells", () => this.SpellHandler.LoadSpells(this),
                () => this.SpellHandler.Count)) return;

            if (!this.LoadStep("Item Templates", () => this.ItemHandler.LoadTemplates(this),
                () => this.ItemHandler.TemplateCount)) return;

            if (!this.LoadStep("item titles", () => this.ItemHandler.LoadTitles(this),
                () => this.ItemHandler.TitleCount)) return;
            if (!this.LoadStep("item surnames", () => this.ItemHandler.LoadSurnames(this),
                () => this.ItemHandler.SurnameCount)) return;

            if (!this.LoadStep("Quests", () => this.QuestHandler.LoadQuests(this),
                () => this.QuestHandler.Quests.Count)) return;

            if (!this.LoadStep("Maps", () => this.MapHandler.LoadMaps(this),
                () => this.MapHandler.Count)) return;

            if (!this.LoadStep("Classes", () => this.ClassHandler.LoadClasses(this),
                () => this.ClassHandler.Count)) return;

            if (!this.LoadStep("NPC Templates", () => this.NPCHandler.LoadNPCTemplates(this),
                () => this.NPCHandler.TemplateCount)) return;

            if (!this.LoadStep("NPC Spawns", () => this.NPCHandler.LoadNPCs(this),
                () => this.NPCHandler.NPCCount)) return;

            if (!this.LoadStep("Combinations", () => this.CombinationHandler.LoadCombinations(this),
                () => this.CombinationHandler.Count)) return;

            if (!this.LoadStep("Chat Filter", () => this.ChatFilter.LoadFilter(this),
                () => this.ChatFilter.Count)) return;

            this.CharactersCreatedPerIP = [];
            this.LoginThrottle = new LoginThrottle(this.Settings);
            Event clearCreatedHistory = new ClearCreatedHistoryEvent();
            clearCreatedHistory.Ticks += this.TimerFrequency * 24 * 60 * 60;
            this.EventHandler.AddEvent(clearCreatedHistory);

            Event updateExperienceModifier = new PlayerCountExperienceModifierUpdateEvent();
            // H6: clamp to >= 1, a 0/negative IdleTimeout re-enqueues at now and spins EventHandler.Update
            updateExperienceModifier.Ticks += this.TimerFrequency * Math.Max(1, this.Settings.IdleTimeout);
            this.EventHandler.AddEvent(updateExperienceModifier);

            //Event updateCredits = new CreditsUpdateEvent();
            //this.EventHandler.AddEvent(updateCredits);

            // Add gold item
            var gold = new Item();
            gold.ItemID = this.Settings.ItemIDStartpoint + this.Settings.GoldItemID;
            gold.LoadFromTemplate(ItemHandler.GetTemplate(this.Settings.GoldItemID)!);
            this.ItemHandler.AddItem(gold, this);

            if (!this.LoadStep("Global Scripts", () => LoadGlobalScripts())) return;

            // After global scripts: their OnLoaded can register item templates/currencies
            // that player inventories and banks reference at load time.
            if (!this.LoadStep("Players", () => this.PlayerHandler.LoadPlayerData(this),
                () => this.PlayerHandler.PlayerDataCount)) return;

            this.RankHandler.UpdateAll(this);

            log.Info("Finished loading game. Ready to join.");

            this.Running = true;
        }

        /// <summary>
        /// Runs a loading step, logging the item count on success or aborting on failure.
        /// Returns false if the step threw (the server should not continue).
        /// </summary>
        private bool LoadStep(string name, Action action, Func<int>? countFn = null)
        {
            log.Info("Loading {0}: ", name);
            try
            {
                action();

                string unit = name.ToLower();
                if (countFn is not null)
                    log.Info("{0} {1} loaded.", countFn(), unit);
                else
                    log.Info("Done loading {0}.", name);

                return true;
            }
            catch (Exception e)
            {
                log.Fatal(e, "");
                log.Info("Aborting...");
                return false;
            }
        }

        /**
         * Stop, game shutdown
         *
         * Makes sure all information is saved properly, etc
         *
         */
        public void Stop()
        {
            this.Running = false;

            log.Info("Shutting down server.");

            log.Info("Saving players.");
            foreach (var player in this.PlayerHandler.Players)
            {
                if (player.State > Player.States.LoadingGame)
                {
                    player.SaveToDatabase(this);
                }
            }

            // LogHandler buffers in memory and is otherwise only flushed on a 10 minute
            // cadence, so without this every shutdown discarded up to that much of the
            // audit trail - including the logs used to investigate dupes.
            log.Info("Saving logs.");
            try
            {
                this.LogHandler.Save(this);
            }
            catch (Exception e)
            {
                log.Error(e, "Failed to save buffered logs during shutdown.");
            }

            log.Info("Waiting for database writes.");
            while (this.Database.PendingCount > 0)
            {
                Thread.Sleep(100);
            }
            this.Database.Stop();

            log.Info("Finished shutting down.");
        }

        /**
         * NewConnection, player joined server
         *
         * Creates a new Player object and gives it to the PlayerHandler
         *
         *
         */
        public void NewConnection(Socket sock)
        {
            log.Info("Connection attempt: " + sock.RemoteEndPoint!.ToString());

            if (this.Settings.ServerType == "Illutia")
            {
                try
                {
                    sock.Send(Encoding.ASCII.GetBytes("IMN00000000" + "\x1"));
                }
                catch { }
            }
        }

        /**
         * LostConnection, player left server
         *
         * Removes the player that left
         *
         *
         */
        public void LostConnection(Socket sock)
        {
            preLoginBuffers.Remove(sock);
            try
            {
                log.Info("Connection lost: " + sock.RemoteEndPoint!.ToString());

                this.GameServer!.Disconnect(sock);

                Event ev = new LogoutEvent();
                ev.Data = sock;
                ev.Ticks += (this.Settings.LogoutLagTime * this.TimerFrequency);

                this.EventHandler.AddEvent(ev);
            }
            catch (Exception)
            {
                //eaten
            }
        }

        /**
         * Received, received data from socket
         *
         * First we check if we already have the player, if we do we call the player's Received method
         * Then parse the data
         *
         * If the player is null then we haven't seen them before so create a new Player object
         * then this bit is hackish but it really shouldn't be a problem..
         * We assume the data is the full login packet so add an event to the event handler
         *
         */
        public void Received(Socket sock, string data)
        {
            Player? player = this.PlayerHandler.GetPlayer(sock);
            if (player is not null)
            {
                preLoginBuffers.Remove(sock);
                player.Received(data);

                // The client delimits packets with \x1 and ParseData only trims up to the
                // last one. A client that never sends a delimiter would otherwise grow this
                // buffer without limit.
                if (player.Buffer.Length > MaxReceiveBufferSize)
                {
                    log.Warn("Dropping connection for " + player.Name + ": receive buffer exceeded " +
                             MaxReceiveBufferSize + " bytes with no packet delimiter.");

                    player.Buffer.Clear();
                    this.LostConnection(sock);
                    return;
                }

                this.ParseData(player);
            }
            else
            {
                if (!preLoginBuffers.TryGetValue(sock, out StringBuilder? buffer))
                {
                    buffer = new StringBuilder();
                    preLoginBuffers.Add(sock, buffer);
                }
                buffer.Append(data);

                if (buffer.Length > MaxPreLoginBufferSize)
                {
                    log.Warn("Dropping pre-login connection: buffer exceeded " +
                             MaxPreLoginBufferSize + " bytes.");
                    preLoginBuffers.Remove(sock);
                    this.LostConnection(sock);
                    return;
                }

                string s = buffer.ToString();
                // H1: a classic login is only actionable once name and password are both
                // in (two commas); LoginEvent disconnects on a truncated password.
                int firstComma = s.IndexOf(',');
                bool complete = (s.StartsWith("LOGIN", StringComparison.Ordinal) && firstComma > 0
                                 && s.IndexOf(',', firstComma + 1) >= 0)
                                || s.Length >= MinIllutiaLoginLength;
                if (!complete) return;

                preLoginBuffers.Remove(sock);
                Event ev = new LoginEvent();
                ev.Data = new Object[] { sock, s };
                this.EventHandler.AddEvent(ev);
            }
        }

        /**
         * ParseData, parses data received from player
         *
         * Single pass over the StringBuilder — dispatches each packet as its \x1 delimiter
         * is found. No full buffer-to-string copy. Extracted packets allocate per-packet
         * strings (unavoidable since the event dispatcher expects string arguments).
         * Processed data is removed in-place at the end so only the partial trailing
         * packet remains in the buffer.
         *
         */
        public void ParseData(Player player)
        {
            var buffer = player.Buffer;
            int length = buffer.Length;

            int start = 0;
            int packetsDispatched = 0;

            for (int i = 0; i < length; i++)
            {
                if (buffer[i] != '\x1') continue;

                int packetLength = i - start;
                if (packetLength > 0)
                {
                    this.EventHandler.AddEvent(player, buffer.ToString(start, packetLength));
                }
                else
                {
                    this.EventHandler.AddEvent(player, string.Empty);
                }

                start = i + 1;
                packetsDispatched++;
            }

            // Remove processed data in-place, leaving only the unprocessed tail.
            if (packetsDispatched > 0)
            {
                buffer.Remove(0, start);
            }
        }

        /**
         * Update, update the game world
         *
         * Called every 5ms at least, will probably update the EventHandler, do NPC logic, etc
         *
         */
        public void Update()
        {
            this.EventHandler.Update(this);
        }


        /**
         * Send, sends data to player
         *
         * Adds \x1 to the end which is the packet delimiter
         *
         */
        public void Send(Player player, string data)
        {
            if (player is Pet || data is null) return;
            //Console.Out.WriteLine("Send: " + data);

            data += "\x1";
            try
            {
                if (!player.Send(data))
                {
                    log.Warn("Player {0} send buffer exceeded, dropping connection", player.Name);
                    this.LostConnection(player.Sock);
                }
            }
            catch (Exception)
            {

            }
        }

        /**
         * SendRaw, sends data directly to a socket before a Player object exists.
         *
         * Used during login when the client has connected but no Player has been
         * created yet. Avoids fabricating throwaway Player instances just to route
         * a denial packet.
         *
         */
        public void SendRaw(Socket sock, string data)
        {
            if (sock is null || data is null) return;

            data += "\x1";
            try
            {
                sock.Send(Encoding.ASCII.GetBytes(data));
            }
            catch (Exception)
            {
            }
        }

        /**
         * SendToAll, sends data to all players
         *
         * Sends data to all Players whose state is > Player.States.LoadingGame,
         * because if they are loading the game or not logged in they'll probably crash
         *
         */
        public void SendToAll(string data)
        {
            foreach (var player in this.PlayerHandler.Players)
            {
                if (player.State > Player.States.LoadingGame)
                {
                    this.Send(player, data);
                }
            }
        }

        /**
         * SendToMap, sends data to all players in map
         *
         * Sends data to all Players whose state is Player.States.Ready,
         * because everyone on the map should be ready
         *
         */
        public void SendToMap(Map map, string data)
        {
            foreach (var player in map.Players)
            {
                if (player.State == Player.States.Ready)
                {
                    this.Send(player, data);
                }
            }
        }

        public void LaunchServerBrowserUpdateThread()
        {
        }

        public void LoadGlobalScripts()
        {
            foreach (var scriptPath in Directory.EnumerateFiles(this.Settings.DataPathAbsolute + "/Scripts/Global", "*.csx"))
            {
                if (this.ScriptHandler.HasScript(scriptPath)) continue;

                var script = this.ScriptHandler.GetScript<IGlobalScript>(scriptPath.Substring(this.Settings.DataPathAbsolute.Length + 1));
                script.Object.OnLoaded(this);
            }
        }

        public bool RollChance(double chance)
        {
            return this.Random.Next(1, 1000000001) <= chance * 1000000000;
        }
    }
}
