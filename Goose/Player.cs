using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;

using Goose.Events;
using Goose.Quests;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Data.Common;
using System.Data.SQLite;

namespace Goose
{
    /**
     * Player,
     *
     * Implements the ICharacter interface
     *
     *
     */
    public class Player : ICharacter
    {
        private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        /**
         * Networking stuff
         *
         * Socket and receive buffer
         *
         */
        Socket sock;
        public Socket Sock
        {
            get { return this.sock; }
            set { this.sock = value; }
        }
        public string Buffer { get; set; }

        /**
         * ExperienceMessage, used for sending to AddExperience to diaplay
         * the right message when gaining experience
         *
         */
        public enum ExperienceMessage
        {
            None = 0,
            TooLow,
            TooHigh,
            TooFarAway,
            Normal
        }

        /**
         * Player's state
         *
         */
        public enum States
        {
            NotLoggedIn = 0,
            LoadingGame,
            LoadingMap,
            Ready
        }
        public States State { get; set; }

        /**
         * Player account access status
         *
         */
        public enum AccessStatus
        {
            Deleted = 0,
            Banned,
            Normal,
            Helper = 3,
            EventMaster = 6,
            Guide = 7,
            GameMaster = 9
        }
        public AccessStatus Access { get; set; }

        /**
         * Temporary information used when a player is autocreated
         *
         * AutoCreatedNotSaved - When a player is autocreated they're not saved until the
         * player save event is run
         *
         */
        public bool AutoCreatedNotSaved { get; set; }

        /**
         * Player info
         *
         */
        /**
         * PlayerID is the ID of the character in the database
         */
        public int PlayerID { get; set; }
        /**
         * Character name
         */
        public string Name { get; set; }
        /**
         * Name prefix
         */
        public string Title { get; set; }
        /**
         * Name postfix
         */
        public string Surname { get; set; }
        /**
         * md5 password hash
         */
        public string PasswordHash { get; set; }
        /**
         * NOTE: Salt is stored base64 encoded
         */
        public string PasswordSalt { get; set; }
        /**
         * LoginID is the ID assigned by the server on login
         */
        public int LoginID { get; set; }

        /**
         * Current map id
         */
        public int MapID { get; set; }
        /**
         * Current map x
         */
        public int MapX { get; set; }
        /**
         * Current map y
         */
        public int MapY { get; set; }
        /**
         * Current Map object
         */
        public Map Map { get; set; }
        /**
         * Facing direction
         */
        public int Facing { get; set; }
        /**
         * BaseStats, stats loaded from database
         */
        public AttributeSet BaseStats { get; set; }
        /**
         * Stats from base, items, buffs
         */
        public AttributeSet MaxStats { get; set; }
        /**
         * Current HP
         */
        long currentHP;
        public long CurrentHP
        {
            get { return this.currentHP; }
            set
            {
                this.currentHP = Math.Min(value, this.MaxHP);
            }
        }
        /**
         * Current MP
         */
        long currentMP;
        public long CurrentMP
        {
            get { return this.currentMP; }
            set
            {
                currentMP = Math.Min(value, this.MaxMP);
            }
        }
        /**
          * Current SP
          */
        long currentSP;
        public long CurrentSP
        {
            get { return this.currentSP; }
            set
            {
                this.currentSP = Math.Min(value, this.MaxSP);
            }
        }

        public long MaxHP
        {
            get
            {
                return this.TemporaryMaxHP ?? this.MaxStats.HP;
            }
        }

        public long MaxMP
        {
            get
            {
                return this.TemporaryMaxMP ?? this.MaxStats.MP;
            }
        }

        public long MaxSP
        {
            get
            {
                return this.TemporaryMaxSP ?? this.MaxStats.SP;
            }
        }

        public long? TemporaryMaxHP { get; set; }

        public long? TemporaryMaxMP { get; set; }
        public long? TemporaryMaxSP { get; set; }

        /**
         * Bound/respawn map id
         */
        public int BoundID { get; set; }
        /**
         * Bound/respawn map x
         */
        public int BoundX { get; set; }
        /**
         * Bound/respawn map y
         */
        public int BoundY { get; set; }
        /**
         * Bound Map
         */
        public Map BoundMap { get; set; }
        /**
         * Hair style id
         */
        public int HairID { get; set; }
        /**
         * Hair colour r
         */
        public int HairR { get; set; }
        /**
         * Hair colour g
         */
        public int HairG { get; set; }
        /**
         * Hair colour b
         */
        public int HairB { get; set; }
        /**
         * Hair colour a
         */
        public int HairA { get; set; }
        /**
         * Body colour r
         */
        public int BodyR { get; set; }
        /**
         * Body colour g
         */
        public int BodyG { get; set; }
        /**
         * Body colour b
         */
        public int BodyB { get; set; }
        /**
         * Body colour a
         */
        public int BodyA { get; set; }
        /**
         * Face id
         */
        public int FaceID { get; set; }
        /**
         * Body ID
         */
        public int BodyID { get; set; }
        /**
         * Current Body ID
         */
        public int CurrentBodyID { get; set; }
        /**
         * Body state/pose
         */
        public int BodyState { get; set; }
        /**
         * Gold
         */
        public long Gold { get; set; }
        /**
         * Experience
         */
        public long Experience { get; set; }
        /**
         * Experience sold
         */
        public long ExperienceSold { get; set; }
        /**
         * Level
         */
        public int Level { get; set; }
        /**
         * Class ID
         */
        public int ClassID { get; set; }
        /**
         * Guild ID
         */
        public int GuildID { get; set; }
        /**
         * Guild object
         */
        public Guild Guild { get; set; }

        /**
         * Class object
         */
        public Class Class { get; set; }

        /**
         * So regen event doesn't double up
         */
        public bool RegenEventExists { get; set; }

        /**
         * Player's inventory
         *
         */
        public Inventory Inventory { get; set; }

        public PlayerBank Bank { get; set; }

        /**
         * Time of last melee attack
         *
         */
        public long LastAttack { get; set; }

        /**
         * For ping timeout
         *
         */
        public long LastPing { get; set; }

        /**
         * Holds players spells
         *
         */
        public Spellbook Spellbook { get; set; }

        /**
         * Buffs, holds players buffs
         *
         */
        public List<Buff> Buffs { get; set; }

        /**
         * The group the player is in
         *
         * If none is null.
         *
         */
        public Group Group { get; set; }
        public bool GroupInvitesEnabled { get; set; }

        public int LastWindowID { get; set; }
        public List<Window> Windows { get; set; }

        public long MovementRecordingStarted { get; set; }
        public long MovementRecordingSteps { get; set; }

        public int NumberOfBankPages { get; set; }

        public decimal AdditionalExperienceModifier { get; set; }

        public bool SPRegenSwitch { get; set; }

        private PriorityQueue<int, int> moveSpeed { get; set; }

        /**
         * Bitfield for toggled settings
         *
         *
         */
        public enum ToggleSetting
        {
            Experience = 1,
            Tell = 2,
            WordFilter = 4,
            QuestCredit = 8,
            GMInvisible = 16, // GM only
            GM = 32,
            ItemBuffs = 64,
            WhoInvisible = 128,
        }

        public ToggleSetting ToggleSettings { get; set; }

        public bool ChatFilterEnabled { get { return ((this.ToggleSettings & Player.ToggleSetting.WordFilter) == 0); } }
        public bool QuestCreditFilterEnabled { get { return ((this.ToggleSettings & Player.ToggleSetting.QuestCredit) != 0); } }
        public bool IsGMInvisible { get { return (this.HasPrivilege(AccessPrivilege.GMInvisible) && ((this.ToggleSettings & Player.ToggleSetting.GMInvisible) == 0)); } }
        public bool IsWhoInvisible { get { return (this.HasPrivilege(AccessPrivilege.WhoInvisible) && ((this.ToggleSettings & Player.ToggleSetting.WhoInvisible) == 0)); } }
        public bool IsGM { get { return (this.Access == AccessStatus.GameMaster && ((this.ToggleSettings & Player.ToggleSetting.GM) == 0)); } }
        public bool ShowItemBuffs { get { return ((this.ToggleSettings & Player.ToggleSetting.ItemBuffs) == 0); } }

        public decimal AetherThreshold { get; set; }

        /// <summary>
        /// Holds all of the Player's pets
        /// </summary>
        public List<Pet> Pets { get; set; }

        /// <summary>
        /// The last time we received a movement, chat, spell, or spacebar attack
        /// </summary>
        public long LastActive { get; set; }
        bool isIdle;

        /// <summary>
        /// Amount of Donation credits
        /// </summary>
        public int Credits { get; set; }

        /// <summary>
        /// Total time spent active
        /// </summary>
        public long TotalPlayTime { get; set; }
        /// <summary>
        /// Total time spent afk
        /// </summary>
        public long TotalAfkTime { get; set; }
        /// <summary>
        /// The last time we updated the playtime
        /// </summary>
        public long LastPlaytimeUpdate { get; set; }

        public long SuspectedMacroFirstTime { get; set; }
        public int SuspectedMacroCount { get; set; }

        internal List<Quest> QuestsCompleted { get; set; }
        internal List<Quest> QuestsStarted { get; set; }
        internal List<QuestProgress> QuestProgress { get; set; }

