using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Data.SqlClient;
using System.Runtime.InteropServices;

using Goose.Events;
using Goose.Quests;
using System.Threading.Tasks;
using GooseServerBrowserService.Client;
using System.Threading;
using Goose.Scripting;
using System.IO;

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

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpPerformanceFrequency);

        public PlayerHandler PlayerHandler { get; set; }
        public EventHandler EventHandler { get; set; }
        public MapHandler MapHandler { get; set; }
        public NPCHandler NPCHandler { get; set; }
        public ClassHandler ClassHandler { get; set; }
        public SqlConnection SqlConnection { get; set; }
        public GameServer GameServer { get; set; }
        public ItemHandler ItemHandler { get; set; }
        public SpellHandler SpellHandler { get; set; }
        public GuildHandler GuildHandler { get; set; }
        public RankHandler RankHandler { get; set; }
        public CombinationHandler CombinationHandler { get; set; }
        public ChatFilter ChatFilter { get; set; }
        public LogHandler LogHandler { get; set; }
        internal QuestHandler QuestHandler { get; set; }
        public ScriptHandler ScriptHandler { get; set; }

        public Dictionary<string, int> CharactersCreatedPerIP { get; set; }

        long timerfreq;
        public long TimerFrequency { get { return this.timerfreq; } }
        Random rng;
        public Random Random
        {
            get { return this.rng; }
        }

        public long TimeNow
        {
            get
            {
                long now;
                GameWorld.QueryPerformanceCounter(out now);
                return now;
            }
        }

        public bool Running { get; set; }

        public decimal ExperienceModifier { get; set; }

        /**
         * Constructor
         * 
         * Constructs all of our Handler objects
         * 
         */
        public GameWorld(GameServer server)
        {
            QueryPerformanceFrequency(out this.timerfreq);
            this.rng = new Random();

            this.GameServer = server;
            this.PlayerHandler = new PlayerHandler();
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
            this.ScriptHandler = new ScriptHandler();

            this.SqlConnection = new SqlConnection("user id=" + GameSettings.Default.DatabaseUsername +
                                       ";password=" + GameSettings.Default.DatabasePassword +
                                       ";server=" + GameSettings.Default.DatabaseAddress +
                                       //";Trusted_Connection=yes;" + // hopefully remove the windows connection
                                       ";database=" + GameSettings.Default.DatabaseName +
                                       ";connection timeout=30" +
                                       ";async=true;MultipleActiveResultSets=True");

            this.ExperienceModifier = GameSettings.Default.ExperienceModifier;

            this.LaunchServerBrowserUpdateThread();
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

            Console.Out.WriteLine("Starting Goose Private Server v" + GameSettings.Default.ServerVersion);
            Console.Out.Write("Connecting to Database ({0}): ", GameSettings.Default.DatabaseAddress);
            try
            {
                this.SqlConnection.Open();
            }
            catch (SqlException e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine("Connected.");

            /**
             * 
             * This is for fixing spell last cast time
             * If the host pc has been restarted players will have a high chance
             * of not being able to cast spells
             * 
             */
            SqlCommand query = new SqlCommand("UPDATE spellbook SET last_casted=0", this.SqlConnection);
            query.ExecuteNonQuery();

            Console.Out.Write("Loading Guilds: ");
            try
            {
                this.GuildHandler.LoadGuilds(this);
                this.GuildHandler.AddSaveEvent(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.GuildHandler.Count.ToString() + " guilds loaded.");

            Console.Out.Write("Loading Spell Effects: ");
            try
            {
                this.SpellHandler.LoadSpellEffects(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.SpellHandler.EffectCount.ToString() + " spell effects loaded.");

            Console.Out.Write("Loading Spells: ");
            try
            {
                this.SpellHandler.LoadSpells(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.SpellHandler.Count.ToString() + " spells loaded.");

            Console.Out.Write("Loading Item Templates: ");
            try
            {
                this.ItemHandler.LoadTemplates(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.ItemHandler.TemplateCount.ToString() + " item templates loaded.");

            Console.Out.Write("Loading Items: ");
            try
            {
                this.ItemHandler.LoadItems(this);
                this.ItemHandler.AddSaveEvent(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.ItemHandler.ItemCount.ToString() + " items loaded.");

            Console.Out.Write("Loading Quests: ");
            try
            {
                this.QuestHandler.LoadQuests(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.QuestHandler.Quests.Count.ToString() + " quests loaded.");

            Console.Out.Write("Loading Maps: ");
            try
            {
                this.MapHandler.LoadMaps(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.MapHandler.Count.ToString() + " maps loaded.");

            Console.Out.Write("Loading Classes: ");
            try
            {
                this.ClassHandler.LoadClasses(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.ClassHandler.Count.ToString() + " classes loaded.");

            Console.Out.Write("Loading Players: ");
            try
            {
                this.PlayerHandler.LoadPlayerData(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.PlayerHandler.PlayerDataCount.ToString() + " players loaded.");

            this.RankHandler.UpdateAll(this);

            Console.Out.Write("Loading NPC Templates: ");
            try
            {
                this.NPCHandler.LoadNPCTemplates(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.NPCHandler.TemplateCount.ToString() + " NPC templates loaded.");

            Console.Out.Write("Loading NPC Spawns: ");
            try
            {
                this.NPCHandler.LoadNPCs(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.NPCHandler.NPCCount.ToString() + " NPCs spawned.");

            Console.Out.Write("Loading Combinations: ");
            try
            {
                this.CombinationHandler.LoadCombinations(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.CombinationHandler.Count.ToString() + " combinations loaded.");

            Console.Out.Write("Loading Chat Filter: ");
            try
            {
                this.ChatFilter.LoadFilter(this);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }
            Console.Out.WriteLine(this.ChatFilter.Count.ToString() + " words loaded.");

            this.CharactersCreatedPerIP = new Dictionary<string, int>();
            Event clearCreatedHistory = new ClearCreatedHistoryEvent();
            clearCreatedHistory.Ticks += this.TimerFrequency * 24 * 60 * 60;
            this.EventHandler.AddEvent(clearCreatedHistory);

            Event updateExperienceModifier = new PlayerCountExperienceModifierUpdateEvent();
            updateExperienceModifier.Ticks += this.TimerFrequency * GameSettings.Default.IdleTimeout;
            this.EventHandler.AddEvent(updateExperienceModifier);

            //Event updateCredits = new CreditsUpdateEvent();
            //updateExperienceModifier.Ticks += this.TimerFrequency * GameSettings.Default.CreditUpdateInterval;
            //this.EventHandler.AddEvent(updateCredits);

            Console.Out.Write("Loading Global Scripts: ");
            try
            {
                LoadGlobalScripts();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Fail, " + e.Message);
                Console.Out.WriteLine("Aborting...");
                return;
            }

            Console.Out.WriteLine("Finished loading game. Ready to join.");

            this.Running = true;
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

            Console.WriteLine("Shutting down server.");
            Console.WriteLine("Clearing maps.");
            foreach (Map map in this.MapHandler.Maps)
            {
                foreach (ItemTile tile in map.Items)
                {
                    if (tile.ItemSlot.Item != this.ItemHandler.GetGold()) tile.ItemSlot.Item.Delete = true;
                }
            }
            Console.WriteLine("Saving items.");
            this.ItemHandler.Save(this);
            Console.WriteLine("Saving players.");
            foreach (Player player in this.PlayerHandler.Players)
            {
                if (player.State > Player.States.LoadingGame)
                {
                    player.SaveToDatabase(this);
                }
            }
            Console.WriteLine("Finished shutting down.");
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
            Console.Out.WriteLine("Connection attempt: " + sock.RemoteEndPoint.ToString());

            try
            {
                sock.Send(Encoding.ASCII.GetBytes("IMN00000000" + "\x1"));
            }
            catch { }
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
            try
            {
                Console.Out.WriteLine("Connection lost: " + sock.RemoteEndPoint.ToString());

                this.GameServer.Disconnect(sock);

                Event ev = new LogoutEvent();
                ev.Data = sock;
                ev.Ticks += (GameSettings.Default.LogoutLagTime * this.TimerFrequency);

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
            Player player = this.PlayerHandler.GetPlayer(sock);
            if (player != null)
            {
                player.Received(data);
                this.ParseData(player);
            }
            else
            {
                //player = new Player(sock);
                //this.EventHandler.AddEvent(player, data.TrimEnd("\x1".ToCharArray()));
                Event ev = new LoginEvent();
                ev.Data = new Object[] { sock, data };
                this.EventHandler.AddEvent(ev);
            }
        }

        /**
         * ParseData, parses data received from player
         * 
         * Splits the data by \x1 which is chr(1) which the client uses to delimit packets
         * Passes each packet along to the EventHandler.AddEvent to see if the EventHandler
         * will add the event.
         * 
         */
        public void ParseData(Player player)
        {
            string data = player.Buffer;
            string[] tokens = data.Split("\x1".ToCharArray());
            int limit = tokens.Length - 1;

            if (!data.EndsWith("\x1"))
            {
                limit--;
            }

            string packet;
            for (int i = 0; i < limit; i++)
            {
                packet = tokens[i];
                data = data.Remove(0, packet.Length + 1);

                this.EventHandler.AddEvent(player, packet);
            }

            player.Buffer = data;
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
            if (player is Pet) return;
            //Console.Out.WriteLine("Send: " + data);

            data += "\x1";
            try
            {
                player.Sock.Send(Encoding.ASCII.GetBytes(data));
            }
            catch (Exception)
            {
                // No crash me pls pls pls
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
            foreach (Player player in this.PlayerHandler.Players)
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
            foreach (Player player in map.Players)
            {
                if (player.State == Player.States.Ready)
                {
                    this.Send(player, data);
                }
            }
        }

        public static void DefaultEndExecuteNonQueryAsyncCallback(IAsyncResult ar)
        {
            try
            {
                SqlCommand command = (SqlCommand)ar.AsyncState;
                command.EndExecuteNonQuery(ar);
            }
            catch (Exception e)
            {
                log.Error(e, "SQL Query Failed");
            }
        }

        public void LaunchServerBrowserUpdateThread()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        new GooseServerBrowserClient("http://goose.ddns.net:3000/").Register(new GooseServerBrowserService.Contract.RegisterRequest()
                        {
                            ServerName = GameSettings.Default.ServerName,
                            PlayerCount = this.PlayerHandler.PlayerCount,
                            Port = GameSettings.Default.GameServerPort,
                            Version = GameSettings.Default.ServerVersion,
                        });
                    }
                    catch
                    {

                    }

                    Thread.Sleep(TimeSpan.FromMinutes(2));
                }
            });
        }

        public void LoadGlobalScripts()
        {
            foreach (var scriptPath in Directory.EnumerateFiles("Scripts/Global", "*.csx"))
            {
                if (this.ScriptHandler.HasScript(scriptPath)) continue;

                var script = this.ScriptHandler.GetScript<IGlobalScript>(scriptPath);
                script.Object.OnLoaded(this);
            }
        }
    }
}
