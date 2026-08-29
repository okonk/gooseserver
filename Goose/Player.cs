using System.Text;
using System.Text.Json;
using System.Net.Sockets;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

using Goose.Events;
using Goose.Quests;
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
        Socket sock = null!;
        public Socket Sock
        {
            get => this.sock;
            set { this.sock = value; }
        }
        public StringBuilder Buffer { get; set; } = null!;

        public const int MaxSendBufferSize = 1024 * 1024;

        public List<byte> SendBuffer { get; private set; } = null!;

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
        public string Name { get; set; } = null!;
        /**
         * Name prefix
         */
        public string Title { get; set; } = null!;
        /**
         * Name postfix
         */
        public string Surname { get; set; } = null!;
        /**
         * md5 password hash
         */
        public string PasswordHash { get; set; } = null!;
        /**
         * NOTE: Salt is stored base64 encoded
         */
        public string PasswordSalt { get; set; } = null!;
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
        public Map Map { get; set; } = null!;
        /**
         * Facing direction
         */
        public int Facing { get; set; }
        /**
         * BaseStats, stats loaded from database
         */
        public AttributeSet BaseStats { get; set; } = null!;
        /**
         * Stats from base, items, buffs
         */
        public AttributeSet MaxStats { get; set; } = null!;
        /**
         * Current HP
         */
        long currentHP;
        public long CurrentHP
        {
            get => this.currentHP;
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
            get => this.currentMP;
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
            get => this.currentSP;
            set
            {
                this.currentSP = Math.Min(value, this.MaxSP);
            }
        }

        public long MaxHP
        {
            get => this.TemporaryMaxHP ?? this.MaxStats.HP;
        }

        public long MaxMP
        {
            get => this.TemporaryMaxMP ?? this.MaxStats.MP;
        }

        public long MaxSP
        {
            get => this.TemporaryMaxSP ?? this.MaxStats.SP;
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
        public Map BoundMap { get; set; } = null!;
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
        public Guild? Guild { get; set; }

        /**
         * Class object
         */
        public Class Class { get; set; } = null!;

        /**
         * So regen event doesn't double up
         */
        public bool RegenEventExists { get; set; }

        /**
         * Player's inventory
         *
         */
        public Inventory Inventory { get; set; } = null!;

        public PlayerBank Bank { get; set; } = null!;

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
        public Spellbook Spellbook { get; set; } = null!;

        /**
         * Buffs, holds players buffs
         *
         */
        public List<Buff> Buffs { get; set; } = null!;

        /**
         * Count of buffs with an Invisible/SeeInvisible spell effect. Public set is
         * intentional: scripts may drive invisibility directly, and the counters are
         * the authoritative invis state. AddBuff/RemoveBuff keep them in sync with
         * Buffs for buff-driven changes.
         */
        public int InvisibleBuffCount { get; set; }
        public int SeeInvisibleBuffCount { get; set; }

        public bool IsInvisible { get => this.InvisibleBuffCount > 0; }

        public bool CanSeeInvisible { get => this.SeeInvisibleBuffCount > 0 || this.Access > AccessStatus.Normal; }

        private void AddToInvisCounters(SpellEffect effect)
        {
            if (effect is null) return;
            if (effect.EffectType == SpellEffect.EffectTypes.Invisible) this.InvisibleBuffCount++;
            else if (effect.EffectType == SpellEffect.EffectTypes.SeeInvisible) this.SeeInvisibleBuffCount++;
        }

        private void RemoveFromInvisCounters(SpellEffect effect)
        {
            if (effect is null) return;
            if (effect.EffectType == SpellEffect.EffectTypes.Invisible) this.InvisibleBuffCount--;
            else if (effect.EffectType == SpellEffect.EffectTypes.SeeInvisible) this.SeeInvisibleBuffCount--;
        }

        /**
         * The group the player is in
         *
         * If none is null.
         *
         */
        public Group? Group { get; set; }
        public bool GroupInvitesEnabled { get; set; }

        public int LastWindowID { get; set; }
        public List<Window> Windows { get; set; } = null!;

        public long MovementRecordingStarted { get; set; }
        public long MovementRecordingSteps { get; set; }

        public int NumberOfBankPages { get; set; }

        public decimal AdditionalExperienceModifier { get; set; }

        public bool SPRegenSwitch { get; set; }

        private PriorityQueue<int, int> moveSpeed { get; set; } = null!;

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

        public bool ChatFilterEnabled { get => ((this.ToggleSettings & Player.ToggleSetting.WordFilter) == 0); }
        public bool QuestCreditFilterEnabled { get => ((this.ToggleSettings & Player.ToggleSetting.QuestCredit) != 0); }
        public bool IsGMInvisible { get => (this.HasPrivilege(AccessPrivilege.GMInvisible) && ((this.ToggleSettings & Player.ToggleSetting.GMInvisible) == 0)); }
        public bool IsWhoInvisible { get => (this.HasPrivilege(AccessPrivilege.WhoInvisible) && ((this.ToggleSettings & Player.ToggleSetting.WhoInvisible) == 0)); }
        public bool IsGM { get => (this.Access == AccessStatus.GameMaster && ((this.ToggleSettings & Player.ToggleSetting.GM) == 0)); }
        public bool ShowItemBuffs { get => ((this.ToggleSettings & Player.ToggleSetting.ItemBuffs) == 0); }

        public decimal AetherThreshold { get; set; }

        /// <summary>
        /// Holds all of the Player's pets
        /// </summary>
        public List<Pet> Pets { get; set; } = null!;

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

        internal List<Quest> QuestsCompleted { get; set; } = null!;
        internal List<Quest> QuestsStarted { get; set; } = null!;
        internal List<QuestProgress> QuestProgress { get; set; } = null!;

        public int MacroCheckFailures { get; set; }

        public long LastMacroCheckTime { get; set; }

        public MacroCheckEvent? MacroCheckEvent { get; set; }

        public DateTime? UnbanDate { get; set; }

        /// <summary>Arbitrary per-player storage for scripts. Persisted as JSON in players.player_properties.</summary>
        public PropertiesDictionary Properties { get; set; } = new PropertiesDictionary();

        /// <summary>Split out from LoadFromReader so it can be tested without a reader.</summary>
        internal void LoadPropertiesFromColumn(string json)
        {
            this.Properties = string.IsNullOrWhiteSpace(json)
                ? new PropertiesDictionary()
                : (JsonHelper.Deserialize<PropertiesDictionary>(json) ?? new PropertiesDictionary());
        }

        private object socketLock = new object();


        /**
         * Constructor
         *
         *
         */
        public Player(int unused)
        {
            this.Buffer = new StringBuilder();

            this.LastAttack = 0;
            this.LastPing = 0;
            this.LastWindowID = 1000;
            this.Windows = [];

            this.State = States.NotLoggedIn;

            this.Buffs = [];
            this.Pets = [];

            this.QuestProgress = [];
            this.QuestsCompleted = [];
            this.QuestsStarted = [];

            this.GroupInvitesEnabled = false;

            this.MovementRecordingSteps = 0;

            this.moveSpeed = new PriorityQueue<int, int>();
        }

        public Player()
        {

        }

        public void OnLogin()
        {
            this.Buffer = new StringBuilder();
            this.SendBuffer = new();
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
            this.Buffer.Append(data);
        }

        private Map ResolveBoundMap(GameWorld world)
        {
            Map? map = world.MapHandler.GetMap(this.BoundID);
            if (map is null)
            {
                log.Error("Player {0}: bound map {1} not found; rebinding to starting map", this.Name, this.BoundID);
                this.BoundID = world.Settings.StartingMapID;
                this.BoundX = world.Settings.StartingMapX;
                this.BoundY = world.Settings.StartingMapY;
                map = world.MapHandler.GetMap(this.BoundID);
            }
            // Starting map existence is validated at startup (GameWorld LoadStep chain).
            return map!;
        }

        internal static bool ResolveClassAndLevel(Player player, GameWorld world)
        {
            Class? cls = world.ClassHandler.GetClass(player.ClassID);
            if (cls is null)
            {
                cls = world.ClassHandler.GetFallbackClass();
                if (cls is null)
                {
                    log.Error("Player {0}: no classes loaded; load failed", player.Name);
                    return false;
                }
                log.Error("Player {0}: class {1} not found; using fallback class {2}",
                    player.Name, player.ClassID, cls.ClassID);
                player.ClassID = cls.ClassID;   // keep the persisted row and scripts consistent
            }
            player.Class = cls;

            var levelIds = cls.LevelIds.OrderBy(i => i).ToList();
            if (levelIds.Count == 0)
            {
                log.Error("Player {0}: class {1} has no level rows; load failed", player.Name, player.ClassID);
                return false;
            }
            var atOrBelow = levelIds.Where(i => i <= player.Level).ToList();
            int validLevel = atOrBelow.Count > 0 ? atOrBelow[atOrBelow.Count - 1] : levelIds[0];
            if (validLevel != player.Level)
                log.Error("Player {0}: level {1} missing for class {2}; loading at level {3}",
                    player.Name, player.Level, player.ClassID, validLevel);
            player.Level = validLevel;
            return true;
        }

        /**
         * LoadFromAutoCreate, fills in player info from server defaults
         *
         */
        public bool LoadFromAutoCreate(string name, string password, GameWorld world)
        {
            var (passwordHash, base64Salt) = PasswordHasher.Create(password);

            this.AutoCreatedNotSaved = true;
            this.PlayerID = world.PlayerHandler.CurrentID;
            world.PlayerHandler.CurrentID++;
            this.Name = name;
            this.Title = world.Settings.StartingTitle;
            this.Surname = world.Settings.StartingSurname;
            this.PasswordHash = passwordHash;
            this.PasswordSalt = base64Salt;
            this.Access = AccessStatus.Normal;
            this.MapID = world.Settings.StartingMapID;
            this.MapX = world.Settings.StartingMapX;
            this.MapY = world.Settings.StartingMapY;

            this.Facing = 2;
            this.BoundID = world.Settings.StartingMapID;
            this.BoundX = world.Settings.StartingMapX;
            this.BoundY = world.Settings.StartingMapY;
            this.BoundMap = this.ResolveBoundMap(world);
            this.Gold = world.Settings.StartingGold;
            this.Level = world.Settings.StartingLevel;
            this.ClassID = world.Settings.StartingClassID;
            this.GuildID = world.Settings.StartingGuildID;
            this.Guild = world.GuildHandler.GetGuild(this.GuildID);
            this.Experience = world.Settings.StartingExperience;
            this.ExperienceSold = world.Settings.StartingExperienceSold;
            this.BodyID = world.Settings.StartingBodyID;
            this.BodyR = world.Settings.StartingBodyR;
            this.BodyG = world.Settings.StartingBodyG;
            this.BodyB = world.Settings.StartingBodyB;
            this.BodyA = world.Settings.StartingBodyA;
            this.CurrentBodyID = this.BodyID;
            this.FaceID = world.Settings.StartingFaceID;
            this.HairID = world.Settings.StartingHairID;
            this.HairR = world.Settings.StartingHairR;
            this.HairG = world.Settings.StartingHairG;
            this.HairB = world.Settings.StartingHairB;
            this.HairA = world.Settings.StartingHairA;

            this.BaseStats = new AttributeSet();
            this.BaseStats.HP = world.Settings.StartingHP;
            this.BaseStats.MP = world.Settings.StartingMP;
            this.BaseStats.SP = world.Settings.StartingSP;
            this.BaseStats.AC = world.Settings.StartingAC;
            this.BaseStats.Strength = world.Settings.StartingStrength;
            this.BaseStats.Stamina = world.Settings.StartingStamina;
            this.BaseStats.Intelligence = world.Settings.StartingIntelligence;
            this.BaseStats.Dexterity = world.Settings.StartingDexterity;
            this.BaseStats.FireResist = world.Settings.StartingFireResist;
            this.BaseStats.AirResist = world.Settings.StartingAirResist;
            this.BaseStats.EarthResist = world.Settings.StartingEarthResist;
            this.BaseStats.SpiritResist = world.Settings.StartingSpiritResist;
            this.BaseStats.WaterResist = world.Settings.StartingWaterResist;
            this.BaseStats.MoveSpeed = world.Settings.StartingMoveSpeed;

            this.MaxStats = new AttributeSet();
            this.MaxStats += this.BaseStats;
            this.MaxStats.Haste = world.Settings.BaseHaste;
            this.MaxStats.SpellDamage = world.Settings.BaseSpellDamage;
            this.MaxStats.SpellCrit = world.Settings.BaseSpellCrit;
            this.MaxStats.MeleeDamage = world.Settings.BaseMeleeDamage;
            this.MaxStats.MeleeCrit = world.Settings.BaseMeleeCrit;
            this.MaxStats.DamageReduction = world.Settings.BaseDamageReduction;
            this.MaxStats.HPPercentRegen = world.Settings.BaseHPPercentRegen;
            this.MaxStats.HPStaticRegen = world.Settings.BaseHPStaticRegen;
            this.MaxStats.MPPercentRegen = world.Settings.BaseMPPercentRegen;
            this.MaxStats.MPStaticRegen = world.Settings.BaseMPStaticRegen;
            this.MaxStats.SPPercentRegen = world.Settings.BaseSPPercentRegen;
            this.MaxStats.SPStaticRegen = world.Settings.BaseSPStaticRegen;

            if (!ResolveClassAndLevel(this, world)) return false;
            this.MaxStats += this.Class.GetLevel(this.Level)!.BaseStats;

            this.BodyState = world.Settings.StartingBodyState;

            this.ToggleSettings = (ToggleSetting)world.Settings.DefaultToggleSettings;
            this.AetherThreshold = world.Settings.DefaultAetherThreshold;

            this.NumberOfBankPages = world.Settings.StartingBankPages;
            this.Credits = 0;
            this.TotalAfkTime = 0;
            this.TotalPlayTime = 0;

            this.LastActive = world.TimeNow;
            this.LastPlaytimeUpdate = world.TimeNow;

            this.Inventory = new Inventory(this, world.Settings);
            string[] items = world.Settings.StartingItems.Split(' ');
            if (items.Length > 0)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    try
                    {
                        int templateid = Convert.ToInt32(items[i]);
                        ItemTemplate? template = world.ItemHandler.GetTemplate(templateid);
                        if (template is null)
                        {
                            // log bad id in starting items
                            continue;
                        }
                        Item item = new Item();
                        if (!item.LoadFromTemplate(template)) continue;
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
            this.Spellbook = new Spellbook(this, world.Settings);
            this.Bank = new PlayerBank();

            // kind of a hack to ensure the queue should never be empty
            this.moveSpeed.Enqueue(this.BaseStats.MoveSpeed, this.BaseStats.MoveSpeed);
            this.moveSpeed.Enqueue(this.BaseStats.MoveSpeed, this.BaseStats.MoveSpeed);
            return true;
        }

        /**
         * LoadFromReader, loads player info from a Sq1DataReader
         *
         */
        public bool LoadFromReader(GameWorld world, DbDataReader reader)
        {
            this.Access = (AccessStatus)reader.GetInt32("access_status");

            string databaseHash = reader.GetString("password_hash");
            string base64Salt = reader.GetString("password_salt");

            this.AutoCreatedNotSaved = false;
            this.PlayerID = reader.GetInt32("player_id");
            this.Name = reader.GetString("player_name");
            this.Title = reader.GetString("player_title");
            this.Surname = reader.GetString("player_surname");
            this.PasswordHash = databaseHash;
            this.PasswordSalt = base64Salt;
            this.MapID = reader.GetInt32("map_id");
            this.MapX = reader.GetInt32("map_x");
            this.MapY = reader.GetInt32("map_y");
            this.Facing = reader.GetInt32("player_facing");
            this.BoundID = reader.GetInt32("bound_id");
            this.BoundX = reader.GetInt32("bound_x");
            this.BoundY = reader.GetInt32("bound_y");
            this.BoundMap = this.ResolveBoundMap(world);
            this.Gold = reader.GetInt64("player_gold");
            this.Level = reader.GetInt32("player_level");
            this.ClassID = reader.GetInt32("class_id");
            this.GuildID = reader.GetInt32("guild_id");
            this.Guild = world.GuildHandler.GetGuild(this.GuildID);
            this.Experience = reader.GetInt64("experience");
            this.ExperienceSold = reader.GetInt64("experience_sold");
            this.BodyID = reader.GetInt32("body_id");
            this.BodyR = reader.GetInt32("body_r");
            this.BodyG = reader.GetInt32("body_g");
            this.BodyB = reader.GetInt32("body_b");
            this.BodyA = reader.GetInt32("body_a");
            this.CurrentBodyID = this.BodyID;
            this.FaceID = reader.GetInt32("face_id");
            this.HairID = reader.GetInt32("hair_id");
            this.HairR = reader.GetInt32("hair_r");
            this.HairG = reader.GetInt32("hair_g");
            this.HairB = reader.GetInt32("hair_b");
            this.HairA = reader.GetInt32("hair_a");
            this.LoadPropertiesFromColumn(reader.GetString("player_properties"));

            this.BaseStats = new AttributeSet();
            this.BaseStats.HP = reader.GetInt64("player_hp");
            this.BaseStats.MP = reader.GetInt64("player_mp");
            this.BaseStats.SP = reader.GetInt64("player_sp");
            this.BaseStats.AC = reader.GetInt32("stat_ac");
            this.BaseStats.Strength = reader.GetInt32("stat_str");
            this.BaseStats.Stamina = reader.GetInt32("stat_sta");
            this.BaseStats.Intelligence = reader.GetInt32("stat_int");
            this.BaseStats.Dexterity = reader.GetInt32("stat_dex");
            this.BaseStats.FireResist = reader.GetInt32("res_fire");
            this.BaseStats.AirResist = reader.GetInt32("res_air");
            this.BaseStats.EarthResist = reader.GetInt32("res_earth");
            this.BaseStats.SpiritResist = reader.GetInt32("res_spirit");
            this.BaseStats.WaterResist = reader.GetInt32("res_water");
            this.BaseStats.MoveSpeed = reader.GetInt32("move_speed");

            this.MaxStats = new AttributeSet();
            this.MaxStats += this.BaseStats;
            this.MaxStats.Haste = world.Settings.BaseHaste;
            this.MaxStats.SpellDamage = world.Settings.BaseSpellDamage;
            this.MaxStats.SpellCrit = world.Settings.BaseSpellCrit;
            this.MaxStats.MeleeDamage = world.Settings.BaseMeleeDamage;
            this.MaxStats.MeleeCrit = world.Settings.BaseMeleeCrit;
            this.MaxStats.DamageReduction = world.Settings.BaseDamageReduction;
            this.MaxStats.HPPercentRegen = world.Settings.BaseHPPercentRegen;
            this.MaxStats.HPStaticRegen = world.Settings.BaseHPStaticRegen;
            this.MaxStats.MPPercentRegen = world.Settings.BaseMPPercentRegen;
            this.MaxStats.MPStaticRegen = world.Settings.BaseMPStaticRegen;
            this.MaxStats.SPPercentRegen = world.Settings.BaseSPPercentRegen;
            this.MaxStats.SPStaticRegen = world.Settings.BaseSPStaticRegen;

            if (!ResolveClassAndLevel(this, world)) return false;
            this.MaxStats += this.Class.GetLevel(this.Level)!.BaseStats;

            this.ToggleSettings = (ToggleSetting)reader.GetInt64("toggle_settings");
            this.AetherThreshold = reader.GetDecimal("aether_threshold");

            this.NumberOfBankPages = reader.GetInt32("bank_pages");
            this.Credits = reader.GetInt32("donation_credits");
            this.TotalPlayTime = reader.GetInt64("total_playtime");
            this.TotalAfkTime = reader.GetInt64("total_afktime");

            var unbanDate = reader["unban_date"];
            this.UnbanDate = (unbanDate == DBNull.Value ? null : Convert.ToDateTime(unbanDate));

            this.MacroCheckFailures = reader.GetInt32("macrocheck_failures");

            this.LastActive = world.TimeNow;
            this.LastPlaytimeUpdate = world.TimeNow;

            // kind of a hack to ensure the queue should never be empty
            this.moveSpeed.Enqueue(this.BaseStats.MoveSpeed, this.BaseStats.MoveSpeed);
            this.moveSpeed.Enqueue(this.BaseStats.MoveSpeed, this.BaseStats.MoveSpeed);
            return true;
        }


        public void LoadAdditional(GameWorld world)
        {
            this.Inventory = new Inventory(this, world.Settings);
            this.Inventory.Load(world);
            this.Spellbook = new Spellbook(this, world.Settings);
            this.Spellbook.Load(world);
            this.Bank = new PlayerBank();
            this.Bank.Load(world, this);

            this.BodyState = world.Settings.StartingBodyState;

            this.LoadPets(world);
            this.LoadQuests(world);
        }

        /// <summary>
        /// Loads all pets from database
        /// </summary>
        /// <param name="world"></param>
        public void LoadPets(GameWorld world)
        {
            int playerId = this.PlayerID;
            world.Database.Execute(conn =>
            {
                using var query = conn.CreateCommand();
                query.CommandText = "SELECT * FROM pets WHERE owner_id=" + playerId;
                using var reader = query.ExecuteReader();

                while (reader.Read())
                {
                    Pet? pet = Pet.FromReader(reader, world);
                    if (pet is not null) this.AddPet(pet);
                }
            });
        }

        public void LoadQuests(GameWorld world)
        {
            int playerId = this.PlayerID;
            world.Database.Execute(conn =>
            {
                using var query = conn.CreateCommand();
                query.CommandText = "SELECT serialized_data FROM quest_status WHERE player_id=" + playerId;
                string? raw = Convert.ToString(query.ExecuteScalar());
                QuestStatus? questStatus = null;
                if (!string.IsNullOrEmpty(raw))
                {
                    try
                    {
                        questStatus = JsonHelper.Deserialize<QuestStatus>(raw);
                    }
                    catch (JsonException e)
                    {
                        log.Error("player {0}: quest_status blob is corrupt; starting empty", playerId, e);
                    }
                }

                if (questStatus is null)
                {
                    log.Warn("player {0}: no quest_status row; starting empty", playerId);
                    return;
                }

                foreach (var started in questStatus.Started ?? [])
                {
                    var quest = world.QuestHandler.Get(started);
                    if (quest is not null)
                        this.QuestsStarted.Add(quest);
                }

                foreach (var completed in questStatus.Completed ?? [])
                {
                    var quest = world.QuestHandler.Get(completed);
                    if (quest is not null)
                        this.QuestsCompleted.Add(quest);
                }

                foreach (var progress in questStatus.Progress ?? [])
                {
                    var quest = world.QuestHandler.Get(progress.QuestId);
                    if (quest is null)
                        continue;

                    var requirement = quest.Requirements.FirstOrDefault(r => r.Id == progress.RequirementId);
                    if (requirement is null)
                        continue;

                    this.QuestProgress.Add(new QuestProgress { Requirement = requirement, Value = progress.Progress });
                }
            });
        }

        /**
         * SaveToDatabase, saves player info to database
         *
         */
        public virtual void SaveToDatabase(GameWorld world)
        {
            string playerName = this.Name;
            string playerTitle = this.Title;
            string playerSurname = this.Surname;
            object unbanDate = this.UnbanDate.HasValue ? (object)this.UnbanDate.Value : DBNull.Value;
            // Snapshot on the game thread: the save runs off it, and serializing the live
            // dictionary there would race a concurrent key add from a script.
            string playerProperties = JsonHelper.Serialize(this.Properties.Clone());

            // H8: captured at build time; cleared in the onCommit after COMMIT so a
            // rolled-back save retries INSERT instead of UPDATE-matching zero rows.
            bool isNew = this.AutoCreatedNotSaved;

            // H9: the guild work item fills this cell with the effective guild ID - a
            // new guild's ID is only known mid-transaction, and this.GuildID is set post-commit.
            int guildIdCell = 0;
            bool guildRan = false;
            int playerGuildId = this.GuildID;

            Action<SQLiteConnection> savePlayerRow;

            if (this.AutoCreatedNotSaved)
            {
                string insertQuery = this.BuildInsertQuery();
                savePlayerRow = conn =>
                {
                    using var command = BuildInsertCommand(conn, insertQuery, guildRan ? guildIdCell : playerGuildId, playerName, playerTitle, playerSurname, unbanDate, playerProperties);
                    command.ExecuteNonQuery();
                };
            }
            else
            {
                string updateQuery = this.BuildUpdateQuery();
                savePlayerRow = conn =>
                {
                    using var command = BuildUpdateCommand(conn, updateQuery, guildRan ? guildIdCell : playerGuildId, playerName, playerTitle, playerSurname, unbanDate, playerProperties);
                    command.ExecuteNonQuery();
                };
            }

            // Build every part of the save on the game thread, snapshotting state as we go,
            // then run the whole set inside one transaction. These used to be six or more
            // independent work items each committing on its own, so a crash partway through
            // could persist the players row against a stale inventory - buy an item, crash,
            // and keep both the gold and the item.
            var work = new List<Action<SQLiteConnection>>();
            Action? guildOnCommit = null;

            // First so the guild INSERT assigns the ID the players row binds.
            if (this.Guild is not null && (this.GuildID == 0 || this.Guild.Dirty))
            {
                var (guildSave, commit) = this.Guild.BuildSave(id =>
                {
                    guildIdCell = id;
                    guildRan = true;
                });
                guildOnCommit = commit;
                work.Add(guildSave);
            }

            work.Add(savePlayerRow);
            work.Add(this.Inventory.BuildSave());
            work.Add(this.Spellbook.BuildSave());
            work.Add(this.Bank.BuildSave(this));

            var newPets = new List<Pet>();
            foreach (var pet in this.Pets)
            {
                if (pet.AutoCreatedNotSaved)
                    newPets.Add(pet);
                work.Add(pet.BuildSave());
            }

            work.Add(this.BuildSaveQuests());

            Action? onCommit = null;
            if (isNew || newPets.Count > 0 || guildOnCommit is not null)
            {
                onCommit = () =>
                {
                    if (isNew)
                        this.AutoCreatedNotSaved = false;
                    foreach (var pet in newPets)
                        pet.AutoCreatedNotSaved = false;
                    guildOnCommit?.Invoke();
                };
            }

            world.Database.EnqueueTransaction(conn =>
            {
                foreach (var part in work)
                {
                    part(conn);
                }
            }, onCommit);
        }

        /// <summary>
        /// Builds the INSERT query text for a brand-new player row. Called on the game thread
        /// so every scalar is snapshotted at the same moment as the rest of the save; the
        /// command builder then only binds parameters, guild_id included (a new guild's ID
        /// is only known inside the save transaction).
        /// </summary>
        internal string BuildInsertQuery()
        {
            return "INSERT INTO players (player_id, player_name, player_title, player_surname, " +
                "password_hash, password_salt, access_status, map_id, map_x, map_y, player_facing, " +
                "bound_id, bound_x, bound_y, player_gold, player_level, experience, experience_sold, " +
                "player_hp, player_mp, player_sp, class_id, guild_id, stat_ac, stat_str, stat_sta, " +
                "stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, body_id, body_r, body_g, body_b, body_a, " +
                "face_id, hair_id, hair_r, hair_g, hair_b, hair_a, aether_threshold, toggle_settings, " +
                "donation_credits, total_playtime, total_afktime, move_speed, bank_pages, unban_date, macrocheck_failures, player_properties) VALUES" +
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
                " @guildId, " +
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
                this.MacroCheckFailures + ", " +
                "@playerProperties" +
                ")";
        }

        /// <summary>
        /// Creates the INSERT command from a query string built on the game thread, binding
        /// every parameter. Split out of SaveToDatabase so the persistence tests can execute
        /// the shipped query text and parameter binding without a live GameWorld.
        /// </summary>
        internal SQLiteCommand BuildInsertCommand(SQLiteConnection conn, string query, int guildId, string playerName, string playerTitle, string playerSurname, object unbanDate, string playerProperties)
        {
            var command = conn.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(new SQLiteParameter("@guildId", DbType.Int32) { Value = guildId });
            command.Parameters.Add(new SQLiteParameter("@playerName", DbType.String) { Value = playerName });
            command.Parameters.Add(new SQLiteParameter("@playerTitle", DbType.String) { Value = playerTitle });
            command.Parameters.Add(new SQLiteParameter("@playerSurname", DbType.String) { Value = playerSurname });
            command.Parameters.Add(new SQLiteParameter("@unbanDate", DbType.DateTime2) { Value = unbanDate, IsNullable = true });
            command.Parameters.Add(new SQLiteParameter("@playerProperties", DbType.String) { Value = playerProperties });
            return command;
        }

        /// <summary>
        /// Builds the UPDATE query text for an existing player row. Called on the game thread
        /// so every scalar is snapshotted at the same moment as the rest of the save; the
        /// command builder then only binds parameters, guild_id included (a new guild's ID
        /// is only known inside the save transaction).
        /// </summary>
        internal string BuildUpdateQuery()
        {
            return "UPDATE players SET " +
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
                "guild_id=@guildId, " +
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
                "macrocheck_failures=" + this.MacroCheckFailures + ", " +
                "player_properties=@playerProperties " +
                "WHERE player_id=" + this.PlayerID;
        }

        /// <summary>
        /// Creates the UPDATE command from a query string built on the game thread, binding
        /// every parameter. Split out of SaveToDatabase so the persistence tests can execute
        /// the shipped query text and parameter binding without a live GameWorld.
        /// </summary>
        internal SQLiteCommand BuildUpdateCommand(SQLiteConnection conn, string query, int guildId, string playerName, string playerTitle, string playerSurname, object unbanDate, string playerProperties)
        {
            var command = conn.CreateCommand();
            command.CommandText = query;
            command.Parameters.Add(new SQLiteParameter("@guildId", DbType.Int32) { Value = guildId });
            command.Parameters.Add(new SQLiteParameter("@playerName", DbType.String) { Value = playerName });
            command.Parameters.Add(new SQLiteParameter("@playerTitle", DbType.String) { Value = playerTitle });
            command.Parameters.Add(new SQLiteParameter("@playerSurname", DbType.String) { Value = playerSurname });
            command.Parameters.Add(new SQLiteParameter("@unbanDate", DbType.DateTime2) { Value = unbanDate, IsNullable = true });
            command.Parameters.Add(new SQLiteParameter("@playerProperties", DbType.String) { Value = playerProperties });
            return command;
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

        private Action<SQLiteConnection> BuildSaveQuests()
        {
            var questStatus = new QuestStatus();
            questStatus.Completed = this.QuestsCompleted.Select(q => q.Id).ToArray();
            questStatus.Started = this.QuestsStarted.Select(q => q.Id).ToArray();
            questStatus.Progress = this.QuestProgress.Select(q => new QuestStatus.QuestProgress(q.Requirement.Quest.Id, q.Requirement.Id, q.Value)).ToArray();

            int playerId = this.PlayerID;
            string serialized = JsonHelper.Serialize(questStatus);

            return conn =>
            {
                using var saveQuestStatusCommand = conn.CreateCommand();
                saveQuestStatusCommand.CommandText =
                    @"INSERT INTO quest_status (player_id, serialized_data) VALUES (@player_id, @serialized_data)
                      ON CONFLICT(player_id) DO UPDATE SET serialized_data=@serialized_data WHERE player_id=@player_id;";
                saveQuestStatusCommand.Parameters.Add(new SQLiteParameter("@player_id", DbType.Int32) { Value = playerId });
                saveQuestStatusCommand.Parameters.Add(new SQLiteParameter("@serialized_data", DbType.String) { Value = serialized });
                saveQuestStatusCommand.ExecuteNonQuery();
            };
        }

        /**
         * CanMoveTo, checks if player can move to the specified x,y
         *
         */
        public bool CanMoveTo(int x, int y)
        {
            return this.Map.CanMoveTo(this, x, y);
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
                log.Error(e, "Map OnPlayerMove {0} ({1}) player {2} ({3}) Exception", this.Map.Name, this.Map.ID, this.Name, this.LoginID);
            }

            List<Player> afterRange = this.Map.GetPlayersInRange(this);
            List<NPC> afterNPCRange = this.Map.GetNPCsInRange(this);

            string gmstring = P.AdminMode(this.LoginID);

            string mkc = P.MakeCharacter(this);
            // Send to all people that are in after but aren't in before MKC
            // MKC on client too
            foreach (var player in afterRange.Except<Player>(beforeRange))
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
            foreach (var npc in afterNPCRange.Except<NPC>(beforeNPCRange))
            {
                world.Send(this, P.MakeNPCCharacter(npc));
            }

            if (!IsGMInvisible)
            {
                // Send to everyone MOC
                string packet = P.MoveCharacter(this);
                foreach (var player in afterRange.Union<Player>(beforeRange).Distinct<Player>())
                {
                    world.Send(player, packet);
                }
                // check if aggro any npcs
                foreach (var npc in afterNPCRange.Union<NPC>(beforeNPCRange).Distinct<NPC>())
                {
                    npc.AggroIfInRange(this, world);
                }
            }

            string erc = P.EraseCharacter(this.LoginID);
            // Send to all people that aren't in after but are in before ERC
            // Erase from client too
            foreach (var player in beforeRange.Except<Player>(afterRange))
            {
                if (!IsGMInvisible)
                    world.Send(player, erc);

                world.Send(this, P.EraseCharacter(player.LoginID));
            }

            // Erase old npcs
            // Remove npc aggro towards player
            foreach (var npc in beforeNPCRange.Except<NPC>(afterNPCRange))
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
            foreach (var player in this.Map.GetPlayersInRange(this))
            {
                if (!IsGMInvisible)
                    world.Send(player, erc);

                world.Send(this, P.EraseCharacter(player.LoginID));
            }
            foreach (var npc in this.Map.GetNPCsInRange(this))
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
                foreach (var player in this.Map.GetPlayersInRange(this))
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
                foreach (var npc in this.Map.GetNPCsInRange(this))
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
                this.Map = null!;
                this.MapID = map.ID;
                this.MapX = x;
                this.MapY = y;

                world.Send(this, P.SendMapFlags(map));
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
            // H6: clamp to >= 1, a 0/negative period re-enqueues at now and spins EventHandler.Update
            ev.Ticks += (long)(Math.Max(1m, world.Settings.RegenSpeed) * world.TimerFrequency);
            ev.Data = this;

            this.RegenEventExists = true;

            world.EventHandler.AddEvent(ev);
        }

        /// <summary>Class change with an explicit experience loss. Rebirth passes 0: it is an
        /// exchange (experience becomes spirit), not the 7% penalty quest 60 charges.</summary>
        public void ChangeClass(int classid, int newLevel, GameWorld world, double experienceLossPercent)
        {
            // todo unequip equipment i guess

            Class? dest = world.ClassHandler.GetClass(classid);
            if (this.Class.GetLevel(this.Level) is null || dest is null ||
                dest.GetLevel(newLevel) is null ||
                (newLevel > 1 && this.Class.GetLevel(newLevel - 1) is null))
            {
                log.Error("ChangeClass rejected for {0}: missing level data (class {1} level {2} -> class {3} level {4})",
                    this.Name, this.ClassID, this.Level, classid, newLevel);
                return;
            }

            this.RemoveStats(this.BaseStats, world);

            this.MaxStats -= this.Class.GetLevel(this.Level)!.BaseStats;
            this.Level = newLevel;
            if (classid == 1)
            {
                this.ExperienceSold = this.Experience + this.ExperienceSold;
                // This is a hack, need a better solution
                this.ExperienceSold = (long)(this.ExperienceSold * (1.0d - experienceLossPercent));
            }
            this.Experience = (this.Level == 1 ? 0 : this.Class.GetLevel(this.Level - 1)!.Experience);
            this.ClassID = classid;
            this.Class = world.ClassHandler.GetClass(this.ClassID)!;
            this.BaseStats.HP = 0;
            this.BaseStats.MP = 0;
            this.BoundID = world.Settings.StartingMapID;
            this.BoundMap = this.ResolveBoundMap(world);
            this.BoundX = world.Settings.StartingMapX;
            this.BoundY = world.Settings.StartingMapY;

            this.AddStats(this.Class.GetLevel(this.Level)!.BaseStats, world);
            this.AddStats(this.BaseStats, world);

            this.Spellbook.RemoveNonClassSpells(world);

            world.Send(this, P.StatusInfo(this));
            world.Send(this, P.ExpBar(this));
            world.Send(this, P.ServerMessage("Changed class to " + this.Class.ClassName + "."));

            for (int level = 1; level <= this.Level; level++)
            {
                if (level > this.Class.MaxLevel) break;

                foreach (var spell in this.Class.GetLevel(level)!.Spells)
                {
                    this.LearnSpell(spell.ID, world);
                }
            }
        }

        /**
         * ChangeClass, changes players class
         *
         * Resets level/exp to starting values, applying the settings loss percent.
         */
        public void ChangeClass(int classid, int newLevel, GameWorld world)
        {
            this.ChangeClass(classid, newLevel, world, world.Settings.ChangeClassExperienceLossPercent);
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
                world.Send(this, P.ServerMessage($"You are too low experienced to use {item.Name}. {item.MinExperience} experience required."));
                return false;
            }
            if ((item.MaxExperience != 0) &&
                (this.Experience + this.ExperienceSold > item.MaxExperience))
            {
                world.Send(this, P.ServerMessage($"You are too high experienced to use {item.Name}. {item.MaxExperience} experience maximum."));
                return false;
            }

            if (!this.Class.CanUse(item.ClassRestrictions))
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
            foreach (var player in this.Map.GetPlayersInRange(this))
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
            this.MaxStats.HP += (stats.Stamina * world.Settings.StaminaToHP);
            this.MaxStats.MP += (stats.Intelligence * world.Settings.IntelligenceToMP);

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
            this.MaxStats.HP -= (stats.Stamina * world.Settings.StaminaToHP);
            this.MaxStats.MP -= (stats.Intelligence * world.Settings.IntelligenceToMP);

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
            double maxac = world.Settings.MaxAC;
            double absorb = (1 - ((double)(character.MaxStats.AC * character.Class.ACMultiplier) / maxac));

            if (world.Random.Next(1, 10001) <= this.MaxStats.MeleeCrit * 10000) damage *= 2;
            damage *= (double)world.Settings.DamageModifier;
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
        public virtual long WeaponDamage
        {
            get => this.Inventory.GetWeaponDamage();
            set { }
        }
        /**
         * WeaponDelay
         */
        public virtual int WeaponDelay
        {
            get => this.Inventory.GetWeaponDelay();
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
            this.AddExperience(exp, world, message, applyModifiers: true);
        }

        /// <summary>applyModifiers: false grants exactly `exp`. Purchased experience must not be
        /// re-scaled by world.ExperienceModifier — the two-branch scaling below cannot be
        /// inverted from script, and buyers are exactly the players past ExperienceModifierLimit.
        ///
        /// Not virtual, and Pet does not override it: only Player-side purchases call this.</summary>
        public void AddExperience(long exp, GameWorld world, ExperienceMessage message, bool applyModifiers)
        {
            if (world.Settings.ExperienceCap > 0 &&
                this.Experience + this.ExperienceSold > world.Settings.ExperienceCap)
            {
                if ((this.ToggleSettings & ToggleSetting.Experience) != 0) return;
                world.Send(this, P.ServerMessage("You have reached the experience cap. Gained 0 experience points."));
                return;
            }

            if (applyModifiers)
            {
                if (!(world.Settings.ExperienceModifierLimit > 0 &&
                    this.Experience + this.ExperienceSold > world.Settings.ExperienceModifierLimit))
                {
                    // Under the limit gets the full modifier
                    exp = (long)(exp * (world.ExperienceModifier + AdditionalExperienceModifier));
                }
                else
                {
                    // over the limit only gets player bonus
                    exp = (long)(exp * (world.ExperienceModifier - world.Settings.ExperienceModifier + 1 + AdditionalExperienceModifier));
                }
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

            this.ProcessLevelUp(world);
        }

        /**
         * ProcessLevelUp, applies any pending level-ups from current Experience
         * (stats, spells, vitals, client updates). Safe for offline players.
         */
        public void ProcessLevelUp(GameWorld world)
        {
            long levelup;
            int levels = 0;

            int i = this.Level;
            ClassLevel? level = this.Class.GetLevel(i);
            while (this.Class.GetLevel(i) is not null)
            {
                levelup = level!.Experience;
                if (levelup == 0) break;
                if (this.Experience >= levelup)
                {
                    levels++;
                    if (this.Class.GetLevel(i + 1) is not null && this.Class.GetLevel(i + 1)!.Spells.Count > 0)
                    {
                        foreach (var spell in this.Class.GetLevel(i + 1)!.Spells)
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

            this.RemoveStats(this.Class.GetLevel(this.Level)!.BaseStats, world);
            this.Level += levels;
            this.AddStats(this.Class.GetLevel(this.Level)!.BaseStats, world);
            this.CurrentHP = this.MaxHP;
            this.CurrentMP = this.MaxMP;
            world.Send(this, P.VitalsPercentage(this));
            if (levels == 1) world.Send(this, P.ServerMessage("You have gained a level."));
            else world.Send(this, P.ServerMessage("You have gained " + levels + " levels."));

            string packet = P.BattleTextYellow(this, "Level Up!");
            world.Send(this, packet);
            if (this.Map is not null)
            {
                List<Player> range = this.Map.GetPlayersInRange(this);
                foreach (var player in range)
                {
                    world.Send(player, packet);
                }
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
                foreach (var p in range)
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
                    foreach (var p in range)
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
                List<Buff> removebuff = [];
                foreach (var b in this.Buffs)
                {
                    if (!b.ItemBuff) removebuff.Add(b);
                }

                foreach (var b in removebuff)
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
            foreach (var p in range)
            {
                world.Send(p, packet);
            }

            if (damage > 0)
            {
                foreach (var pet in this.Pets.Where(p => p.Mode == Pet.Modes.Defend && p.Target is null))
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

            // H6: clamp to >= 1s, shared by the ping-timeout check and the save schedule;
            // at 0 it disconnected on every PONG and re-enqueued at now, spinning EventHandler.Update
            long savePeriodTicks = (long)(Math.Max(1, world.Settings.PlayerSavePeriod) * world.TimerFrequency);

            if ((world.TimeNow - this.LastPing) > savePeriodTicks * 1.10)
            {
                world.LostConnection(this.Sock);
            }
            else
            {
                world.Send(this, "PING");

                PlayerSaveEvent ev = new PlayerSaveEvent();
                ev.Player = this;
                ev.Ticks += savePeriodTicks;

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
            Spell? spell = this.Spellbook.GetSlot(spellslot);
            if (spell is null) return;

            foreach (var b in this.Buffs)
            {
                // can't cast when stunned
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                {
                    world.Send(this, P.BattleTextStunned(this));
                    return;
                }
            }

            foreach (var window in this.Windows)
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
                        if (this.Group is not null)
                        {
                            foreach (var p in this.Group.Players)
                            {
                                if (p != this && p.Map == this.Map &&
                                    Math.Abs(p.MapX - this.MapX) < Map.RANGE_X &&
                                    Math.Abs(p.MapY - this.MapY) < Map.RANGE_Y)
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
                        foreach (var player in this.Map.GetPlayersInRange(this))
                        {
                            world.Send(player, packet);
                        }
                    }
                }
                else
                {
                    string packet = P.BattleTextYellow(this, "Fizzle");

                    world.Send(this, packet);
                    foreach (var player in this.Map.GetPlayersInRange(this))
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
            bool wasInvisible = this.IsInvisible;
            bool wasCanSee = this.CanSeeInvisible;

            if (this.State <= States.LoadingGame)
            {
                this.Buffs.Add(buff);
                this.AddToInvisCounters(buff.SpellEffect);

                // Add/remove stats
                this.AddStats(buff.SpellEffect.Stats, world, updateCharacter: false);

                return;
            }

            var range = this.Map.GetPlayersInRange(this);

            foreach (var b in this.Buffs)
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
                    RenewBuff(b, buff, wasInvisible, wasCanSee, range, updateCharacter, world);

                    return;
                }
            }

            var packetBuilder = new StringBuilder();

            if (buff.SpellEffect.Duration > 0)
            {
                // else we don't have the buff. add it
                var ev = new BuffExpireEvent();
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
                if (buff.BuffExpireEvent is not null &&
                    buff.BuffExpireEvent.Ticks - world.TimeNow >
                    world.Settings.SpellEffectPeriod * world.TimerFrequency)
                {
                    var ev = new BuffTickEvent();
                    ev.Data = buff;
                    ev.Player = this;
                    ev.Ticks += (long)(Math.Max(1m, world.Settings.SpellEffectPeriod) * world.TimerFrequency);

                    world.EventHandler.AddEvent(ev);
                }
            }

            this.Buffs.Add(buff);
            this.AddToInvisCounters(buff.SpellEffect);

            // Add/remove stats
            this.AddStats(buff.SpellEffect.Stats, world, updateCharacter: updateCharacter);

            try
            {
                buff.SpellEffect?.Script?.Object.OnBuffAdded(buff, world);
            }
            catch (Exception e)
            {
                log.Error(e, "SpellEffect OnBuffAdded {0} ({1}) target {2} ({3}) Exception",
                    buff.SpellEffect.Name, buff.SpellEffect.ID, buff.Target?.Name, buff.Target?.LoginID);
            }

            if (buff.SpellEffect!.EffectType == SpellEffect.EffectTypes.Tick)
            {
                buff.SpellEffect.CastFormulaSpell(buff.Caster, buff.Target, world);
            }

            packetBuilder.Append(P.VitalsPercentage(this));

            if (buff.SpellEffect.Stats.Haste != Decimal.Zero)
                world.Send(this, P.WeaponSpeed(this));

            bool sendCharacterUpdate = false;

            // for illusions
            if (buff.SpellEffect.BodyID != 0)
            {
                this.CurrentBodyID = buff.SpellEffect.BodyID;
                sendCharacterUpdate = true;
            }

            this.AddRegenEvent(world);

            if (buff.SpellEffect.Animation != 0)
                packetBuilder.Append("\x1").Append(P.SpellPlayer(this.LoginID, buff.SpellEffect.Animation, buff.SpellEffect.AnimationFile));

            if (buff.SpellEffect.DoAttackAnimation) 
                packetBuilder.Append("\x1").Append(P.Attack(this));

            if (buff.SpellEffect.OnEffectText != "")
                world.Send(this, P.ServerMessage(buff.SpellEffect.OnEffectText));

            world.Send(this, P.StatusInfo(this));

            sendCharacterUpdate |= this.FireInvisTransitions(world, wasInvisible, wasCanSee);
            if (sendCharacterUpdate)
                packetBuilder.Append("\x1").Append(P.UpdateCharacter(this));

            if (packetBuilder.Length > 0)
            {
                var packet = packetBuilder.ToString();

                world.Send(this, packet);
                foreach (var player in range)
                {
                    world.Send(player, packet);
                }
            }

            if (refreshbar) this.SendBuffBar(world);
        }

        private void RenewBuff(Buff existingBuff, Buff newBuff, bool wasInvisible, bool wasCanSee, List<Player> range, bool updateCharacter, GameWorld world)
        {
            var packetBuilder = new StringBuilder();

            if (existingBuff.SpellEffect.EffectType != newBuff.SpellEffect.EffectType)
            {
                this.RemoveFromInvisCounters(existingBuff.SpellEffect);
                this.AddToInvisCounters(newBuff.SpellEffect);
            }

            this.RemoveStats(existingBuff.SpellEffect.Stats, world);
            this.AddStats(newBuff.SpellEffect.Stats, world, updateCharacter: updateCharacter);

            world.Send(this, P.WeaponSpeed(this));

            if (existingBuff.SpellEffect.OffEffectText != "") world.Send(this, P.ServerMessage(existingBuff.SpellEffect.OffEffectText));
            if (newBuff.SpellEffect.OnEffectText != "") world.Send(this, P.ServerMessage(newBuff.SpellEffect.OnEffectText));

            existingBuff.TimeCast = world.TimeNow;
            existingBuff.SpellEffect = newBuff.SpellEffect;
            existingBuff.Caster = newBuff.Caster;

            if (newBuff.SpellEffect.Animation != 0)
            {
                packetBuilder.Append(P.SpellPlayer(this.LoginID, newBuff.SpellEffect.Animation, newBuff.SpellEffect.AnimationFile));

                if (newBuff.SpellEffect.DoAttackAnimation)
                    packetBuilder.Append("\x1").Append(P.Attack(this));
            }

            this.SendBuffBar(world);

            bool sendCharacterUpdate = this.FireInvisTransitions(world, wasInvisible, wasCanSee);

            if (sendCharacterUpdate)
            {
                if (packetBuilder.Length > 0)
                    packetBuilder.Append("\x1");
                packetBuilder.Append(P.UpdateCharacter(this));
            }

            if (packetBuilder.Length > 0)
            {
                var packet = packetBuilder.ToString();

                world.Send(this, packet);
                foreach (var player in range)
                {
                    world.Send(player, packet);
                }
            }
        }

        // Returns true when the invisibility state flipped, so the caller folds a
        // CHP into the packet it is already broadcasting (one send per bystander).
        private bool FireInvisTransitions(GameWorld world, bool wasInvisible, bool wasCanSee)
        {
            if (this.State != States.Ready) return false;

            bool isInvisible = this.IsInvisible;
            if (!wasInvisible && isInvisible)
            {
                this.ClearNPCAggroIfUnseen(world);
            }

            bool canSee = this.CanSeeInvisible;
            if (canSee != wasCanSee)
            {
                world.Send(this, P.SeeInvisible(canSee));
            }

            return wasInvisible != isInvisible;
        }

        private void ClearNPCAggroIfUnseen(GameWorld world)
        {
            foreach (var npc in this.Map.GetNPCsInRange(this))
            {
                if (!npc.CanSeeInvisible) npc.RemoveAggro(this);
            }
        }

        public bool IsMounted(GameWorld world)
        {
            // If there is no mount slot, just return false. This is for Aspereta
            if ((int)Inventory.EquipSlots.Mount > world.Settings.EquippedSize)
                return false;

            return this.Inventory.GetEquippedSlot(Inventory.EquipSlots.Mount) is not null;
        }

        public void RemoveBuff(Buff buff, GameWorld world)
        {
            this.RemoveBuff(buff, world, true);
        }

        public void BreakInvisibility(GameWorld world)
        {
            if (!this.IsInvisible) return;

            var toRemove = this.Buffs
                .Where(b => b.SpellEffect.EffectType == SpellEffect.EffectTypes.Invisible)
                .ToList();

            foreach (var buff in toRemove)
            {
                this.RemoveBuff(buff, world);
            }
        }

        /**
         * RemoveBuff, removes buff from buffs list
         *
         */
        public void RemoveBuff(Buff buff, GameWorld world, bool refreshbar, bool updateCharacter = true)
        {
            bool wasInvisible = this.IsInvisible;
            bool wasCanSee = this.CanSeeInvisible;

            var packetBuilder = new StringBuilder();

            // Only decrement when the buff was actually on the list - a double-remove
            // must not drive the counters negative.
            if (this.Buffs.Remove(buff)) this.RemoveFromInvisCounters(buff.SpellEffect);

            if (buff.BuffExpireEvent is not null)
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
            catch (Exception e)
            {
                log.Error(e, "SpellEffect OnBuffRemoved {0} ({1}) target {2} ({3}) Exception",
                    buff.SpellEffect.Name, buff.SpellEffect.ID, buff.Target?.Name, buff.Target?.LoginID);
            }

            packetBuilder.Append(P.VitalsPercentage(this));

            if (buff.SpellEffect!.Stats.Haste != Decimal.Zero)
                world.Send(this, P.WeaponSpeed(this));

            bool sendCharacterUpdate = false;

            // for illusions
            if (buff.SpellEffect.BodyID != 0)
            {
                this.CurrentBodyID = this.BodyID;
                sendCharacterUpdate = true;
            }

            this.AddRegenEvent(world);

            if (this.State == States.Ready)
            {
                var range = this.Map.GetPlayersInRange(this);

                if (buff.SpellEffect.OffEffectText != "") world.Send(this, P.ServerMessage(buff.SpellEffect.OffEffectText));
                world.Send(this, P.StatusInfo(this));

                sendCharacterUpdate |= this.FireInvisTransitions(world, wasInvisible, wasCanSee);
                if (sendCharacterUpdate)
                    packetBuilder.Append("\x1").Append(P.UpdateCharacter(this));

                if (packetBuilder.Length > 0)
                {
                    var packet = packetBuilder.ToString();

                    world.Send(this, packet);
                    foreach (var player in range)
                    {
                        world.Send(player, packet);
                    }
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

            foreach (var buff in this.Buffs)
            {
                if (buff.ItemBuff && !this.ShowItemBuffs) continue;

                world.Send(this, P.BuffBar(buff, i));
                i++;
            }

            while (i <= world.Settings.BuffBarVisibleSize)
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
            foreach (var b in this.Buffs)
            {
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.OnMeleeHit)
                {
                    SpellEffect? spell = b.SpellEffect.OnMeleeHitSpell;
                    if (spell is not null && world.Random.Next(1, 10001) <= b.SpellEffect.OnMeleeHitSpellChance * 100)
                        spell.Cast(this, hitter, world);
                }
            }
        }

        /**
         * OnMeleeAttack, casts melee attack spells when we hit something
         *
         */
        public void OnMeleeAttack(ICharacter hit, GameWorld world)
        {
            foreach (var b in this.Buffs)
            {
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.OnAttack)
                {
                    SpellEffect? spell = b.SpellEffect.OnMeleeAttackSpell;
                    if (spell is not null && world.Random.Next(1, 10001) <= b.SpellEffect.OnMeleeAttackSpellChance * 100)
                        spell.Cast(this, this, world);
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

            if (this.LastActive + (world.Settings.IdleTimeout * world.TimerFrequency) <= world.TimeNow)
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

        public virtual bool Send(string data)
        {
            if (this.sock is null) return true;

            var bytes = Encoding.ASCII.GetBytes(data);

            lock (socketLock)
            {
                // H2: a direct send would reach the client before the buffered tail of an
                // older packet, so hold the new payload in the buffer until it drains.
                if (this.SendBuffer is not null && this.SendBuffer.Count > 0)
                {
                    this.SendBuffer.AddRange(bytes);
                    return this.SendBuffer.Count <= MaxSendBufferSize;
                }

                try
                {
                    var bytesSent = this.sock.Send(bytes);
                    if (bytesSent != bytes.Length)
                    {
                        this.SendBuffer ??= new();
                        this.SendBuffer.AddRange(bytes.AsSpan(bytesSent));
                    }
                }
                // H2: a would-block send throws and drops the whole packet; buffer it all
                catch (SocketException)
                {
                    this.SendBuffer ??= new();
                    this.SendBuffer.AddRange(bytes);
                }
            }

            return this.SendBuffer is null || this.SendBuffer.Count <= MaxSendBufferSize;
        }

        public void Send()
        {
            if (this.sock is null || this.SendBuffer is null) return;

            lock (socketLock)
            {
                var bytesSent = this.sock.Send(this.SendBuffer.ToArray());
                this.SendBuffer.RemoveRange(0, bytesSent);
            }
        }

        public void SetPassword(string password)
        {
            var (passwordHash, base64Salt) = PasswordHasher.Create(password);

            this.PasswordHash = passwordHash;
            this.PasswordSalt = base64Salt;
        }
    }
}