        public int MacroCheckFailures { get; set; }

        public long LastMacroCheckTime { get; set; }

        public MacroCheckEvent MacroCheckEvent { get; set; }

        public DateTime? UnbanDate { get; set; }

        private object socketLock = new object();


        /**
         * Constructor
         *
         *
         */
        public Player(int unused)
        {
            this.Buffer = "";

            this.LastAttack = 0;
            this.LastPing = 0;
            this.LastWindowID = 1000;
            this.Windows = new List<Window>();

            this.State = States.NotLoggedIn;

            this.Buffs = new List<Buff>();
            this.Pets = new List<Pet>();

            this.QuestProgress = new List<QuestProgress>();
            this.QuestsCompleted = new List<Quest>();
            this.QuestsStarted = new List<Quest>();

            this.GroupInvitesEnabled = false;

            this.MovementRecordingSteps = 0;

            this.moveSpeed = new PriorityQueue<int, int>();
        }

        public Player()
        {

        }

        public bool HasPrivilege(AccessPrivilege privilege)
        {
            return AccessLevels.HasPrivilege(this, privilege);
        }

        /**
         * Received, received data from player
         *
         * Adds the data to the receive buffer
         *
         */
        public void Received(string data)
        {
            this.Buffer += data;
        }

        /**
         * LoadFromAutoCreate, fills in player info from server defaults
         *
         */
        public void LoadFromAutoCreate(string name, string password, GameWorld world)
        {
            byte[] saltBytes = new byte[16];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(saltBytes);

            string salt = Encoding.ASCII.GetString(saltBytes);
            string base64Salt = Convert.ToBase64String(saltBytes);

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] data = Encoding.ASCII.GetBytes(salt + password + GameWorld.Settings.ServerName);
            data = md5.ComputeHash(data);

            string passwordHash = BitConverter.ToString(data).Replace("-", "").ToLower();

            this.AutoCreatedNotSaved = true;
            this.PlayerID = world.PlayerHandler.CurrentID;
            world.PlayerHandler.CurrentID++;
            this.Name = name;
            this.Title = GameWorld.Settings.StartingTitle;
            this.Surname = GameWorld.Settings.StartingSurname;
            this.PasswordHash = passwordHash;
            this.PasswordSalt = base64Salt;
            this.Access = AccessStatus.Normal;
            this.MapID = GameWorld.Settings.StartingMapID;
            this.MapX = GameWorld.Settings.StartingMapX;
            this.MapY = GameWorld.Settings.StartingMapY;

            this.Facing = 2;
            this.BoundID = GameWorld.Settings.StartingMapID;
            this.BoundX = GameWorld.Settings.StartingMapX;
            this.BoundY = GameWorld.Settings.StartingMapY;
            this.BoundMap = world.MapHandler.GetMap(this.BoundID);
            this.Gold = GameWorld.Settings.StartingGold;
            this.Level = GameWorld.Settings.StartingLevel;
            this.ClassID = GameWorld.Settings.StartingClassID;
            this.GuildID = GameWorld.Settings.StartingGuildID;
            this.Guild = world.GuildHandler.GetGuild(this.GuildID);
            this.Experience = GameWorld.Settings.StartingExperience;
            this.ExperienceSold = GameWorld.Settings.StartingExperienceSold;
            this.BodyID = GameWorld.Settings.StartingBodyID;
            this.BodyR = GameWorld.Settings.StartingBodyR;
            this.BodyG = GameWorld.Settings.StartingBodyG;
            this.BodyB = GameWorld.Settings.StartingBodyB;
            this.BodyA = GameWorld.Settings.StartingBodyA;
            this.CurrentBodyID = this.BodyID;
            this.FaceID = GameWorld.Settings.StartingFaceID;
            this.HairID = GameWorld.Settings.StartingHairID;
            this.HairR = GameWorld.Settings.StartingHairR;
            this.HairG = GameWorld.Settings.StartingHairG;
            this.HairB = GameWorld.Settings.StartingHairB;
            this.HairA = GameWorld.Settings.StartingHairA;

            this.BaseStats = new AttributeSet();
            this.BaseStats.HP = GameWorld.Settings.StartingHP;
            this.BaseStats.MP = GameWorld.Settings.StartingMP;
            this.BaseStats.SP = GameWorld.Settings.StartingSP;
            this.BaseStats.AC = GameWorld.Settings.StartingAC;
            this.BaseStats.Strength = GameWorld.Settings.StartingStrength;
            this.BaseStats.Stamina = GameWorld.Settings.StartingStamina;
            this.BaseStats.Intelligence = GameWorld.Settings.StartingIntelligence;
            this.BaseStats.Dexterity = GameWorld.Settings.StartingDexterity;
            this.BaseStats.FireResist = GameWorld.Settings.StartingFireResist;
            this.BaseStats.AirResist = GameWorld.Settings.StartingAirResist;
            this.BaseStats.EarthResist = GameWorld.Settings.StartingEarthResist;
            this.BaseStats.SpiritResist = GameWorld.Settings.StartingSpiritResist;
            this.BaseStats.WaterResist = GameWorld.Settings.StartingWaterResist;
            this.BaseStats.MoveSpeed = GameWorld.Settings.StartingMoveSpeed;

            this.MaxStats = new AttributeSet();
            this.MaxStats += this.BaseStats;
            this.MaxStats.Haste = GameWorld.Settings.BaseHaste;
            this.MaxStats.SpellDamage = GameWorld.Settings.BaseSpellDamage;
            this.MaxStats.SpellCrit = GameWorld.Settings.BaseSpellCrit;
            this.MaxStats.MeleeDamage = GameWorld.Settings.BaseMeleeDamage;
            this.MaxStats.MeleeCrit = GameWorld.Settings.BaseMeleeCrit;
            this.MaxStats.DamageReduction = GameWorld.Settings.BaseDamageReduction;
            this.MaxStats.HPPercentRegen = GameWorld.Settings.BaseHPPercentRegen;
            this.MaxStats.HPStaticRegen = GameWorld.Settings.BaseHPStaticRegen;
            this.MaxStats.MPPercentRegen = GameWorld.Settings.BaseMPPercentRegen;
            this.MaxStats.MPStaticRegen = GameWorld.Settings.BaseMPStaticRegen;
            this.MaxStats.SPPercentRegen = GameWorld.Settings.BaseSPPercentRegen;
            this.MaxStats.SPStaticRegen = GameWorld.Settings.BaseSPStaticRegen;

            this.Class = world.ClassHandler.GetClass(this.ClassID);
            this.MaxStats += this.Class.GetLevel(this.Level).BaseStats;

            this.BodyState = GameWorld.Settings.StartingBodyState;

            this.ToggleSettings = (ToggleSetting)GameWorld.Settings.DefaultToggleSettings;
            this.AetherThreshold = GameWorld.Settings.DefaultAetherThreshold;

            this.NumberOfBankPages = GameWorld.Settings.StartingBankPages;
            this.Credits = 0;
            this.TotalAfkTime = 0;
            this.TotalPlayTime = 0;

            this.LastActive = world.TimeNow;
            this.LastPlaytimeUpdate = world.TimeNow;

