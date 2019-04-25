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
                this.currentSP = Math.Min(value, this.MaxStats.SP);
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

        public long? TemporaryMaxHP { get; set; }

        public long? TemporaryMaxMP { get; set; }

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

        internal List<QuestCompleted> QuestsCompleted { get; set; }
        internal List<QuestStarted> QuestsStarted { get; set; }
        internal List<QuestProgress> QuestProgress { get; set; }

        public int MacroCheckFailures { get; set; }

        public long LastMacroCheckTime { get; set; }

        public MacroCheckEvent MacroCheckEvent { get; set; }

        public DateTime? UnbanDate { get; set; }


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
            this.QuestsCompleted = new List<QuestCompleted>();
            this.QuestsStarted = new List<QuestStarted>();

            this.GroupInvitesEnabled = false;

            this.MovementRecordingSteps = 0;
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
         * MKCString, returns the MKC packet string for this character
         * 
         * MKCid,character type,name,title,surname,guild,x,y,facing,hp percent,body,
         * body pose,hair id,chest id,chest r,g,b,a,helm id,helm r,g,b,a,
         * pants id,pants r,g,b,a,shoes id,shoes r,g,b,a,
         * shield id,shield r,g,b,a,weapon id,weapon r,g,b,a,hair_r,hair_g,hair_b,hair_a,invis,head
         *
         * character type = 1 for player, 2 for regular npc, some others for banker and vendors will find later
         * hair id = 20ish for the normal hairs
         * head = 70-73 for faces
         * body pose/state = 1 for normal, 3 for staff, 4 for sword
         * body = values 100-166 are illusions, 1 is male, 11 is female. 2/12 are naga. 3 is skeleton
         * invis = not sure at moment
         * 
         * For item r,g,b,a of 0,0,0,0 you can use * instead
         * 
         */
        public virtual string MKCString()
        {
            int pose = this.BodyState;
            ItemSlot weapon = this.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
            if (weapon != null)
            {
                pose = weapon.Item.BodyState;
            }

            return "MKC" + this.LoginID + "," +
                           "1," +
                          this.Name + "," +
                          this.Title + "," +
                          this.Surname + "," +
                          "" + "," + // Guild name
                          this.MapX + "," +
                          this.MapY + "," +
                          this.Facing + "," +
                          (int)(((float)this.CurrentHP / this.MaxHP) * 100) + "," + // HP %
                          this.CurrentBodyID + "," +
                          this.BodyR + "," + // Body Color R
                          this.BodyG + "," + // Body Color G
                          this.BodyB + "," + // Body Color B
                          this.BodyA + "," + // Body Color A
                          (this.CurrentBodyID >= 100 ? 3 : pose) + "," +
                          (this.CurrentBodyID >= 100 ? "" : this.HairID + ",") +
                          (this.CurrentBodyID >= 100 ? "" : this.Inventory.EquippedDisplay()) + // Note: EquippedDisplay() adds it's own , on end
                          (this.CurrentBodyID >= 100 ? "" : this.HairR + "," + HairG + "," + HairB + "," + HairA + ",") +
                          "0" + "," + // Invis thing
                          (this.CurrentBodyID >= 100 ? "" : this.FaceID + ",") +
                          this.CalculateMoveSpeed() + "," + // Move Speed
                          (this.Access > AccessStatus.Normal ? "1" : "0") + "," + // Is GM
                          (this.CurrentBodyID >= 100 ? "" : this.Inventory.MountDisplay()); // Mount
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
            byte[] data = Encoding.ASCII.GetBytes(salt + password + GameSettings.Default.ServerName);
            data = md5.ComputeHash(data);

            string passwordHash = BitConverter.ToString(data).Replace("-", "").ToLower();

            this.AutoCreatedNotSaved = true;
            this.PlayerID = world.PlayerHandler.CurrentID;
            world.PlayerHandler.CurrentID++;
            this.Name = name;
            this.Title = GameSettings.Default.StartingTitle;
            this.Surname = GameSettings.Default.StartingSurname;
            this.PasswordHash = passwordHash;
            this.PasswordSalt = base64Salt;
            this.Access = AccessStatus.Normal;
            this.MapID = GameSettings.Default.StartingMapID;
            this.MapX = GameSettings.Default.StartingMapX;
            this.MapY = GameSettings.Default.StartingMapY;

            this.Facing = 2;
            this.BoundID = GameSettings.Default.StartingMapID;
            this.BoundX = GameSettings.Default.StartingMapX;
            this.BoundY = GameSettings.Default.StartingMapY;
            this.BoundMap = world.MapHandler.GetMap(this.BoundID);
            this.Gold = GameSettings.Default.StartingGold;
            this.Level = GameSettings.Default.StartingLevel;
            this.ClassID = GameSettings.Default.StartingClassID;
            this.GuildID = GameSettings.Default.StartingGuildID;
            this.Guild = world.GuildHandler.GetGuild(this.GuildID);
            this.Experience = GameSettings.Default.StartingExperience;
            this.ExperienceSold = GameSettings.Default.StartingExperienceSold;
            this.BodyID = GameSettings.Default.StartingBodyID;
            this.BodyR = GameSettings.Default.StartingBodyR;
            this.BodyG = GameSettings.Default.StartingBodyG;
            this.BodyB = GameSettings.Default.StartingBodyB;
            this.BodyA = GameSettings.Default.StartingBodyA;
            this.CurrentBodyID = this.BodyID;
            this.FaceID = GameSettings.Default.StartingFaceID;
            this.HairID = GameSettings.Default.StartingHairID;
            this.HairR = GameSettings.Default.StartingHairR;
            this.HairG = GameSettings.Default.StartingHairG;
            this.HairB = GameSettings.Default.StartingHairB;
            this.HairA = GameSettings.Default.StartingHairA;

            this.BaseStats = new AttributeSet();
            this.BaseStats.HP = GameSettings.Default.StartingHP;
            this.BaseStats.MP = GameSettings.Default.StartingMP;
            this.BaseStats.SP = GameSettings.Default.StartingSP;
            this.BaseStats.AC = GameSettings.Default.StartingAC;
            this.BaseStats.Strength = GameSettings.Default.StartingStrength;
            this.BaseStats.Stamina = GameSettings.Default.StartingStamina;
            this.BaseStats.Intelligence = GameSettings.Default.StartingIntelligence;
            this.BaseStats.Dexterity = GameSettings.Default.StartingDexterity;
            this.BaseStats.FireResist = GameSettings.Default.StartingFireResist;
            this.BaseStats.AirResist = GameSettings.Default.StartingAirResist;
            this.BaseStats.EarthResist = GameSettings.Default.StartingEarthResist;
            this.BaseStats.SpiritResist = GameSettings.Default.StartingSpiritResist;
            this.BaseStats.WaterResist = GameSettings.Default.StartingWaterResist;
            this.BaseStats.MoveSpeed = GameSettings.Default.StartingMoveSpeed;

            this.MaxStats = new AttributeSet();
            this.MaxStats += this.BaseStats;
            this.MaxStats.Haste = GameSettings.Default.BaseHaste;
            this.MaxStats.SpellDamage = GameSettings.Default.BaseSpellDamage;
            this.MaxStats.SpellCrit = GameSettings.Default.BaseSpellCrit;
            this.MaxStats.MeleeDamage = GameSettings.Default.BaseMeleeDamage;
            this.MaxStats.MeleeCrit = GameSettings.Default.BaseMeleeCrit;
            this.MaxStats.DamageReduction = GameSettings.Default.BaseDamageReduction;
            this.MaxStats.HPPercentRegen = GameSettings.Default.BaseHPPercentRegen;
            this.MaxStats.HPStaticRegen = GameSettings.Default.BaseHPStaticRegen;
            this.MaxStats.MPPercentRegen = GameSettings.Default.BaseMPPercentRegen;
            this.MaxStats.MPStaticRegen = GameSettings.Default.BaseMPStaticRegen;
            this.MaxStats.MoveSpeedIncrease = GameSettings.Default.BaseMoveSpeedIncrease;

            this.Class = world.ClassHandler.GetClass(this.ClassID);
            this.MaxStats += this.Class.GetLevel(this.Level).BaseStats;

            this.BodyState = 3;

            this.ToggleSettings = (ToggleSetting)GameSettings.Default.DefaultToggleSettings;
            this.AetherThreshold = GameSettings.Default.DefaultAetherThreshold;

            this.NumberOfBankPages = GameSettings.Default.StartingBankPages;
            this.Credits = 0;
            this.TotalAfkTime = 0;
            this.TotalPlayTime = 0;

            this.LastActive = world.TimeNow;
            this.LastPlaytimeUpdate = world.TimeNow;

            this.Inventory = new Inventory(this);
            string[] items = GameSettings.Default.StartingItems.Split(" ".ToCharArray());
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
                        world.ItemHandler.AddItem(item, world);

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
        }

        /**
         * LoadFromReader, loads player info from a Sq1DataReader
         *
         */
        public void LoadFromReader(GameWorld world, SqlDataReader reader)
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
            this.MaxStats.Haste = GameSettings.Default.BaseHaste;
            this.MaxStats.SpellDamage = GameSettings.Default.BaseSpellDamage;
            this.MaxStats.SpellCrit = GameSettings.Default.BaseSpellCrit;
            this.MaxStats.MeleeDamage = GameSettings.Default.BaseMeleeDamage;
            this.MaxStats.MeleeCrit = GameSettings.Default.BaseMeleeCrit;
            this.MaxStats.DamageReduction = GameSettings.Default.BaseDamageReduction;
            this.MaxStats.HPPercentRegen = GameSettings.Default.BaseHPPercentRegen;
            this.MaxStats.HPStaticRegen = GameSettings.Default.BaseHPStaticRegen;
            this.MaxStats.MPPercentRegen = GameSettings.Default.BaseMPPercentRegen;
            this.MaxStats.MPStaticRegen = GameSettings.Default.BaseMPStaticRegen;
            this.MaxStats.MoveSpeedIncrease = GameSettings.Default.BaseMoveSpeedIncrease;

            this.Class = world.ClassHandler.GetClass(this.ClassID);
            this.MaxStats += this.Class.GetLevel(this.Level).BaseStats;

            this.ToggleSettings = (ToggleSetting)Convert.ToInt64(reader["toggle_settings"]);
            this.AetherThreshold = Convert.ToDecimal(reader["aether_threshold"]);

            this.NumberOfBankPages = Convert.ToInt32(reader["bank_pages"]);
            this.Credits = Convert.ToInt32(reader["donation_credits"]);
            this.TotalPlayTime = Convert.ToInt64(reader["total_playtime"]);
            this.TotalAfkTime = Convert.ToInt64(reader["total_afktime"]);

            var unbanDate = reader["unban_date"];
            this.UnbanDate = (unbanDate == DBNull.Value ? null : (DateTime?)unbanDate);

            this.MacroCheckFailures = Convert.ToInt32(reader["macrocheck_failures"]);

            this.LastActive = world.TimeNow;
            this.LastPlaytimeUpdate = world.TimeNow;
        }


        public void LoadAdditional(GameWorld world)
        {
            this.Inventory = new Inventory(this);
            this.Inventory.Load(world);
            this.Spellbook = new Spellbook(this);
            this.Spellbook.Load(world);
            this.Bank = new PlayerBank();
            this.Bank.Load(world, this);

            this.BodyState = 3;

            this.LoadPets(world);
            this.LoadQuests(world);
        }

        /// <summary>
        /// Loads all pets from database
        /// </summary>
        /// <param name="world"></param>
        public void LoadPets(GameWorld world)
        {
            SqlCommand query = new SqlCommand("SELECT * FROM pets WHERE owner_id=" + this.PlayerID, world.SqlConnection);
            SqlDataReader reader = query.ExecuteReader();

            while (reader.Read())
            {
                this.AddPet(Pet.FromReader(reader, world));
            }

            reader.Close();
        }

        public void LoadQuests(GameWorld world)
        {
            SqlCommand query = new SqlCommand("SELECT * FROM quest_started WHERE player_id=" + this.PlayerID, world.SqlConnection);
            using (SqlDataReader reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    var started = QuestStarted.FromReader(reader, world);
                    if (started != null)
                        this.QuestsStarted.Add(started);
                }
            }

            query = new SqlCommand("SELECT * FROM quest_completed WHERE player_id=" + this.PlayerID, world.SqlConnection);
            using (SqlDataReader reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    var completed = QuestCompleted.FromReader(reader, world);
                    if (completed != null)
                        this.QuestsCompleted.Add(completed);
                }
            }

            query = new SqlCommand("SELECT * FROM quest_progress WHERE player_id=" + this.PlayerID, world.SqlConnection);
            using (SqlDataReader reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    var progress = Quests.QuestProgress.FromReader(reader, world, this);
                    if (progress != null)
                        this.QuestProgress.Add(progress);
                }
            }
        }

        /**
         * SaveToDatabase, saves player info to database
         * 
         */
        public virtual void SaveToDatabase(GameWorld world)
        {
            SqlParameter playerNameParam = new SqlParameter("@playerName", SqlDbType.VarChar, 50);
            playerNameParam.Value = this.Name;
            SqlParameter playerTitleParam = new SqlParameter("@playerTitle", SqlDbType.VarChar, 50);
            playerTitleParam.Value = this.Title;
            SqlParameter playerSurnameParam = new SqlParameter("@playerSurname", SqlDbType.VarChar, 50);
            playerSurnameParam.Value = this.Surname;
            SqlParameter unbanDateParam = new SqlParameter("@unbanDate", SqlDbType.DateTime2);
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

                SqlCommand command = new SqlCommand(query, world.SqlConnection);
                command.Parameters.Add(playerNameParam);
                command.Parameters.Add(playerTitleParam);
                command.Parameters.Add(playerSurnameParam);
                command.Parameters.Add(unbanDateParam);
                command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
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

                SqlCommand command = new SqlCommand(query, world.SqlConnection);
                command.Parameters.Add(playerNameParam);
                command.Parameters.Add(playerTitleParam);
                command.Parameters.Add(playerSurnameParam);
                command.Parameters.Add(unbanDateParam);
                command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);
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
                    progress.Dirty = true;
                    if (!QuestCreditFilterEnabled)
                    {
                        world.Send(this, string.Format("BT{0},{1},Quest Credit: {2}", this.LoginID, 60, progress.Requirement.Quest.Name));
                    }
                }
            }
        }

        private void SaveQuests(GameWorld world)
        {
            foreach (var questCompleted in this.QuestsCompleted)
            {
                if (questCompleted.Dirty)
                {
                    var query = "INSERT INTO quest_completed (quest_id, player_id) VALUES (" + questCompleted.Quest.Id + ", " + this.PlayerID + ");";
                    var command = new SqlCommand(query, world.SqlConnection);
                    command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);

                    questCompleted.Dirty = false;
                }
            }

            foreach (var questStarted in this.QuestsStarted)
            {
                if (questStarted.Dirty)
                {
                    var query = "INSERT INTO quest_started (quest_id, player_id) VALUES (" + questStarted.Quest.Id + ", " + this.PlayerID + ");";
                    var command = new SqlCommand(query, world.SqlConnection);
                    command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);

                    questStarted.Dirty = false;
                }
            }

            List<QuestProgress> toRemove = new List<Quests.QuestProgress>();
            foreach (var questProgress in this.QuestProgress)
            {
                if (questProgress.Remove && questProgress.Id != 0)
                {
                    var query = "DELETE FROM quest_progress WHERE id=" + questProgress.Id + ";";
                    var command = new SqlCommand(query, world.SqlConnection);
                    command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);

                    toRemove.Add(questProgress);
                }
                else if (questProgress.Id == 0)
                {
                    // TODO: Is this going to be stable, is it even worth spawning a task for this?
                    Task.Factory.StartNew(() =>
                    {
                        var qp = questProgress;
                        var query = "INSERT INTO quest_progress (requirement_id, player_id, progress_value) OUTPUT INSERTED.id VALUES (" + qp.Requirement.Id + ", " + this.PlayerID + ", " + qp.Value + ");";
                        var command = new SqlCommand(query, world.SqlConnection);
                        qp.Id = (int)command.ExecuteScalar();

                        qp.Dirty = false;
                    });
                }
                else if (questProgress.Dirty)
                {
                    var query = "UPDATE quest_progress SET progress_value = " + questProgress.Value + " WHERE id=" + questProgress.Id + ";";
                    var command = new SqlCommand(query, world.SqlConnection);
                    command.BeginExecuteNonQuery(new AsyncCallback(GameWorld.DefaultEndExecuteNonQueryAsyncCallback), command);

                    questProgress.Dirty = false;
                }
            }

            foreach (var questProgress in toRemove)
            {
                this.QuestProgress.Remove(questProgress);
            }
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
        public void MoveTo(GameWorld world, int x, int y)
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

            string gmstring = "AMA" + this.LoginID + ",1";

            string mkc = this.MKCString();
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
                    world.Send(this, player.MKCString());
                    if (player.HasPrivilege(AccessPrivilege.GMInvisible))
                    {
                        world.Send(this, "AMA" + player.LoginID + ",1");
                    }
                }
            }

            // MKC all new npcs
            foreach (NPC npc in afterNPCRange.Except<NPC>(beforeNPCRange))
            {
                world.Send(this, npc.MKCString());
            }

            if (!IsGMInvisible)
            {
                // Send to everyone MOC
                string packet = "MOC" + this.LoginID + "," + this.MapX + "," + this.MapY;
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

            string erc = "ERC" + this.LoginID;
            // Send to all people that aren't in after but are in before ERC
            // Erase from client too
            foreach (Player player in beforeRange.Except<Player>(afterRange))
            {
                if (!IsGMInvisible)
                    world.Send(player, erc);

                world.Send(this, "ERC" + player.LoginID);
            }

            // Erase old npcs
            // Remove npc aggro towards player
            foreach (NPC npc in beforeNPCRange.Except<NPC>(afterNPCRange))
            {
                world.Send(this, "ERC" + npc.LoginID);
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
            string erc = "ERC" + this.LoginID;
            foreach (Player player in this.Map.GetPlayersInRange(this))
            {
                if (!IsGMInvisible)
                    world.Send(player, erc);

                world.Send(this, "ERC" + player.LoginID);
            }
            foreach (NPC npc in this.Map.GetNPCsInRange(this))
            {
                world.Send(this, "ERC" + npc.LoginID);
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

                world.Send(this, "SUP" + this.MapX + "," + this.MapY);

                string gmstring = "AMA" + this.LoginID + ",1";

                string mkc = this.MKCString();
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
                        world.Send(this, player.MKCString());
                        if (player.HasPrivilege(AccessPrivilege.GMInvisible))
                        {
                            world.Send(this, "AMA" + player.LoginID + ",1");
                        }
                    }
                }
                foreach (NPC npc in this.Map.GetNPCsInRange(this))
                {
                    world.Send(this, npc.MKCString());

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

                world.Send(this, "SCM" + map.FileName + ",1," + map.Name + ",0");
            }
        }

        /**
         * SNFString, send info string
         * 
         * SNFguildname,,classname,level,max_hp,max_mp,max_sp,cur_,cur_mp,cur_sp,
         * stat_str,stat_sta,stat_int,stat_dex,ac,res_f,res_w,res_e,res_a,res_s,gold 
         */
        public string SNFString()
        {
            return "SNF" +
                   (this.Guild != null ? this.Guild.Name : "") + "," + // guild name
                   "" + "," + // Not sure
                   this.Class.ClassName + "," +
                   this.Level + "," +
                   this.MaxHP + "," +
                   this.MaxMP + "," +
                   this.MaxStats.SP + "," +
                   this.CurrentHP + "," +
                   this.CurrentMP + "," +
                   this.CurrentSP + "," +
                   this.MaxStats.Strength + "," +
                   this.MaxStats.Stamina + "," +
                   this.MaxStats.Intelligence + "," +
                   this.MaxStats.Dexterity + "," +
                   this.MaxStats.AC + "," +
                   this.MaxStats.FireResist + "," +
                   this.MaxStats.WaterResist + "," +
                   this.MaxStats.EarthResist + "," +
                   this.MaxStats.AirResist + "," +
                   this.MaxStats.SpiritResist + "," +
                   this.Gold;
        }

        /**
         * AddRegenEvent, adds regen event to eventhandler if needed
         * 
         */
        public void AddRegenEvent(GameWorld world)
        {
            if (this.RegenEventExists) return;

            if ((this.CurrentHP == this.MaxHP) &&
                (this.CurrentMP == this.MaxMP))
            {
                // Already max stats
                return;
            }

            RegenEvent ev = new RegenEvent();
            ev.Ticks += (long)(GameSettings.Default.RegenSpeed * world.TimerFrequency);
            ev.Data = this;

            this.RegenEventExists = true;

            world.EventHandler.AddEvent(ev);
        }

        /**
         * TNLString, returns data for exp bar
         * 
         */
        public string TNLString()
        {
            long percent, tnl, exp;
            if (this.Class.GetLevel(this.Level).Experience == 0)
            {
                percent = 100;
                tnl = exp = this.Experience;
            }
            else
            {
                long prev;
                if (this.Level == 1) prev = 0;
                else prev = this.Class.GetLevel(this.Level - 1).Experience;

                long next = this.Class.GetLevel(this.Level).Experience;
                tnl = next - this.Experience;
                exp = this.Experience;
                percent = (long)(((float)(exp - prev) / (next - prev)) * 100);
            }
            return "TNL" + percent + "," + exp + "," + tnl + "," + this.ExperienceSold;
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
            this.ExperienceSold = this.Experience + this.ExperienceSold;
            if (classid == 1)
            {
                // This is a hack, need a better solution
                this.ExperienceSold = (long)(this.ExperienceSold * (1.0d - GameSettings.Default.ChangeClassExperienceLossPercent));
            }
            this.Experience = (this.Level == 1 ? 0 : this.Class.GetLevel(this.Level - 1).Experience);
            this.ClassID = classid;
            this.Class = world.ClassHandler.GetClass(this.ClassID);
            this.BaseStats.HP = 0;
            this.BaseStats.MP = 0;
            this.BoundID = 2;
            this.BoundMap = world.MapHandler.GetMap(this.BoundID);
            this.BoundX = 51;
            this.BoundY = 194;

            this.AddStats(this.Class.GetLevel(this.Level).BaseStats, world);
            this.AddStats(this.BaseStats, world);

            this.Spellbook.RemoveNonClassSpells(world);

            world.Send(this, this.SNFString());
            world.Send(this, this.TNLString());
            world.Send(this, "$7Changed class to " + this.Class.ClassName + ".");

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
                world.Send(this, "$7You are too low level to use " + item.Name + ".");
                return false;
            }
            if (item.MaxLevel != 0 && this.Level > item.MaxLevel)
            {
                world.Send(this, "$7You are too high level to use " + item.Name + ".");
                return false;
            }
            if ((item.MinExperience != 0) &&
                (this.Experience + this.ExperienceSold < item.MinExperience))
            {
                world.Send(this, string.Format("$7You are too low experienced to use {0}. {1} experience required.", item.Name, item.MinExperience));
                return false;
            }
            if ((item.MaxExperience != 0) &&
                (this.Experience + this.ExperienceSold > item.MaxExperience))
            {
                world.Send(this, string.Format("$7You are too high experienced to use {0}. {1} experience maximum.", item.Name, item.MaxExperience));
                return false;
            }

            if ((item.ClassRestrictions & Convert.ToInt64(Math.Pow(2.0, (double)this.Class.ClassID))) != 0)
            {
                world.Send(this, "$7You are the wrong class to use " + item.Name + ".");
                return false;
            }

            return true;
        }

        /**
         * CHPString, update character string
         * 
         */
        public virtual string CHPString()
        {
            int pose = this.BodyState;
            ItemSlot weapon = this.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
            if (weapon != null)
            {
                pose = weapon.Item.BodyState;
            }

            // CHP
            // 12278,
            // 1, body
            // 0,0,0,0, body rgba
            // 4, pose
            // 16, hair id
            // 60,*,
            // 133,*,
            // 11,146,183,68,150,
            // 3,150,150,150,135,
            // 224,*,
            // 3,60,25,180,150,
            // 0,0,0,0,
            // 0,
            // 2,
            // 320,
            // 0,0,0,0,0

            // MKC5735,12,Road Sign (Quest),,,,52,177,3,100,152,0,0,0,0,3,0,320,0,0
            // MKC5582,2,Snake (Lv3),,,,64,178,3,100,123,0,0,0,0,3,0,320,0,0.

            return "CHP" +
                   this.LoginID + "," +
                   this.CurrentBodyID + "," +
                   this.BodyR + "," + // Body Color R
                   this.BodyG + "," + // Body Color G
                   this.BodyB + "," + // Body Color B
                   this.BodyA + "," + // Body Color A
                   (this.CurrentBodyID >= 100 ? 3 : pose) + "," +
                   (this.CurrentBodyID >= 100 ? "" : this.HairID + ",") +
                   (this.CurrentBodyID >= 100 ? "" : this.Inventory.EquippedDisplay()) + // Note: EquippedDisplay() adds it's own , on end
                   (this.CurrentBodyID >= 100 ? "" : this.HairR + ",") +
                   (this.CurrentBodyID >= 100 ? "" : this.HairG + ",") +
                   (this.CurrentBodyID >= 100 ? "" : this.HairB + ",") +
                   (this.CurrentBodyID >= 100 ? "" : this.HairA + ",") +
                   "0" + "," + // Invis thing
                   (this.CurrentBodyID >= 100 ? "" : this.FaceID + ",") +
                   this.CalculateMoveSpeed() + "," + // Move Speed
                   (this.CurrentBodyID >= 100 ? "" : this.Inventory.MountDisplay()); // Mount
        }

        public void SendCHPString(GameWorld world)
        {
            string chpstring = this.CHPString();
            world.Send(this, chpstring);
            foreach (Player player in this.Map.GetPlayersInRange(this))
            {
                world.Send(player, chpstring);
            }
        }

        public int CalculateMoveSpeed()
        {
            return (int)(this.BaseStats.MoveSpeed * (1 - this.MaxStats.MoveSpeedIncrease));
        }

        /**
         * AddGold, adds amount of gold to player
         * 
         */
        public void AddGold(long amount, GameWorld world)
        {
            this.Gold += amount;

            world.Send(this, this.SNFString());
        }

        /**
         * RemoveGold, removes amount of gold from player
         * 
         */
        public void RemoveGold(long amount, GameWorld world)
        {
            if (amount > this.Gold) return;
            this.Gold -= amount;

            world.Send(this, this.SNFString());
        }

        /**
         * AddStats, add stats to player
         * 
         */
        public void AddStats(AttributeSet stats, GameWorld world)
        {
            this.MaxStats += stats;
            this.MaxStats.HP += (stats.Stamina * GameSettings.Default.StaminaToHP);
            this.MaxStats.MP += (stats.Intelligence * GameSettings.Default.IntelligenceToMP);

            this.CurrentHP = Math.Min(this.CurrentHP, this.MaxHP);
            this.CurrentMP = Math.Min(this.CurrentMP, this.MaxMP);
            this.CurrentSP = Math.Min(this.CurrentSP, this.MaxStats.SP);

            world.Send(this, this.SNFString());
            this.AddRegenEvent(world);
        }

        /**
         * RemoveStats, remove stats from player
         * 
         */
        public void RemoveStats(AttributeSet stats, GameWorld world, bool changeCurrentHPMP = true)
        {
            this.MaxStats -= stats;
            this.MaxStats.HP -= (stats.Stamina * GameSettings.Default.StaminaToHP);
            this.MaxStats.MP -= (stats.Intelligence * GameSettings.Default.IntelligenceToMP);

            if (changeCurrentHPMP)
            {
                this.CurrentHP = Math.Min(this.CurrentHP, this.MaxHP);
                this.CurrentMP = Math.Min(this.CurrentMP, this.MaxMP);
                this.CurrentSP = Math.Min(this.CurrentSP, this.MaxStats.SP);

                world.Send(this, this.SNFString());
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
            double maxac = GameSettings.Default.MaxAC;
            double absorb = (1 - ((double)(character.MaxStats.AC * character.Class.ACMultiplier) / maxac));

            if (world.Random.Next(1, 10001) <= this.MaxStats.MeleeCrit * 10000) damage *= 2;
            damage *= (double)GameSettings.Default.DamageModifier;
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

        /**
         * AddExperience, player gained experience
         * 
         */
        public virtual void AddExperience(long exp, GameWorld world, ExperienceMessage message)
        {
            if (GameSettings.Default.ExperienceCap > 0 &&
                this.Experience + this.ExperienceSold > GameSettings.Default.ExperienceCap)
            {
                if ((this.ToggleSettings & ToggleSetting.Experience) != 0) return;
                world.Send(this, "$7You have reached the experience cap. Gained 0 experience points.");
                return;
            }

            if (!(GameSettings.Default.ExperienceModifierLimit > 0 &&
                this.Experience + this.ExperienceSold > GameSettings.Default.ExperienceModifierLimit))
            {
                // Under the limit gets the full modifier
                exp = (long)(exp * world.ExperienceModifier);
            }
            else
            {
                // over the limit only gets player bonus
                exp = (long)(exp * (world.ExperienceModifier - GameSettings.Default.ExperienceModifier + 1));
            }

            this.Experience += exp;

            if ((this.ToggleSettings & ToggleSetting.Experience) == 0)
            {
                switch (message)
                {
                    case ExperienceMessage.Normal:
                        world.Send(this, "$7You have gained " + exp + " experience points.");
                        break;

                    case ExperienceMessage.TooFarAway:
                        world.Send(this, "$7You were too far away to gain experience.");
                        break;

                    case ExperienceMessage.TooHigh:
                        world.Send(this,
                            "$7You were too experienced, you only gained " + exp + " experience points.");
                        break;

                    case ExperienceMessage.TooLow:
                        world.Send(this, "$7Group members too high to gain any experience.");
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
                world.Send(this, this.TNLString());
                return;
            }

            this.RemoveStats(this.Class.GetLevel(this.Level).BaseStats, world);
            this.Level += levels;
            this.AddStats(this.Class.GetLevel(this.Level).BaseStats, world);
            this.CurrentHP = this.MaxHP;
            this.CurrentMP = this.MaxMP;
            world.Send(this, this.VPUString());
            if (levels == 1) world.Send(this, "$7You have gained a level.");
            else world.Send(this, "$7You have gained " + levels + " levels.");

            List<Player> range = this.Map.GetPlayersInRange(this);
            string packet = "BT" + this.LoginID + ",60,Level Up!," + this.Name;
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

            world.Send(this, this.SNFString());
            world.Send(this, this.TNLString());
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
                packet = "BT" + this.LoginID + ",21,," + character.Name;
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
                    packet = "BT" + this.LoginID + ",20,," + character.Name;
                    world.Send(this, packet);
                    foreach (Player p in range)
                    {
                        world.Send(p, packet);
                    }
                    return;
                }

                // pvp 1/3 damage
                if (character is Player) damage /= 3;
                packet = "BT" + this.LoginID + ",1," + (-damage) + "," + character.Name + "\x1";
            }
            else
            {
                packet = "BT" + this.LoginID + ",7,+" + (-damage) + "," + character.Name + "\x1";
            }

            this.CurrentHP -= damage;

            if (this.CurrentHP <= 0)
            {
                this.CurrentHP = (long)(this.MaxHP * 0.5);
                this.CurrentMP = (long)(this.MaxMP * 0.1);

                world.SendToMap(this.Map, "$7" + this.Name + " was slain by " + character.Name + ".");

                this.WarpTo(world, this.BoundMap, this.BoundX, this.BoundY);
                this.AddRegenEvent(world);
                world.Send(this, this.VPUString());
                world.Send(this, this.SNFString());

                // Remove all buffs on death
                List<Buff> removebuff = new List<Buff>();
                foreach (Buff b in this.Buffs)
                {
                    if (!b.ItemBuff) removebuff.Add(b);
                }

                foreach (Buff b in removebuff)
                {
                    this.RemoveBuff(b, world, false);
                }

                this.SendBuffBar(world);
            }
            else
            {
                packet += this.VPUString();
                this.AddRegenEvent(world);
            }

            world.Send(this, this.SNFString());
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
         * VPUString, returns regen event string
         */
        public string VPUString()
        {
            return "VPU" + this.LoginID + "," +
                   (int)(((float)this.CurrentHP / this.MaxHP) * 100) + "," +
                   (int)(((float)this.CurrentMP / this.MaxMP) * 100);
        }

        /**
         * AddSaveEvent, Adds save event. Also does ping timeout stuff
         * 
         */
        public void AddSaveEvent(GameWorld world)
        {
            if (this.LastPing == 0) this.LastPing = world.TimeNow;

            if ((world.TimeNow - this.LastPing) >
                ((GameSettings.Default.PlayerSavePeriod * 1.10) * world.TimerFrequency))
            {
                world.LostConnection(this.Sock);
            }
            else
            {
                world.Send(this, "PING");

                PlayerSaveEvent ev = new PlayerSaveEvent();
                ev.Player = this;
                ev.Ticks += (GameSettings.Default.PlayerSavePeriod * world.TimerFrequency);

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
                    world.Send(this, "BT" + this.LoginID + ",50");
                    return;
                }
            }

            foreach (Window window in this.Windows)
            {
                if (window.Type == Window.WindowTypes.Vendor)
                {
                    world.Send(this, "$7You can't cast spells while with a vendor.");
                    return;
                }
            }

            if (!this.Class.CanUse(spell.ClassRestrictions) && !this.HasPrivilege(AccessPrivilege.IgnoreItemRequirements))
            {
                world.Send(this, "$7You are the wrong class to use this spell.");
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
                    this.CurrentMP >= spell.MPStaticCost &&
                    this.CurrentSP >= spell.SPStaticCost)
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

                    this.CurrentHP -= (long)(this.CurrentHP * (spell.HPPercentCost / (decimal)100.0));
                    this.CurrentMP -= (long)(this.CurrentMP * (spell.MPPercentCost / (decimal)100.0));
                    this.CurrentSP -= (long)(this.CurrentSP * (spell.SPPercentCost / (decimal)100.0));

                    if (this.CurrentHP <= 0) this.CurrentHP = 1;
                    if (this.CurrentMP < 0) this.CurrentMP = 0;
                    if (this.CurrentSP < 0) this.CurrentSP = 0;

                    this.Spellbook.SetSlotLastCast(spellslot, now);

                    this.AddRegenEvent(world);

                    if (this.State == States.Ready)
                    {
                        string packet = this.VPUString();

                        world.Send(this, packet);
                        world.Send(this, this.SNFString());
                        foreach (Player player in this.Map.GetPlayersInRange(this))
                        {
                            world.Send(player, packet);
                        }
                    }
                }
                else
                {
                    string packet = "BT" + this.LoginID + ",60,Fizzle";

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

                    world.Send(this, "$7You must wait " + Utils.FormatDuration((long)(wait * 1000)) + " to cast this spell.");
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
        public void AddBuff(Buff buff, GameWorld world, bool refreshbar)
        {
            if (this.State <= States.LoadingGame)
            {
                this.Buffs.Add(buff);

                // Add/remove stats
                this.AddStats(buff.SpellEffect.Stats, world);


                return;
            }

            List<Player> range = this.Map.GetPlayersInRange(this);
            string packet;

            foreach (Buff b in this.Buffs)
            {
                if (buff.SpellEffect.BuffDoesntStackOver.Contains(b.SpellEffect))
                {
                    world.Send(this, "$7The buff had no effect.");
                    return;
                }

                // already have that buff so renew the time cast
                if (!b.ItemBuff && !buff.ItemBuff &&
                    (buff.SpellEffect == b.SpellEffect ||
                    buff.SpellEffect.BuffStacksOver.Contains(b.SpellEffect)))
                {
                    this.RemoveStats(b.SpellEffect.Stats, world);
                    this.AddStats(buff.SpellEffect.Stats, world);

                    world.Send(this, this.WPSString());

                    b.TimeCast = world.TimeNow;
                    b.SpellEffect = buff.SpellEffect;
                    b.Caster = buff.Caster;

                    if (buff.SpellEffect.Animation != 0)
                    {
                        packet = buff.SpellEffect.SPPString(this.LoginID);
                        if (buff.SpellEffect.DoAttackAnimation)
                            packet += "\x1ATT" + this.LoginID; // kinda weird but k

                        world.Send(this, packet);
                        foreach (Player player in range)
                        {
                            world.Send(player, packet);
                        }
                    }
                    if (b.SpellEffect.OffEffectText != "") world.Send(this, "$7" + buff.SpellEffect.OffEffectText);
                    if (buff.SpellEffect.OnEffectText != "") world.Send(this, "$7" + buff.SpellEffect.OnEffectText);

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
                    GameSettings.Default.SpellEffectPeriod * world.TimerFrequency)
                {
                    BuffTickEvent ev = new BuffTickEvent();
                    ev.Data = buff;
                    ev.Player = this;
                    ev.Ticks += (long)(GameSettings.Default.SpellEffectPeriod * world.TimerFrequency);

                    world.EventHandler.AddEvent(ev);
                }
            }

            this.Buffs.Add(buff);

            // Add/remove stats
            this.AddStats(buff.SpellEffect.Stats, world);

            try
            {
                buff.SpellEffect?.Script?.Object.OnBuffAdded(buff, world);
            }
            catch (Exception e) { }

            if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Tick)
            {
                buff.SpellEffect.CastFormulaSpell(buff.Caster, buff.Target, world);
            }

            packet = this.VPUString();

            if (buff.SpellEffect.Stats.Haste != Decimal.Zero)
            {
                world.Send(this, this.WPSString());
            }

            // for illusions
            if (buff.SpellEffect.BodyID != 0)
            {
                this.CurrentBodyID = buff.SpellEffect.BodyID;
                packet += "\x1" + this.CHPString();
            }

            this.AddRegenEvent(world);

            if (buff.SpellEffect.Animation != 0)
                packet += "\x1" + buff.SpellEffect.SPPString(this.LoginID);
            if (buff.SpellEffect.DoAttackAnimation) packet += "\x1ATT" + this.LoginID; // kinda weird but k

            if (buff.SpellEffect.OnEffectText != "") world.Send(this, "$7" + buff.SpellEffect.OnEffectText);
            world.Send(this, this.SNFString());
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
        public void RemoveBuff(Buff buff, GameWorld world, bool refreshbar)
        {
            this.Buffs.Remove(buff);

            if (buff.BuffExpireEvent != null)
            {
                world.EventHandler.RemoveEvent(buff.BuffExpireEvent);
                buff.BuffExpireEvent = null;
            }

            // Add/remove stats
            this.RemoveStats(buff.SpellEffect.Stats, world);

            try
            {
                buff.SpellEffect?.Script?.Object.OnBuffRemoved(buff, world);
            }
            catch (Exception e) { }

            string packet = this.VPUString();

            if (buff.SpellEffect.Stats.Haste != Decimal.Zero)
            {
                world.Send(this, this.WPSString());
            }

            // for illusions
            if (buff.SpellEffect.BodyID != 0)
            {
                this.CurrentBodyID = this.BodyID;
                packet += "\x1" + this.CHPString();
            }

            this.AddRegenEvent(world);

            if (this.State == States.Ready)
            {
                List<Player> range = this.Map.GetPlayersInRange(this);

                if (buff.SpellEffect.OffEffectText != "") world.Send(this, "$7" + buff.SpellEffect.OffEffectText);
                world.Send(this, this.SNFString());
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

                world.Send(this, "BUF" + i + "," + buff.SpellEffect.BuffGraphic + "," + buff.SpellEffect.BuffGraphicFile + "," + buff.SpellEffect.Name);
                i++;
            }

            while (i <= GameSettings.Default.BuffBarVisibleSize)
            {
                world.Send(this, "BUF" + i);
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

        /**
         * WPSSTring, weapon speed string
         * 
         */
        public virtual string WPSString()
        {
            int wps = (int)((decimal)(this.WeaponDelay / 10.0) * (1 - this.MaxStats.Haste) * 1000);

            return "WPS" + wps + ",0,0";
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

            if (this.LastActive + (GameSettings.Default.IdleTimeout * world.TimerFrequency) <= world.TimeNow)
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
    }
}