            this.Inventory = new Inventory(this);
            string[] items = GameWorld.Settings.StartingItems.Split(" ".ToCharArray());
            if (items.Length > 0)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    try
                    {
                        int templateid = Convert.ToInt32(items[i]);
                        ItemTemplate template = world.ItemHandler.GetTemplate(templateid);
                        if (template == null)
                        {
                            // log bad id in starting items
                            continue;
                        }
                        Item item = new Item();
                        item.LoadFromTemplate(template);
                        world.ItemHandler.AddAndAssignId(item, world);

                        if (!this.Inventory.AddItem(item, 1, world))
                        {
                            // log not enough inventory space for starting items
                        }
                    }
                    catch (Exception)
                    {
                        // eaten
                        // log bad id in starting items
                    }
                }
            }
            this.Spellbook = new Spellbook(this);
            this.Bank = new PlayerBank();

            // kind of a hack to ensure the queue should never be empty
            this.moveSpeed.Enqueue(this.BaseStats.MoveSpeed, this.BaseStats.MoveSpeed);
            this.moveSpeed.Enqueue(this.BaseStats.MoveSpeed, this.BaseStats.MoveSpeed);
        }

        /**
         * LoadFromReader, loads player info from a Sq1DataReader
         *
         */
        public void LoadFromReader(GameWorld world, DbDataReader reader)
        {
            this.Access = (AccessStatus)Convert.ToInt32(reader["access_status"]);

            string databaseHash = Convert.ToString(reader["password_hash"]);
            string base64Salt = Convert.ToString(reader["password_salt"]);

            this.AutoCreatedNotSaved = false;
            this.PlayerID = Convert.ToInt32(reader["player_id"]);
            this.Name = Convert.ToString(reader["player_name"]);
            this.Title = Convert.ToString(reader["player_title"]);
            this.Surname = Convert.ToString(reader["player_surname"]);
            this.PasswordHash = databaseHash;
            this.PasswordSalt = base64Salt;
            this.MapID = Convert.ToInt32(reader["map_id"]);
            this.MapX = Convert.ToInt32(reader["map_x"]);
            this.MapY = Convert.ToInt32(reader["map_y"]);
            this.Facing = Convert.ToInt32(reader["player_facing"]);
            this.BoundID = Convert.ToInt32(reader["bound_id"]);
            this.BoundX = Convert.ToInt32(reader["bound_x"]);
            this.BoundY = Convert.ToInt32(reader["bound_y"]);
            this.BoundMap = world.MapHandler.GetMap(this.BoundID);
            this.Gold = Convert.ToInt64(reader["player_gold"]);
            this.Level = Convert.ToInt32(reader["player_level"]);
            this.ClassID = Convert.ToInt32(reader["class_id"]);
            this.GuildID = Convert.ToInt32(reader["guild_id"]);
            this.Guild = world.GuildHandler.GetGuild(this.GuildID);
            this.Experience = Convert.ToInt64(reader["experience"]);
            this.ExperienceSold = Convert.ToInt64(reader["experience_sold"]);
            this.BodyID = Convert.ToInt32(reader["body_id"]);
            this.BodyR = Convert.ToInt32(reader["body_r"]);
            this.BodyG = Convert.ToInt32(reader["body_g"]);
            this.BodyB = Convert.ToInt32(reader["body_b"]);
            this.BodyA = Convert.ToInt32(reader["body_a"]);
            this.CurrentBodyID = this.BodyID;
            this.FaceID = Convert.ToInt32(reader["face_id"]);
            this.HairID = Convert.ToInt32(reader["hair_id"]);
            this.HairR = Convert.ToInt32(reader["hair_r"]);
            this.HairG = Convert.ToInt32(reader["hair_g"]);
            this.HairB = Convert.ToInt32(reader["hair_b"]);
            this.HairA = Convert.ToInt32(reader["hair_a"]);

            this.BaseStats = new AttributeSet();
            this.BaseStats.HP = Convert.ToInt32(reader["player_hp"]);
            this.BaseStats.MP = Convert.ToInt32(reader["player_mp"]);
            this.BaseStats.SP = Convert.ToInt32(reader["player_sp"]);
            this.BaseStats.AC = Convert.ToInt32(reader["stat_ac"]);
            this.BaseStats.Strength = Convert.ToInt32(reader["stat_str"]);
            this.BaseStats.Stamina = Convert.ToInt32(reader["stat_sta"]);
            this.BaseStats.Intelligence = Convert.ToInt32(reader["stat_int"]);
            this.BaseStats.Dexterity = Convert.ToInt32(reader["stat_dex"]);
            this.BaseStats.FireResist = Convert.ToInt32(reader["res_fire"]);
            this.BaseStats.AirResist = Convert.ToInt32(reader["res_air"]);
            this.BaseStats.EarthResist = Convert.ToInt32(reader["res_earth"]);
            this.BaseStats.SpiritResist = Convert.ToInt32(reader["res_spirit"]);
            this.BaseStats.WaterResist = Convert.ToInt32(reader["res_water"]);
            this.BaseStats.MoveSpeed = Convert.ToInt32(reader["move_speed"]);

            this.MaxStats = new AttributeSet();
            this.MaxStats += this.BaseStats;
            this.MaxStats.Haste = GameWorld.Settings.BaseHaste;
            this.MaxStats.SpellDamage = GameWorld.Settings.BaseSpellDamage;
            this.MaxStats.SpellCrit = GameWorld.Settings.BaseSpellCrit;
            this.MaxStats.MeleeDamage = GameWorld.Settings.BaseMeleeDamage;
            this.MaxStats.MeleeCrit = GameWorld.Settings.BaseMeleeCrit;
            this.MaxStats.DamageReduction = GameWorld.Settings.BaseDamageReduction;
            this.MaxStats.HPPercentRegen = GameWorld.Settings.BaseHPPercentRegen;
            this.MaxStats.HPStaticRegen = GameWorld.Settings.BaseHPStaticRegen;
            this.MaxStats.MPPercentRegen = GameWorld.Settings.BaseMPPercentRegen;
            this.MaxStats.MPStaticRegen = GameWorld.Settings.BaseMPStaticRegen;
            this.MaxStats.SPPercentRegen = GameWorld.Settings.BaseSPPercentRegen;
            this.MaxStats.SPStaticRegen = GameWorld.Settings.BaseSPStaticRegen;

            this.Class = world.ClassHandler.GetClass(this.ClassID);
            this.MaxStats += this.Class.GetLevel(this.Level).BaseStats;

            this.ToggleSettings = (ToggleSetting)Convert.ToInt64(reader["toggle_settings"]);
            this.AetherThreshold = Convert.ToDecimal(reader["aether_threshold"]);

            this.NumberOfBankPages = Convert.ToInt32(reader["bank_pages"]);
            this.Credits = Convert.ToInt32(reader["donation_credits"]);
            this.TotalPlayTime = Convert.ToInt64(reader["total_playtime"]);
            this.TotalAfkTime = Convert.ToInt64(reader["total_afktime"]);

            var unbanDate = reader["unban_date"];
            this.UnbanDate = (unbanDate == DBNull.Value ? null : Convert.ToDateTime(unbanDate));

            this.MacroCheckFailures = Convert.ToInt32(reader["macrocheck_failures"]);

            this.LastActive = world.TimeNow;
            this.LastPlaytimeUpdate = world.TimeNow;

            // kind of a hack to ensure the queue should never be empty
            this.moveSpeed.Enqueue(this.BaseStats.MoveSpeed, this.BaseStats.MoveSpeed);
            this.moveSpeed.Enqueue(this.BaseStats.MoveSpeed, this.BaseStats.MoveSpeed);
        }


        public void LoadAdditional(GameWorld world)
        {
            this.Inventory = new Inventory(this);
            this.Inventory.Load(world);
            this.Spellbook = new Spellbook(this);
            this.Spellbook.Load(world);
            this.Bank = new PlayerBank();
            this.Bank.Load(world, this);

            this.BodyState = GameWorld.Settings.StartingBodyState;

            this.LoadPets(world);
            this.LoadQuests(world);
        }

        /// <summary>
        /// Loads all pets from database
        /// </summary>
        /// <param name="world"></param>
        public void LoadPets(GameWorld world)
        {
            var query = world.SqlConnection.CreateCommand();
            query.CommandText = "SELECT * FROM pets WHERE owner_id=" + this.PlayerID;
            var reader = query.ExecuteReader();

            while (reader.Read())
            {
                this.AddPet(Pet.FromReader(reader, world));
            }

            reader.Close();
        }

        public void LoadQuests(GameWorld world)
        {
            using (var query = world.SqlConnection.CreateCommand())
            {
                query.CommandText = "SELECT serialized_data FROM quest_status WHERE player_id=" + this.PlayerID;
                string serialized_data = Convert.ToString(query.ExecuteScalar());
                var questStatus = JsonConvert.DeserializeObject<QuestStatus>(serialized_data, GameWorld.JsonSerializerSettings);

                foreach (var started in questStatus.Started)
                {
                    var quest = world.QuestHandler.Get(started);
                    if (quest != null)
                        this.QuestsStarted.Add(quest);
                }

                foreach (var completed in questStatus.Completed)
                {
                    var quest = world.QuestHandler.Get(completed);
                    if (quest != null)
                        this.QuestsCompleted.Add(quest);
                }

                foreach (var progress in questStatus.Progress)
                {
                    var quest = world.QuestHandler.Get(progress.QuestId);
                    if (quest == null)
                        continue;

                    var requirement = quest.Requirements.FirstOrDefault(r => r.Id == progress.RequirementId);
                    if (requirement == null)
                        continue;

                    this.QuestProgress.Add(new QuestProgress { Requirement = requirement, Value = progress.Progress });
                }
            }
        }

        /**
         * SaveToDatabase, saves player info to database
         *
         */
        public virtual void SaveToDatabase(GameWorld world)
        {
            var playerNameParam = new SQLiteParameter("@playerName", DbType.String) { Value = this.Name };
            var playerTitleParam = new SQLiteParameter("@playerTitle", DbType.String) { Value = this.Title };
            var playerSurnameParam = new SQLiteParameter("@playerSurname", DbType.String) { Value = this.Surname };

            var unbanDateParam = new SQLiteParameter("@unbanDate", DbType.DateTime2);
            if (this.UnbanDate.HasValue)
                unbanDateParam.Value = this.UnbanDate.Value;
            else
                unbanDateParam.Value = DBNull.Value;
            unbanDateParam.IsNullable = true;

            if (this.GuildID == 0 && this.Guild != null) this.Guild.Save(world);

            if (this.AutoCreatedNotSaved)
            {
                string query = "INSERT INTO players (player_id, player_name, player_title, player_surname, " +
                    "password_hash, password_salt, access_status, map_id, map_x, map_y, player_facing, " +
                    "bound_id, bound_x, bound_y, player_gold, player_level, experience, experience_sold, " +
                    "player_hp, player_mp, player_sp, class_id, guild_id, stat_ac, stat_str, stat_sta, " +
                    "stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, body_id, body_r, body_g, body_b, body_a, " +
                    "face_id, hair_id, hair_r, hair_g, hair_b, hair_a, aether_threshold, toggle_settings, " +
                    "donation_credits, total_playtime, total_afktime, move_speed, bank_pages, unban_date, macrocheck_failures) VALUES" +
                    "(" +
                    this.PlayerID + "," +
                    " @playerName, @playerTitle, @playerSurname, " +
                    "'" + this.PasswordHash + "', " +
                    "'" + this.PasswordSalt + "', " +
                    (int)this.Access + ", " +
                    this.MapID + ", " +
                    this.MapX + ", " +
                    this.MapY + ", " +
                    this.Facing + ", " +
                    this.BoundID + ", " +
                    this.BoundX + ", " +
                    this.BoundY + ", " +
                    this.Gold + ", " +
                    this.Level + ", " +
                    this.Experience + ", " +
                    this.ExperienceSold + ", " +
                    this.BaseStats.HP + ", " +
                    this.BaseStats.MP + ", " +
                    this.BaseStats.SP + ", " +
                    this.ClassID + ", " +
                    this.GuildID + ", " +
                    this.BaseStats.AC + ", " +
                    this.BaseStats.Strength + ", " +
                    this.BaseStats.Stamina + ", " +
                    this.BaseStats.Dexterity + ", " +
                    this.BaseStats.Intelligence + ", " +
                    this.BaseStats.FireResist + ", " +
                    this.BaseStats.WaterResist + ", " +
                    this.BaseStats.SpiritResist + ", " +
                    this.BaseStats.AirResist + ", " +
                    this.BaseStats.EarthResist + ", " +
                    this.BodyID + ", " +
                    this.BodyR + ", " +
                    this.BodyG + ", " +
                    this.BodyB + ", " +
                    this.BodyA + ", " +
                    this.FaceID + ", " +
                    this.HairID + ", " +
                    this.HairR + ", " +
                    this.HairG + ", " +
                    this.HairB + ", " +
                    this.HairA + ", " +
                    this.AetherThreshold + ", " +
                    (long)this.ToggleSettings + ", " +
                    this.Credits + ", " +
                    this.TotalPlayTime + ", " +
                    this.TotalAfkTime + ", " +
                    this.BaseStats.MoveSpeed + ", " +
                    this.NumberOfBankPages + ", " +
                    "@unbanDate, " +
                    this.MacroCheckFailures +
                    ")";

                this.AutoCreatedNotSaved = false;

                var command = world.SqlConnection.CreateCommand();
                command.CommandText = query;
                command.Parameters.Add(playerNameParam);
                command.Parameters.Add(playerTitleParam);
                command.Parameters.Add(playerSurnameParam);
                command.Parameters.Add(unbanDateParam);

                world.DatabaseWriter.Add(command);
            }
            else
            {
                string query = "UPDATE players SET " +
                    "player_name=@playerName, " +
                    "player_title=@playerTitle, " +
                    "player_surname=@playerSurname, " +
                    "password_hash='" + this.PasswordHash + "', " +
                    "password_salt='" + this.PasswordSalt + "', " +
                    "access_status=" + (int)this.Access + ", " +
                    "map_id=" + this.MapID + ", " +
                    "map_x=" + this.MapX + ", " +
                    "map_y=" + this.MapY + ", " +
                    "player_facing=" + this.Facing + ", " +
                    "bound_id=" + this.BoundID + ", " +
                    "bound_x=" + this.BoundX + ", " +
                    "bound_y=" + this.BoundY + ", " +
                    "player_gold=" + this.Gold + ", " +
                    "player_level=" + this.Level + ", " +
                    "experience=" + this.Experience + ", " +
                    "experience_sold=" + this.ExperienceSold + ", " +
                    "player_hp=" + this.BaseStats.HP + ", " +
                    "player_mp=" + this.BaseStats.MP + ", " +
                    "player_sp=" + this.BaseStats.SP + ", " +
                    "class_id=" + this.ClassID + ", " +
                    "guild_id=" + this.GuildID + ", " +
                    "stat_ac=" + this.BaseStats.AC + ", " +
                    "stat_str=" + this.BaseStats.Strength + ", " +
                    "stat_sta=" + this.BaseStats.Stamina + ", " +
                    "stat_dex=" + this.BaseStats.Dexterity + ", " +
                    "stat_int=" + this.BaseStats.Intelligence + ", " +
                    "res_fire=" + this.BaseStats.FireResist + ", " +
                    "res_water=" + this.BaseStats.WaterResist + ", " +
                    "res_spirit=" + this.BaseStats.SpiritResist + ", " +
                    "res_air=" + this.BaseStats.AirResist + ", " +
                    "res_earth=" + this.BaseStats.EarthResist + ", " +
                    "body_id=" + this.BodyID + ", " +
                    "body_r=" + this.BodyR + ", " +
                    "body_g=" + this.BodyG + ", " +
                    "body_b=" + this.BodyB + ", " +
                    "body_a=" + this.BodyA + ", " +
                    "face_id=" + this.FaceID + ", " +
                    "hair_id=" + this.HairID + ", " +
                    "hair_r=" + this.HairR + ", " +
                    "hair_g=" + this.HairG + ", " +
                    "hair_b=" + this.HairB + ", " +
                    "hair_a=" + this.HairA + ", " +
                    "aether_threshold=" + this.AetherThreshold + ", " +
                    "toggle_settings=" + (long)this.ToggleSettings + ", " +
                    "donation_credits=" + this.Credits + ", " +
                    "total_playtime=" + this.TotalPlayTime + ", " +
                    "total_afktime=" + this.TotalAfkTime + ", " +
                    "move_speed=" + this.BaseStats.MoveSpeed + ", " +
                    "bank_pages=" + this.NumberOfBankPages + ", " +
                    "unban_date=@unbanDate, " +
                    "macrocheck_failures=" + this.MacroCheckFailures + " " +
                    "WHERE player_id=" + this.PlayerID;

                var command = world.SqlConnection.CreateCommand();
                command.CommandText = query;
                command.Parameters.Add(playerNameParam);
                command.Parameters.Add(playerTitleParam);
                command.Parameters.Add(playerSurnameParam);
                command.Parameters.Add(unbanDateParam);

                world.DatabaseWriter.Add(command);
            }

            this.Inventory.Save(world);
            this.Spellbook.Save(world);
            this.Bank.Save(world, this);

            foreach (Pet pet in this.Pets)
            {
                pet.SaveToDatabase(world);
            }

            this.SaveQuests(world);
        }

        /// <summary>
        /// Player, or Player's Group or Pet killed the given npc
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="world"></param>
        internal void Killed(NPC npc, GameWorld world)
        {
            this.UpdatePossibleQuestProgress(RequirementType.Kill, npc.NPCTemplate.NPCTemplateID, world);
        }

        internal void TalkedTo(NPC npc, GameWorld world)
        {
            this.UpdatePossibleQuestProgress(RequirementType.TalkToNPC, npc.NPCTemplate.NPCTemplateID, world);
        }

        private void UpdatePossibleQuestProgress(RequirementType requirementType, long requirementValue, GameWorld world)
        {
            foreach (var progress in this.QuestProgress)
            {
                if (progress.Requirement.Type == requirementType && progress.Requirement.Value == requirementValue)
                {
                    progress.Value++;
                    if (!QuestCreditFilterEnabled)
                    {
                        world.Send(this, P.BattleTextYellow(this, "Quest Credit: " + progress.Requirement.Quest.Name));
                    }
                }
            }
        }

        private void SaveQuests(GameWorld world)
        {
            var questStatus = new QuestStatus();
            questStatus.Completed = this.QuestsCompleted.Select(q => q.Id).ToArray();
            questStatus.Started = this.QuestsStarted.Select(q => q.Id).ToArray();
            questStatus.Progress = this.QuestProgress.Select(q => new QuestStatus.QuestProgress(q.Requirement.Quest.Id, q.Requirement.Id, q.Value)).ToArray();

            var saveQuestStatusCommand = world.SqlConnection.CreateCommand();
            saveQuestStatusCommand.CommandText =
                @"INSERT INTO quest_status (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                  ON CONFLICT(player_id) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
            saveQuestStatusCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = this.PlayerID });
            saveQuestStatusCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = JsonConvert.SerializeObject(questStatus, GameWorld.JsonSerializerSettings) });
            world.DatabaseWriter.Add(saveQuestStatusCommand);
        }

        /**
         * CanMoveTo, checks if player can move to the specified x,y
         *
         */
        public bool CanMoveTo(int x, int y)
        {
            if (this.Map.CanMoveTo(this, x, y))
            {
                return true;
            }

            return false;
        }

        /**
         * MoveTo, moves player to x, y
         *
         */
        public virtual void MoveTo(GameWorld world, int x, int y)
        {
            List<Player> beforeRange = this.Map.GetPlayersInRange(this);
            List<NPC> beforeNPCRange = this.Map.GetNPCsInRange(this);

            // move off this square so null
            if (!IsGMInvisible)
                this.Map.SetCharacter(null, this.MapX, this.MapY);
            this.MapX = x;
            this.MapY = y;
            // move onto this square so this
            if (!IsGMInvisible)
                this.Map.SetCharacter(this, this.MapX, this.MapY);

            try
            {
                this.Map.Script?.Object.OnPlayerMove(this.Map, this, world);
            }
            catch (Exception e)
            {
                // TODO: need a logging system
            }

            List<Player> afterRange = this.Map.GetPlayersInRange(this);
            List<NPC> afterNPCRange = this.Map.GetNPCsInRange(this);

            string gmstring = P.AdminMode(this.LoginID);

            string mkc = P.MakeCharacter(this);
            // Send to all people that are in after but aren't in before MKC
            // MKC on client too
            foreach (Player player in afterRange.Except<Player>(beforeRange))
            {
                if (!IsGMInvisible)
                {
                    world.Send(player, mkc);
                    if (this.HasPrivilege(AccessPrivilege.GMInvisible))
                    {
                        world.Send(player, gmstring);
                    }
                }

                if (!player.IsGMInvisible)
                {
                    world.Send(this, P.MakeCharacter(player));
                    if (player.HasPrivilege(AccessPrivilege.GMInvisible))
                    {
                        world.Send(this, P.AdminMode(player.LoginID));
                    }
                }
            }

            // MKC all new npcs
            foreach (NPC npc in afterNPCRange.Except<NPC>(beforeNPCRange))
            {
                world.Send(this, P.MakeNPCCharacter(npc));
            }

            if (!IsGMInvisible)
            {
                // Send to everyone MOC
                string packet = P.MoveCharacter(this);
                foreach (Player player in afterRange.Union<Player>(beforeRange).Distinct<Player>())
                {
                    world.Send(player, packet);
                }
                // check if aggro any npcs
                foreach (NPC npc in afterNPCRange.Union<NPC>(beforeNPCRange).Distinct<NPC>())
                {
                    npc.AggroIfInRange(this, world);
                }
            }

            string erc = P.EraseCharacter(this.LoginID);
            // Send to all people that aren't in after but are in before ERC
            // Erase from client too
            foreach (Player player in beforeRange.Except<Player>(afterRange))
            {
                if (!IsGMInvisible)
                    world.Send(player, erc);

                world.Send(this, P.EraseCharacter(player.LoginID));
            }

            // Erase old npcs
            // Remove npc aggro towards player
            foreach (NPC npc in beforeNPCRange.Except<NPC>(afterNPCRange))
            {
                world.Send(this, P.EraseCharacter(npc.LoginID));
                npc.RemoveAggro(this);
            }
        }
        /**
         * WarpTo, warps player to map, x, y
         * Defaults to losing aggro
         *
         */
        public void WarpTo(GameWorld world, Map map, int x, int y)
        {
            this.WarpTo(world, map, x, y, true);
        }
        /**
         * WarpTo, warps player to map, x, y
         *
         */
        public void WarpTo(GameWorld world, Map map, int x, int y, bool loseaggro)
        {
            string erc = P.EraseCharacter(this.LoginID);
            foreach (Player player in this.Map.GetPlayersInRange(this))
            {
                if (!IsGMInvisible)
                    world.Send(player, erc);

                world.Send(this, P.EraseCharacter(player.LoginID));
            }
            foreach (NPC npc in this.Map.GetNPCsInRange(this))
            {
                world.Send(this, P.EraseCharacter(npc.LoginID));
                if (loseaggro) npc.RemoveAggro(this);
            }

            if (map == this.Map)
            {
                // Same map, no need to reload map
                // move off this square so null
                if (!IsGMInvisible)
                    this.Map.SetCharacter(null, this.MapX, this.MapY);

                this.MapX = x;
                this.MapY = y;

                if (!IsGMInvisible)
                {
                    this.Map.PlaceCharacter(this);
                    // move onto this square so this
                    this.Map.SetCharacter(this, this.MapX, this.MapY);
                }

                world.Send(this, P.SetYourPosition(this));

                string gmstring = P.AdminMode(this.LoginID);

                string mkc = P.MakeCharacter(this);
                foreach (Player player in this.Map.GetPlayersInRange(this))
                {
                    if (!IsGMInvisible)
                    {
                        world.Send(player, mkc);

                        if (this.HasPrivilege(AccessPrivilege.GMInvisible))
                        {
                            world.Send(player, gmstring);
                        }
                    }

                    if (!player.IsGMInvisible)
                    {
                        world.Send(this, P.MakeCharacter(player));
                        if (player.HasPrivilege(AccessPrivilege.GMInvisible))
                        {
                            world.Send(this, P.AdminMode(player.LoginID));
                        }
                    }
                }
                foreach (NPC npc in this.Map.GetNPCsInRange(this))
                {
                    world.Send(this, P.MakeNPCCharacter(npc));

                    if (!IsGMInvisible)
                        npc.AggroIfInRange(this, world);
                }
            }
            else
            {
                this.State = States.LoadingMap;
                if (!IsGMInvisible)
                {
                    // move off this square so null
                    this.Map.SetCharacter(null, this.MapX, this.MapY);
                }

                this.Map.RemovePlayer(this, world);
                this.Map = null;
                this.MapID = map.ID;
                this.MapX = x;
                this.MapY = y;

                world.Send(this, P.SendCurrentMap(map));
            }
        }

        /**
         * AddRegenEvent, adds regen event to eventhandler if needed
         *
         */
        public void AddRegenEvent(GameWorld world)
        {
            if (this.RegenEventExists) return;

            if ((this.CurrentHP == this.MaxHP) &&
                (this.CurrentMP == this.MaxMP) &&
                (this.CurrentSP == this.MaxSP))
            {
                // Already max stats
                return;
            }

            RegenEvent ev = new RegenEvent();
            ev.Ticks += (long)(GameWorld.Settings.RegenSpeed * world.TimerFrequency);
            ev.Data = this;

            this.RegenEventExists = true;

            world.EventHandler.AddEvent(ev);
        }

        /**
         * ChangeClass, changes players class
         *
         * Resets level/exp to starting values
         */
        public void ChangeClass(int classid, int newLevel, GameWorld world)
        {
            // todo unequip equipment i guess

            this.RemoveStats(this.BaseStats, world);

            this.MaxStats -= this.Class.GetLevel(this.Level).BaseStats;
            this.Level = newLevel;
            if (classid == 1)
            {
                this.ExperienceSold = this.Experience + this.ExperienceSold;
                // This is a hack, need a better solution
                this.ExperienceSold = (long)(this.ExperienceSold * (1.0d - GameWorld.Settings.ChangeClassExperienceLossPercent));
            }
            this.Experience = (this.Level == 1 ? 0 : this.Class.GetLevel(this.Level - 1).Experience);
            this.ClassID = classid;
            this.Class = world.ClassHandler.GetClass(this.ClassID);
            this.BaseStats.HP = 0;
            this.BaseStats.MP = 0;
            this.BoundID = GameWorld.Settings.StartingMapID;
            this.BoundMap = world.MapHandler.GetMap(this.BoundID);
            this.BoundX = GameWorld.Settings.StartingMapX;
            this.BoundY = GameWorld.Settings.StartingMapY;

            this.AddStats(this.Class.GetLevel(this.Level).BaseStats, world);
            this.AddStats(this.BaseStats, world);

            this.Spellbook.RemoveNonClassSpells(world);

            world.Send(this, P.StatusInfo(this));
            world.Send(this, P.ExpBar(this));
            world.Send(this, P.ServerMessage("Changed class to " + this.Class.ClassName + "."));

            for (int level = 1; level <= this.Level; level++)
            {
                if (level > this.Class.MaxLevel) break;

                foreach (Spell spell in this.Class.GetLevel(level).Spells)
                {
                    this.LearnSpell(spell.ID, world);
                }
            }
        }

        /**
         * SendInventory, sends inventory to player
         *
         */
        public void SendInventory(GameWorld world)
        {
            this.Inventory.SendAll(world);
        }

        /**
         * CanUse, returns true if player can use item
         *
         */
        public bool CanUse(Item item, GameWorld world)
        {
            if (this.HasPrivilege(AccessPrivilege.IgnoreItemRequirements)) return true;

            if (item.MinLevel != 0 && this.Level < item.MinLevel)
            {
                world.Send(this, P.ServerMessage("You are too low level to use " + item.Name + "."));
                return false;
            }
            if (item.MaxLevel != 0 && this.Level > item.MaxLevel)
            {
                world.Send(this, P.ServerMessage("You are too high level to use " + item.Name + "."));
                return false;
            }
            if ((item.MinExperience != 0) &&
                (this.Experience + this.ExperienceSold < item.MinExperience))
            {
                world.Send(this, P.ServerMessage(string.Format("You are too low experienced to use {0}. {1} experience required.", item.Name, item.MinExperience)));
                return false;
            }
            if ((item.MaxExperience != 0) &&
                (this.Experience + this.ExperienceSold > item.MaxExperience))
            {
                world.Send(this, P.ServerMessage(string.Format("You are too high experienced to use {0}. {1} experience maximum.", item.Name, item.MaxExperience)));
                return false;
            }

            if ((item.ClassRestrictions & Convert.ToInt64(Math.Pow(2.0, (double)this.Class.ClassID))) != 0)
            {
                world.Send(this, P.ServerMessage("You are the wrong class to use " + item.Name + "."));
                return false;
            }

            return true;
        }

        public virtual void SendCHPString(GameWorld world)
        {
            string chpstring = P.UpdateCharacter(this);
            world.Send(this, chpstring);
            foreach (Player player in this.Map.GetPlayersInRange(this))
            {
                world.Send(player, chpstring);
            }
        }

        public int CalculateMoveSpeed()
        {
            this.moveSpeed.TryPeek(out int speed, out int _);
            return speed;
        }

        /**
         * AddGold, adds amount of gold to player
         *
         */
        public void AddGold(long amount, GameWorld world)
        {
            this.Gold += amount;

            world.Send(this, P.StatusInfo(this));
        }

        /**
         * RemoveGold, removes amount of gold from player
         *
         */
        public void RemoveGold(long amount, GameWorld world)
        {
            if (amount > this.Gold) return;
            this.Gold -= amount;

            world.Send(this, P.StatusInfo(this));
        }

        /**
         * AddStats, add stats to player
         *
         */
        public void AddStats(AttributeSet stats, GameWorld world, bool updateCharacter = true)
        {
            this.MaxStats += stats;
            this.MaxStats.HP += (stats.Stamina * GameWorld.Settings.StaminaToHP);
            this.MaxStats.MP += (stats.Intelligence * GameWorld.Settings.IntelligenceToMP);

            if (stats.MoveSpeed != 0)
            {
                var oldSpeed = this.moveSpeed.Peek();
                this.moveSpeed.Enqueue(stats.MoveSpeed, stats.MoveSpeed);

                if (updateCharacter)
                {
                    var newSpeed = this.moveSpeed.Peek();

                    if (newSpeed < oldSpeed)
                    {
                        string updateCharacterPacket = P.UpdateCharacter(this);
                        world.Send(this, updateCharacterPacket);

                        var range = this.Map.GetPlayersInRange(this);
                        foreach (var p in range)
                        {
                            world.Send(p, updateCharacterPacket);
                        }
                    }
                }
            }

            this.CurrentHP = Math.Min(this.CurrentHP, this.MaxHP);
            this.CurrentMP = Math.Min(this.CurrentMP, this.MaxMP);
            this.CurrentSP = Math.Min(this.CurrentSP, this.MaxSP);

            world.Send(this, P.StatusInfo(this));
            this.AddRegenEvent(world);
        }

        /**
         * RemoveStats, remove stats from player
         *
         */
        public void RemoveStats(AttributeSet stats, GameWorld world, bool changeCurrentHPMP = true, bool updateCharacter = false)
        {
            this.MaxStats -= stats;
            this.MaxStats.HP -= (stats.Stamina * GameWorld.Settings.StaminaToHP);
            this.MaxStats.MP -= (stats.Intelligence * GameWorld.Settings.IntelligenceToMP);

            if (stats.MoveSpeed != 0)
            {
                var oldSpeed = this.moveSpeed.Peek();

                var speeds = this.moveSpeed.UnorderedItems.SkipFirstMatching(e => e.Element == oldSpeed).ToArray();
                this.moveSpeed.Clear();
                this.moveSpeed.EnqueueRange(speeds);

                if (updateCharacter)
                {
                    var newSpeed = this.moveSpeed.Peek();

                    if (oldSpeed != newSpeed)
                    {
                        string updateCharacterPacket = P.UpdateCharacter(this);
                        world.Send(this, updateCharacterPacket);

                        var range = this.Map.GetPlayersInRange(this);
                        foreach (var p in range)
                        {
                            world.Send(p, updateCharacterPacket);
                        }
                    }
                }
            }

            if (changeCurrentHPMP)
            {
                this.CurrentHP = Math.Min(this.CurrentHP, this.MaxHP);
                this.CurrentMP = Math.Min(this.CurrentMP, this.MaxMP);
                this.CurrentSP = Math.Min(this.CurrentSP, this.MaxSP);

                world.Send(this, P.StatusInfo(this));
                this.AddRegenEvent(world);
            }
        }

        /**
         * HasItem, returns true if player has templateid somewhere
         *
         */
        public bool HasItem(int templateid)
        {
            return this.Inventory.HasItem(templateid) || this.Bank.HasItem(templateid);
        }

        /**
         * Attack, attack character if possible
         *
         */
        public void Attack(ICharacter character, GameWorld world)
        {
            this.OnMeleeAttack(character, world);

            if (character is Player &&
                (!this.Map.CanPVP && this.Access != AccessStatus.GameMaster))
            {
                return;
            }

            double damage = 0;
            if (this.WeaponDamage == 1)
            {
                damage = this.MaxStats.Strength + 1 + (this.Level - character.Level);
            }
            else
            {
                damage = this.MaxStats.Strength + this.WeaponDamage +
                    this.Level + world.Random.Next(1, this.Level) + (this.Level - character.Level);
            }
            double maxac = GameWorld.Settings.MaxAC;
            double absorb = (1 - ((double)(character.MaxStats.AC * character.Class.ACMultiplier) / maxac));

            if (world.Random.Next(1, 10001) <= this.MaxStats.MeleeCrit * 10000) damage *= 2;
            damage *= (double)GameWorld.Settings.DamageModifier;
            damage *= (1 + (double)this.MaxStats.MeleeDamage);
            damage *= (1 - (double)character.MaxStats.DamageReduction);
            damage *= absorb;
            damage -= (double)(character.MaxStats.AC * character.Class.ACMultiplier / 25);

            character.Attacked(this, (long)damage, world);
            if (damage > 0)
            {
                character.OnMeleeHit(this, world);
            }
        }

        /**
         * WeaponDamage
         */
        public virtual int WeaponDamage
        {
            get { return this.Inventory.GetWeaponDamage(); }
            set { }
        }
        /**
         * WeaponDelay
         */
        public virtual int WeaponDelay
        {
            get { return this.Inventory.GetWeaponDelay(); }
            set { }
        }

        private static readonly System.Globalization.NumberFormatInfo xpFormatter
            = new System.Globalization.NumberFormatInfo { NumberGroupSeparator = " " };

        /**
         * AddExperience, player gained experience
         *
         */
        public virtual void AddExperience(long exp, GameWorld world, ExperienceMessage message)
        {
            if (GameWorld.Settings.ExperienceCap > 0 &&
                this.Experience + this.ExperienceSold > GameWorld.Settings.ExperienceCap)
            {
                if ((this.ToggleSettings & ToggleSetting.Experience) != 0) return;
                world.Send(this, P.ServerMessage("You have reached the experience cap. Gained 0 experience points."));
                return;
            }

            if (!(GameWorld.Settings.ExperienceModifierLimit > 0 &&
                this.Experience + this.ExperienceSold > GameWorld.Settings.ExperienceModifierLimit))
            {
                // Under the limit gets the full modifier
                exp = (long)(exp * (world.ExperienceModifier + AdditionalExperienceModifier));
            }
            else
            {
                // over the limit only gets player bonus
                exp = (long)(exp * (world.ExperienceModifier - GameWorld.Settings.ExperienceModifier + 1 + AdditionalExperienceModifier));
            }

            this.Experience += exp;

            if ((this.ToggleSettings & ToggleSetting.Experience) == 0)
            {
                switch (message)
                {
                    case ExperienceMessage.TooHigh:
                    case ExperienceMessage.Normal:
                        world.Send(this, P.BattleTextYellow(this, $"+{exp.ToString("N0", xpFormatter)} XP"));
                        break;

                    case ExperienceMessage.TooFarAway:
                        world.Send(this, P.ServerMessage("You were too far away to gain experience."));
                        break;

                    // case ExperienceMessage.TooHigh:
                    //     world.Send(this,
                    //         P.ServerMessage("You were too experienced, you only gained " + exp + " experience points."));
                    //     break;

                    case ExperienceMessage.TooLow:
                        world.Send(this, P.ServerMessage("Group members too high to gain any experience."));
                        break;

                    case ExperienceMessage.None:
                        break;
                }
            }

            long levelup;
            int levels = 0;

            int i = this.Level;
            ClassLevel level = this.Class.GetLevel(i);
            while (this.Class.GetLevel(i) != null)
            {
                levelup = level.Experience;
                if (levelup == 0) break;
                if (this.Experience >= levelup)
                {
                    levels++;
                    if (this.Class.GetLevel(i + 1) != null && this.Class.GetLevel(i + 1).Spells.Count > 0)
                    {
                        foreach (Spell spell in this.Class.GetLevel(i + 1).Spells)
                        {
                            this.LearnSpell(spell.ID, world);
                        }
                    }
                }
                else
                {
                    break;
                }

                i++;
                level = this.Class.GetLevel(i);
            }

            if (levels == 0)
            {
                world.Send(this, P.ExpBar(this));
                return;
            }

            this.RemoveStats(this.Class.GetLevel(this.Level).BaseStats, world);
            this.Level += levels;
            this.AddStats(this.Class.GetLevel(this.Level).BaseStats, world);
            this.CurrentHP = this.MaxHP;
            this.CurrentMP = this.MaxMP;
            world.Send(this, P.VitalsPercentage(this));
            if (levels == 1) world.Send(this, P.ServerMessage("You have gained a level."));
            else world.Send(this, P.ServerMessage("You have gained " + levels + " levels."));

            List<Player> range = this.Map.GetPlayersInRange(this);
            string packet = P.BattleTextYellow(this, "Level Up!");
            world.Send(this, packet);
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }

            if (this.Level == this.Class.MaxLevel)
            {
                this.Experience += this.ExperienceSold;
                this.ExperienceSold = 0;
            }

            world.Send(this, P.StatusInfo(this));
            world.Send(this, P.ExpBar(this));
        }

        /**
         * Player was attacked by character
         *
         */
        public virtual void Attacked(ICharacter character, long damage, GameWorld world)
        {
            if (this.State != States.Ready) return;

            List<Player> range = this.Map.GetPlayersInRange(this);

            string packet;

            if (damage == 0 || this.Access == AccessStatus.GameMaster)
            {
                packet = P.BattleTextMiss(this);
                world.Send(this, packet);
                foreach (Player p in range)
                {
                    world.Send(p, packet);
                }
                return;
            }

            if (damage > 0)
            {
                double dodge = this.MaxStats.Dexterity / 100.0;
                if (dodge > 50) dodge = 50;

                if (world.Random.Next(0, 10001) <= dodge * 100)
                {
                    packet = P.BattleTextDodge(this);
                    world.Send(this, packet);
                    foreach (Player p in range)
                    {
                        world.Send(p, packet);
                    }
                    return;
                }

                // pvp 1/3 damage
                if (character is Player) damage /= 3;
                packet = P.BattleTextDamage(this, damage) + "\x1";
            }
            else
            {
                packet = P.BattleTextHeal(this, damage) + "\x1";
            }

            this.CurrentHP -= damage;

            if (this.CurrentHP <= 0)
            {
                this.CurrentHP = (long)(this.MaxHP * 0.5);
                this.CurrentMP = (long)(this.MaxMP * 0.1);

                world.SendToMap(this.Map, P.ServerMessage(this.Name + " was slain by " + character.Name + "."));

                this.WarpTo(world, this.BoundMap, this.BoundX, this.BoundY);
                this.AddRegenEvent(world);
                world.Send(this, P.VitalsPercentage(this));
                world.Send(this, P.StatusInfo(this));

                // Remove all buffs on death
                List<Buff> removebuff = new List<Buff>();
                foreach (Buff b in this.Buffs)
                {
                    if (!b.ItemBuff) removebuff.Add(b);
                }

                foreach (Buff b in removebuff)
                {
                    this.RemoveBuff(b, world, false, updateCharacter: true);
                }

                this.SendBuffBar(world);
            }
            else
            {
                packet += P.VitalsPercentage(this);
                this.AddRegenEvent(world);
            }

            world.Send(this, P.StatusInfo(this));
            world.Send(this, packet);
            foreach (Player p in range)
            {
                world.Send(p, packet);
            }

            if (damage > 0)
            {
                foreach (Pet pet in this.Pets.Where(p => p.Mode == Pet.Modes.Defend && p.Target == null))
                {
                    pet.Target = character;
                    pet.AddAttackEvent(world);
                }
            }

            return;
        }

        /**
         * AddSaveEvent, Adds save event. Also does ping timeout stuff
         *
         */
        public void AddSaveEvent(GameWorld world)
        {
            if (this.LastPing == 0) this.LastPing = world.TimeNow;

            if ((world.TimeNow - this.LastPing) >
                ((GameWorld.Settings.PlayerSavePeriod * 1.10) * world.TimerFrequency))
            {
                world.LostConnection(this.Sock);
            }
            else
            {
                world.Send(this, "PING");

                PlayerSaveEvent ev = new PlayerSaveEvent();
                ev.Player = this;
                ev.Ticks += (GameWorld.Settings.PlayerSavePeriod * world.TimerFrequency);

                world.EventHandler.AddEvent(ev);
            }
        }

        /**
         * LearnSpell, learns spell id
         *
         */
        public bool LearnSpell(int spellid, GameWorld world)
        {
            return this.Spellbook.LearnSpell(spellid, world);
        }

        /**
         * SendSpellbook, sends spellbook to player
         *
         */
        public void SendSpellbook(GameWorld world)
        {
            this.Spellbook.SendAll(world);
        }

        /**
         * CastSpell, casts spellslot spell on target
         *
         */
        public void CastSpell(int spellslot, ICharacter target, GameWorld world)
        {
            Spell spell = this.Spellbook.GetSlot(spellslot);
            if (spell == null) return;

            foreach (Buff b in this.Buffs)
            {
                // can't cast when stunned
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                {
                    world.Send(this, P.BattleTextStunned(this));
                    return;
                }
            }

            foreach (Window window in this.Windows)
            {
                if (window.Type == Window.WindowTypes.Vendor)
                {
                    world.Send(this, P.ServerMessage("You can't cast spells while with a vendor."));
                    return;
                }
            }

            if (!this.Class.CanUse(spell.ClassRestrictions) && !this.HasPrivilege(AccessPrivilege.IgnoreItemRequirements))
            {
                world.Send(this, P.ServerMessage("You are the wrong class to use this spell."));
                return;
            }

            if ((spell.Target == Spell.SpellTargets.Group || spell.Target == Spell.SpellTargets.Self) &&
                target != this)
            {
                target = this;
                // log bad target
            }

            long lastcast = this.Spellbook.GetSlotLastCast(spellslot);
            long now = world.TimeNow;

            if (now - lastcast >= (long)((spell.Aether / 1000.0) * world.TimerFrequency))
            {
                if (this.CurrentHP >= spell.HPStaticCost &&
                    this.CurrentMP >= spell.MPStaticCost)
                    //this.CurrentSP >= spell.SPStaticCost) // for testing sp spells don't check the cost..
                {
                    if (spell.Target == Spell.SpellTargets.Group)
                    {
                        if (this.Group != null)
                        {
                            foreach (Player p in this.Group.Players)
                            {
                                if (p != this && p.Map == this.Map &&
                                    Math.Abs(p.MapX - this.MapX) <= Map.RANGE_X &&
                                    Math.Abs(p.MapY - this.MapY) <= Map.RANGE_Y)
                                {
                                    spell.SpellEffect.Cast(this, p, world);
                                }
                            }
                        }

                        spell.SpellEffect.Cast(this, this, world);

                    }
                    else
                    {
                        spell.SpellEffect.Cast(this, target, world);
                    }

                    this.CurrentHP -= spell.HPStaticCost;
                    this.CurrentMP -= spell.MPStaticCost;
                    this.CurrentSP -= spell.SPStaticCost;

                    this.CurrentHP -= (long)(this.CurrentHP * (spell.HPPercentCost / 100.0m));
                    this.CurrentMP -= (long)(this.CurrentMP * (spell.MPPercentCost / 100.0m));
                    this.CurrentSP -= (long)(this.CurrentSP * (spell.SPPercentCost / 100.0m));

                    if (this.CurrentHP <= 0) this.CurrentHP = 1;
                    if (this.CurrentMP < 0) this.CurrentMP = 0;
                    if (this.CurrentSP < 0) this.CurrentSP = 0;

                    this.Spellbook.SetSlotLastCast(spellslot, now);

                    this.AddRegenEvent(world);

                    if (this.State == States.Ready)
                    {
                        string packet = P.VitalsPercentage(this);

                        world.Send(this, packet);
                        world.Send(this, P.StatusInfo(this));
                        foreach (Player player in this.Map.GetPlayersInRange(this))
                        {
                            world.Send(player, packet);
                        }
                    }
                }
                else
                {
                    string packet = P.BattleTextYellow(this, "Fizzle");

                    world.Send(this, packet);
                    foreach (Player player in this.Map.GetPlayersInRange(this))
                    {
                        world.Send(player, packet);
                    }
                }
            }
            else
            {
                decimal wait = (((decimal)((spell.Aether / 1000.0) * world.TimerFrequency) - (now - lastcast))
                    / world.TimerFrequency);
                wait = Math.Round(wait, 2);
                if (wait >= this.AetherThreshold)
                {
                    world.Send(this, P.BattleTextYellow(this, Utils.FormatDuration((long)(wait * 1000))));
                    //world.Send(this, P.ServerMessage("You must wait " + Utils.FormatDuration((long)(wait * 1000)) + " to cast this spell."));
                }
            }

        }

        public void AddBuff(Buff buff, GameWorld world)
        {
            this.AddBuff(buff, world, true);
        }

        /**
         * AddBuff, add buff to players buff list
         *
         */
        public void AddBuff(Buff buff, GameWorld world, bool refreshbar, bool updateCharacter = true)
        {
            if (this.State <= States.LoadingGame)
            {
                this.Buffs.Add(buff);

                // Add/remove stats
                this.AddStats(buff.SpellEffect.Stats, world, updateCharacter: false);


                return;
            }

            List<Player> range = this.Map.GetPlayersInRange(this);
            string packet;

            foreach (Buff b in this.Buffs)
            {
                if (buff.SpellEffect.BuffDoesntStackOver.Contains(b.SpellEffect))
                {
                    world.Send(this, P.ServerMessage("The buff had no effect."));
                    return;
                }

                // already have that buff so renew the time cast
                if (!b.ItemBuff && !buff.ItemBuff &&
                    (buff.SpellEffect == b.SpellEffect ||
                    buff.SpellEffect.BuffStacksOver.Contains(b.SpellEffect)))
                {
                    this.RemoveStats(b.SpellEffect.Stats, world);
                    this.AddStats(buff.SpellEffect.Stats, world, updateCharacter: updateCharacter);

                    world.Send(this, P.WeaponSpeed(this));

                    b.TimeCast = world.TimeNow;
                    b.SpellEffect = buff.SpellEffect;
                    b.Caster = buff.Caster;

                    if (buff.SpellEffect.Animation != 0)
                    {
                        packet = P.SpellPlayer(this.LoginID, buff.SpellEffect.Animation, buff.SpellEffect.AnimationFile);
                        if (buff.SpellEffect.DoAttackAnimation)
                            packet += "\x1" + P.Attack(this); // kinda weird but k

                        world.Send(this, packet);
                        foreach (Player player in range)
                        {
                            world.Send(player, packet);
                        }
                    }
                    if (b.SpellEffect.OffEffectText != "") world.Send(this, P.ServerMessage(buff.SpellEffect.OffEffectText));
                    if (buff.SpellEffect.OnEffectText != "") world.Send(this, P.ServerMessage(buff.SpellEffect.OnEffectText));

                    this.SendBuffBar(world);

                    return;
                }
            }

            if (buff.SpellEffect.Duration > 0)
            {
                // else we don't have the buff. add it
                Event ev = new BuffExpireEvent();
                ev.Ticks += buff.SpellEffect.Duration * world.TimerFrequency;
                ev.Player = this;
                ev.Data = buff;

                world.EventHandler.AddEvent(ev);
                buff.BuffExpireEvent = ev;
            }

            if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Tick ||
                buff.SpellEffect.EffectType == SpellEffect.EffectTypes.TickBuff ||
                buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Viral ||
                buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Root ||
                buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
            {
                // buff will expire before next tick
                if (buff.BuffExpireEvent.Ticks - world.TimeNow >
                    GameWorld.Settings.SpellEffectPeriod * world.TimerFrequency)
                {
                    BuffTickEvent ev = new BuffTickEvent();
                    ev.Data = buff;
                    ev.Player = this;
                    ev.Ticks += (long)(GameWorld.Settings.SpellEffectPeriod * world.TimerFrequency);

                    world.EventHandler.AddEvent(ev);
                }
            }

            this.Buffs.Add(buff);

            // Add/remove stats
            this.AddStats(buff.SpellEffect.Stats, world, updateCharacter: updateCharacter);

            try
            {
                buff.SpellEffect?.Script?.Object.OnBuffAdded(buff, world);
            }
            catch (Exception e) { }

            if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Tick)
            {
                buff.SpellEffect.CastFormulaSpell(buff.Caster, buff.Target, world);
            }

            packet = P.VitalsPercentage(this);

            if (buff.SpellEffect.Stats.Haste != Decimal.Zero)
            {
                world.Send(this, P.WeaponSpeed(this));
            }

            // for illusions
            if (buff.SpellEffect.BodyID != 0)
            {
                this.CurrentBodyID = buff.SpellEffect.BodyID;
                packet += "\x1" + P.UpdateCharacter(this);
            }

            this.AddRegenEvent(world);

            if (buff.SpellEffect.Animation != 0)
                packet += "\x1" + P.SpellPlayer(this.LoginID, buff.SpellEffect.Animation, buff.SpellEffect.AnimationFile);
            if (buff.SpellEffect.DoAttackAnimation) packet += "\x1" + P.Attack(this); // kinda weird but k

            if (buff.SpellEffect.OnEffectText != "") world.Send(this, P.ServerMessage(buff.SpellEffect.OnEffectText));
            world.Send(this, P.StatusInfo(this));
            world.Send(this, packet);
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }

            if (refreshbar) this.SendBuffBar(world);
        }

        public bool IsMounted()
        {
            return this.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount) != null;
        }

        public void RemoveBuff(Buff buff, GameWorld world)
        {
            this.RemoveBuff(buff, world, true);
        }

        /**
         * RemoveBuff, removes buff from buffs list
         *
         */
        public void RemoveBuff(Buff buff, GameWorld world, bool refreshbar, bool updateCharacter = true)
        {
            this.Buffs.Remove(buff);

            if (buff.BuffExpireEvent != null)
            {
                world.EventHandler.RemoveEvent(buff.BuffExpireEvent);
                buff.BuffExpireEvent = null;
            }

            // Add/remove stats
            this.RemoveStats(buff.SpellEffect.Stats, world, updateCharacter: updateCharacter);

            try
            {
                buff.SpellEffect?.Script?.Object.OnBuffRemoved(buff, world);
            }
            catch (Exception e) { }

            string packet = P.VitalsPercentage(this);

            if (buff.SpellEffect.Stats.Haste != Decimal.Zero)
            {
                world.Send(this, P.WeaponSpeed(this));
            }

            // for illusions
            if (buff.SpellEffect.BodyID != 0)
            {
                this.CurrentBodyID = this.BodyID;
                packet += "\x1" + P.UpdateCharacter(this);
            }

            this.AddRegenEvent(world);

            if (this.State == States.Ready)
            {
                List<Player> range = this.Map.GetPlayersInRange(this);

                if (buff.SpellEffect.OffEffectText != "") world.Send(this, P.ServerMessage(buff.SpellEffect.OffEffectText));
                world.Send(this, P.StatusInfo(this));
                world.Send(this, packet);
                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }
            }

            if (refreshbar) this.SendBuffBar(world);
        }

        /**
         * SendBuffBar, sends buff bar to player
         *
         */
        public void SendBuffBar(GameWorld world)
        {
            if (this.State <= States.LoadingGame) return;

            int i = 1;

            foreach (Buff buff in this.Buffs)
            {
                if (buff.ItemBuff && !this.ShowItemBuffs) continue;

                world.Send(this, P.BuffBar(buff, i));
                i++;
            }

            while (i <= GameWorld.Settings.BuffBarVisibleSize)
            {
                world.Send(this, P.BuffBar(null, i));
                i++;
            }
        }


        /**
         * OnMeleeHit, when hit by melee cast any reaction spells
         *
         */
        public void OnMeleeHit(ICharacter hitter, GameWorld world)
        {
            foreach (Buff b in this.Buffs)
            {
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.OnMeleeHit)
                {
                    if (world.Random.Next(1, 10001) <= b.SpellEffect.OnMeleeHitSpellChance * 100)
                        b.SpellEffect.OnMeleeHitSpell.Cast(this, hitter, world);
                }
            }
        }

        /**
         * OnMeleeAttack, casts melee attack spells when we hit something
         *
         */
        public void OnMeleeAttack(ICharacter hit, GameWorld world)
        {
            foreach (Buff b in this.Buffs)
            {
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.OnAttack)
                {
                    if (world.Random.Next(1, 10001) <= b.SpellEffect.OnMeleeAttackSpellChance * 100)
                        b.SpellEffect.OnMeleeAttackSpell.Cast(this, this, world);
                }
            }
        }

        /// <summary>
        /// Adds a pet to this player's control
        /// </summary>
        /// <param name="pet">pet to add</param>
        public void AddPet(Pet pet)
        {
            pet.Owner = this;
            this.Pets.Add(pet);
        }

        /// <summary>
        /// Updates the player's idle status to active
        /// </summary>
        /// <param name="world"></param>
        public void UpdateIdleStatus(GameWorld world)
        {
            this.isIdle = false;
            this.LastActive = world.TimeNow;
        }

        /// <summary>
        /// Checks if the player is idle
        /// </summary>
        public bool IsIdle(GameWorld world)
        {
            if (this.isIdle) return true;

            if (this.LastActive + (GameWorld.Settings.IdleTimeout * world.TimerFrequency) <= world.TimeNow)
            {
                this.isIdle = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the players play/afk timers
        /// </summary>
        /// <param name="world"></param>
        public void UpdatePlayTime(GameWorld world)
        {
            long afkTime = 0;
            long playTime = 0;

            if (this.LastActive < this.LastPlaytimeUpdate)
            {
                // LastActive ---------- LastUpdate ---------- Now
                // afk = now - lastupdate
                // played = 0

                afkTime = world.TimeNow - this.LastPlaytimeUpdate;
            }
            else
            {
                // LastUpdate ---------- LastActive ---------- Now
                // afk = now - lastactive
                // played = lastactive - lastupdate

                afkTime = world.TimeNow - this.LastActive;
                playTime = this.LastActive - this.LastPlaytimeUpdate;
            }

            this.TotalAfkTime += (afkTime / world.TimerFrequency);
            this.TotalPlayTime += (playTime / world.TimerFrequency);

            this.LastPlaytimeUpdate = world.TimeNow;
        }

        public void Send(string data)
        {
            if (this.sock == null) return;

            var bytes = Encoding.ASCII.GetBytes(data);

            lock (socketLock)
            {
                this.sock.Send(bytes);
            }
        }

        public void SetPassword(string password)
        {
            var saltBytes = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(saltBytes);

            var salt = Encoding.ASCII.GetString(saltBytes);
            var base64Salt = Convert.ToBase64String(saltBytes);

            var md5 = new MD5CryptoServiceProvider();
            var data = Encoding.ASCII.GetBytes(salt + password + GameWorld.Settings.ServerName);

            var passwordHash = BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "").ToLower();

            this.PasswordHash = passwordHash;
            this.PasswordSalt = base64Salt;
        }
    }
}
