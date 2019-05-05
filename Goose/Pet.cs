using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

using Goose.Events;

namespace Goose
{
    /// <summary>
    /// Pets yay. Internally a player object since it's easier than adding another character type
    /// </summary>
    public class Pet : Player
    {
        /// <summary>
        /// Maps Login IDs to pet objects
        /// </summary>
        public static Hashtable LoginIDToPet = new Hashtable();
        /// <summary>
        /// Returns the first empty login id for a pet
        /// </summary>
        /// <returns></returns>
        public static int GetLoginID() 
        {
            int startpoint = GameSettings.Default.MaxPlayers + GameSettings.Default.MaxNPCs + 1;
            for (int i = startpoint; ; i++)
            {
                if (Pet.LoginIDToPet[i] == null) return i;
            }
        }

        static int currentdbid = 1;
        /// <summary>
        /// Gets/sets the next available pet database id
        /// </summary>
        public static int CurrentID
        {
            get { return Pet.currentdbid; }
            set { Pet.currentdbid = value; }
        }

        public enum Modes
        {
            Neutral = 0, // Laze around owner
            Follow, // Follow owner
            Defend, // Defend owner
            Attack, // Attack something as directed by owner
        }

        /// <summary>
        /// ID of Pet in the database
        /// </summary>
        public int PetID { get; set; }

        /// <summary>
        /// Owner of the pet
        /// </summary>
        public Player Owner { get; set; }

        /// <summary>
        /// System time of allowed next respawn
        /// </summary>
        public long NextRespawnTime { get; set; }

        /// <summary>
        /// Total time between respawns in seconds
        /// </summary>
        public int RespawnTime { get; set; }

        /// <summary>
        /// Pet's Weapon Damage
        /// </summary>
        public override int WeaponDamage { get; set; }

        /// <summary>
        /// Items to display on humanoid pets
        /// </summary>
        public string EquippedItems { get; set; }

        /// <summary>
        /// Gets a boolean indicating whether the pet is spawned or not
        /// </summary>
        public bool IsAlive { get { return (this.Map != null); } }

        /// <summary>
        /// The current move event
        /// </summary>
        public PetMoveEvent MoveEvent { get; set; }

        /// <summary>
        /// Time between movement events
        /// </summary>
        public decimal MoveSpeed { get; set; }

        /// <summary>
        /// The current attack event
        /// </summary>
        public PetAttackEvent AttackEvent { get; set; }

        /// <summary>
        /// Time between attack events
        /// </summary>
        public decimal AttackSpeed { get; set; }

        /// <summary>
        /// Maximum distance in tiles from pet that can be hit
        /// </summary>
        public int AttackRange { get; set; }

        /// <summary>
        /// Current mode of Pet, defined in enum above
        /// </summary>
        public Modes Mode { get; set; }

        /// <summary>
        /// Current target, used in both attack and defend modes
        /// </summary>
        public ICharacter Target { get; set; }

        /// <summary>
        /// Unused, aggro range
        /// </summary>
        public int AggroRange { get; set; }

        /// <summary>
        /// Does this pet need to be deleted?
        /// </summary>
        public bool Delete { get; set; }

        /// <summary>
        /// Constructs a Pet object with info from another character
        /// </summary>
        /// <param name="character">character to take info from</param>
        public static Pet FromCharacter(NPC character)
        {
            Pet pet = new Pet();

            pet.PetID = Pet.CurrentID;
            Pet.CurrentID++;
            pet.Title = "";
            pet.Surname = "";
            pet.Name = character.Name;
            pet.Level = character.Level;
            pet.Class = character.Class;
            pet.ClassID = character.ClassID;
            pet.BaseStats = character.BaseStats + new AttributeSet();
            pet.BaseStats.HPPercentRegen = 0;
            pet.BaseStats.HPStaticRegen = 0;
            pet.BaseStats.MPPercentRegen = 0;
            pet.BaseStats.MPStaticRegen = 0;
            pet.MaxStats = pet.BaseStats + pet.Class.GetLevel(pet.Level).BaseStats;
            pet.MaxStats.Haste += GameSettings.Default.BaseHaste;
            pet.MaxStats.SpellDamage += GameSettings.Default.BaseSpellDamage;
            pet.MaxStats.SpellCrit += GameSettings.Default.BaseSpellCrit;
            pet.MaxStats.MeleeDamage += GameSettings.Default.BaseMeleeDamage;
            pet.MaxStats.MeleeCrit += GameSettings.Default.BaseMeleeCrit;
            pet.MaxStats.DamageReduction += GameSettings.Default.BaseDamageReduction;
            pet.MaxStats.HPPercentRegen += GameSettings.Default.BaseHPPercentRegen;
            pet.MaxStats.HPStaticRegen += GameSettings.Default.BaseHPStaticRegen;
            pet.MaxStats.MPPercentRegen += GameSettings.Default.BaseMPPercentRegen;
            pet.MaxStats.MPStaticRegen += GameSettings.Default.BaseMPStaticRegen;
            pet.CurrentHP = pet.MaxHP;
            pet.CurrentMP = pet.MaxMP;
            pet.Experience = pet.Class.GetLevel(pet.Level).Experience/2;
            pet.ExperienceSold = 0;
            pet.WeaponDamage = character.WeaponDamage;

            pet.RespawnTime = character.RespawnTime;

            pet.HairID = character.HairID;
            pet.HairR = character.HairR;
            pet.HairG = character.HairG;
            pet.HairB = character.HairB;
            pet.HairA = character.HairA;
            pet.Facing = character.Facing;
            pet.FaceID = character.FaceID;
            pet.BodyID = character.BodyID;
            pet.BodyR = character.BodyR;
            pet.BodyG = character.BodyG;
            pet.BodyB = character.BodyB;
            pet.BodyA = character.BodyA;
            pet.CurrentBodyID = pet.BodyID;
            pet.EquippedItems = character.EquippedItems;
            pet.BodyState = character.BodyState;

            pet.MoveSpeed = character.MoveSpeed;
            pet.AttackRange = character.AttackRange;
            pet.AttackSpeed = character.AttackSpeed;
            pet.AggroRange = character.AggroRange;

            pet.AutoCreatedNotSaved = true;
            pet.Delete = false;

            return pet;
        }

        public static Pet FromReader(SqlDataReader reader, GameWorld world)
        {
            Pet pet = new Pet();

            pet.PetID = Convert.ToInt32(reader["pet_id"]);
            pet.Title = Convert.ToString(reader["pet_title"]);
            pet.Name = Convert.ToString(reader["pet_name"]);
            pet.Surname = Convert.ToString(reader["pet_surname"]);
            pet.Level = Convert.ToInt32(reader["pet_level"]);
            pet.ClassID = Convert.ToInt32(reader["class_id"]);
            pet.Class = world.ClassHandler.GetClass(pet.ClassID);

            pet.Experience = Convert.ToInt64(reader["experience"]);
            pet.ExperienceSold = Convert.ToInt64(reader["experience_sold"]);
            pet.BodyID = Convert.ToInt32(reader["body_id"]);
            pet.BodyR = Convert.ToInt32(reader["body_r"]);
            pet.BodyG = Convert.ToInt32(reader["body_g"]);
            pet.BodyB = Convert.ToInt32(reader["body_b"]);
            pet.BodyA = Convert.ToInt32(reader["body_a"]);
            pet.CurrentBodyID = pet.BodyID;
            pet.FaceID = Convert.ToInt32(reader["face_id"]);
            pet.HairID = Convert.ToInt32(reader["hair_id"]);
            pet.HairR = Convert.ToInt32(reader["hair_r"]);
            pet.HairG = Convert.ToInt32(reader["hair_g"]);
            pet.HairB = Convert.ToInt32(reader["hair_b"]);
            pet.HairA = Convert.ToInt32(reader["hair_a"]);

            pet.BaseStats = new AttributeSet();
            pet.BaseStats.HP = Convert.ToInt32(reader["pet_hp"]);
            pet.BaseStats.MP = Convert.ToInt32(reader["pet_mp"]);
            pet.BaseStats.SP = Convert.ToInt32(reader["pet_sp"]);
            pet.BaseStats.AC = Convert.ToInt32(reader["stat_ac"]);
            pet.BaseStats.Strength = Convert.ToInt32(reader["stat_str"]);
            pet.BaseStats.Stamina = Convert.ToInt32(reader["stat_sta"]);
            pet.BaseStats.Intelligence = Convert.ToInt32(reader["stat_int"]);
            pet.BaseStats.Dexterity = Convert.ToInt32(reader["stat_dex"]);
            pet.BaseStats.FireResist = Convert.ToInt32(reader["res_fire"]);
            pet.BaseStats.AirResist = Convert.ToInt32(reader["res_air"]);
            pet.BaseStats.EarthResist = Convert.ToInt32(reader["res_earth"]);
            pet.BaseStats.SpiritResist = Convert.ToInt32(reader["res_spirit"]);
            pet.BaseStats.WaterResist = Convert.ToInt32(reader["res_water"]);

            pet.MaxStats = new AttributeSet();
            pet.MaxStats += pet.BaseStats;
            pet.MaxStats.Haste += GameSettings.Default.BaseHaste;
            pet.MaxStats.SpellDamage += GameSettings.Default.BaseSpellDamage;
            pet.MaxStats.SpellCrit += GameSettings.Default.BaseSpellCrit;
            pet.MaxStats.MeleeDamage += GameSettings.Default.BaseMeleeDamage;
            pet.MaxStats.MeleeCrit += GameSettings.Default.BaseMeleeCrit;
            pet.MaxStats.DamageReduction += GameSettings.Default.BaseDamageReduction;
            pet.MaxStats.HPPercentRegen += GameSettings.Default.BaseHPPercentRegen;
            pet.MaxStats.HPStaticRegen += GameSettings.Default.BaseHPStaticRegen;
            pet.MaxStats.MPPercentRegen += GameSettings.Default.BaseMPPercentRegen;
            pet.MaxStats.MPStaticRegen += GameSettings.Default.BaseMPStaticRegen;

            pet.Class = world.ClassHandler.GetClass(pet.ClassID);
            pet.MaxStats += pet.Class.GetLevel(pet.Level).BaseStats;

            pet.CurrentHP = pet.MaxHP;
            pet.CurrentMP = pet.MaxMP;

            pet.WeaponDamage = Convert.ToInt32(reader["weapon_damage"]);

            pet.RespawnTime = Convert.ToInt32(reader["respawn_time"]);
            pet.NextRespawnTime = Convert.ToInt64(reader["next_respawn_time"]);

            pet.EquippedItems = Convert.ToString(reader["equipped_items"]);
            pet.BodyState = Convert.ToInt32(reader["body_state"]);

            pet.AggroRange = Convert.ToInt32(reader["aggro_range"]);
            pet.MoveSpeed = Convert.ToDecimal(reader["move_speed"]);
            pet.AttackRange = Convert.ToInt32(reader["attack_range"]);
            pet.AttackSpeed = Convert.ToDecimal(reader["attack_speed"]);

            pet.AutoCreatedNotSaved = false;
            pet.Delete = false;

            if (pet.PetID >= Pet.CurrentID)
            {
                Pet.CurrentID = pet.PetID + 1;
            }

            return pet;
        }

        public Pet()
        {
            this.Buffs = new List<Buff>();
        }


        public override void SaveToDatabase(GameWorld world)
        {
            SqlParameter petNameParam = new SqlParameter("@petName", SqlDbType.VarChar, 50);
            petNameParam.Value = this.Name;
            SqlParameter petTitleParam = new SqlParameter("@petTitle", SqlDbType.VarChar, 50);
            petTitleParam.Value = this.Title;
            SqlParameter petSurnameParam = new SqlParameter("@petSurname", SqlDbType.VarChar, 50);
            petSurnameParam.Value = this.Surname;

            if (this.AutoCreatedNotSaved)
            {
                string query = "INSERT INTO pets (pet_id, pet_name, pet_title, pet_surname, " +
                    "pet_facing, pet_level, experience, experience_sold, " +
                    "pet_hp, pet_mp, pet_sp, class_id, stat_ac, stat_str, stat_sta, " +
                    "stat_dex, stat_int, res_fire, res_water, res_spirit, res_air, res_earth, body_id, body_r, body_g, body_b, body_a, " +
                    "face_id, hair_id, hair_r, hair_g, hair_b, hair_a, respawn_time, aggro_range, attack_speed, attack_range, " +
                    "move_speed, body_state, equipped_items, weapon_damage, hp_percent_regen, hp_static_regen, " +
                    "mp_percent_regen, mp_static_regen, owner_id) VALUES" +
                    "(" + 
                    this.PetID + "," +
                    " @petName, @petTitle, @petSurname, " +
                    this.Facing + ", " +
                    this.Level + ", " +
                    this.Experience + ", " +
                    this.ExperienceSold + ", " +
                    this.BaseStats.HP + ", " +
                    this.BaseStats.MP + ", " +
                    this.BaseStats.SP + ", " +
                    this.Class.ClassID + ", " +
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
                    this.RespawnTime + ", " +
                    this.AggroRange + ", " +
                    this.AttackSpeed + ", " +
                    this.AttackRange + ", " +
                    this.MoveSpeed + ", " +
                    this.BodyState + ", " +
                    "'" + this.EquippedItems + "', " +
                    this.WeaponDamage + ", " +
                    this.BaseStats.HPPercentRegen + ", " +
                    this.BaseStats.HPStaticRegen + ", " +
                    this.BaseStats.MPPercentRegen + ", " +
                    this.BaseStats.MPStaticRegen + ", " +
                    this.Owner.PlayerID +
                    ")";
                SqlCommand command = new SqlCommand(query, world.SqlConnection);
                command.Parameters.Add(petNameParam);
                command.Parameters.Add(petTitleParam);
                command.Parameters.Add(petSurnameParam);
                world.DatabaseWriter.Add(command);
                this.AutoCreatedNotSaved = false;
            }
            else if (this.Delete)
            {
                string query = "DELETE FROM pets WHERE pet_id=" + this.PetID;

                SqlCommand command = new SqlCommand(query, world.SqlConnection);
                world.DatabaseWriter.Add(command);
            }
            else
            {
                string query = "UPDATE pets SET " +
                    "pet_name=@petName, " +
                    "pet_title=@petTitle, " +
                    "pet_surname=@petSurname, " +
                    "pet_facing=" + this.Facing + ", " +
                    "pet_level=" + this.Level + ", " +
                    "experience=" + this.Experience + ", " +
                    "experience_sold=" + this.ExperienceSold + ", " +
                    "pet_hp=" + this.BaseStats.HP + ", " +
                    "pet_mp=" + this.BaseStats.MP + ", " +
                    "pet_sp=" + this.BaseStats.SP + ", " +
                    "class_id=" + this.ClassID + ", " +
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
                    "respawn_time=" + this.RespawnTime + ", " +
                    "aggro_range=" + this.AggroRange + ", " +
                    "attack_speed=" + this.AttackSpeed + ", " +
                    "attack_range=" + this.AttackRange + ", " +
                    "move_speed=" + this.MoveSpeed + ", " +
                    "body_state=" + this.BodyState + ", " +
                    "equipped_items='" + this.EquippedItems + "', " +
                    "weapon_damage=" + this.WeaponDamage + ", " +
                    "hp_percent_regen=" + this.BaseStats.HPPercentRegen + ", " +
                    "hp_static_regen=" + this.BaseStats.HPStaticRegen + ", " +
                    "mp_percent_regen=" + this.BaseStats.MPPercentRegen + ", " +
                    "mp_static_regen=" + this.BaseStats.MPStaticRegen + ", " +
                    "owner_id=" + this.Owner.PlayerID + " " +
                    "WHERE pet_id=" + this.PetID;

                SqlCommand command = new SqlCommand(query, world.SqlConnection);
                command.Parameters.Add(petNameParam);
                command.Parameters.Add(petTitleParam);
                command.Parameters.Add(petSurnameParam);
                world.DatabaseWriter.Add(command);
            }
        }

        /// <summary>
        /// Spawns this Pet
        /// </summary>
        /// <param name="world"></param>
        public void Spawn(GameWorld world)
        {
            this.Destroy(world);

            this.LoginID = Pet.GetLoginID();
            Pet.LoginIDToPet[this.LoginID] = this;

            this.Facing = this.Owner.Facing;

            this.CurrentHP = this.MaxHP;
            this.CurrentMP = this.MaxMP;
            this.CurrentSP = this.MaxStats.SP;
            this.CurrentBodyID = this.BodyID;

            this.LastAttack = world.TimeNow;

            // Fixes regen event
            this.State = States.Ready;

            this.Map = this.Owner.Map;
            this.MapX = this.Owner.MapX;
            this.MapY = this.Owner.MapY;
            this.Map.PlaceCharacter(this);
            this.Map.SetCharacter(this, this.MapX, this.MapY);
            this.Map.AddPlayer(this, world);

            this.Mode = Modes.Neutral;

            string packet = this.MKCString();
            List<Player> range = this.Map.GetPlayersInRange(this);
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }

            List<NPC> npcrange = this.Map.GetNPCsInRange(this);
            foreach (NPC npc in npcrange)
            {
                npc.AggroIfInRange(this, world);
            }

            this.AddMoveEvent(world);
        }

        /// <summary>
        /// Kills this Pet
        /// </summary>
        /// <param name="world"></param>
        public void Destroy(GameWorld world)
        {
            if (!this.IsAlive) return;

            string erase = "ERC" + this.LoginID;
            List<Player> oldrange = this.Map.GetPlayersInRange(this);
            foreach (Player player in oldrange)
            {
                world.Send(player, erase);
            }
            List<NPC> oldnpcrange = this.Map.GetNPCsInRange(this);
            foreach (NPC npc in oldnpcrange)
            {
                npc.RemoveAggro(this);
            }

            List<Buff> removebuff = new List<Buff>();
            foreach (Buff b in this.Buffs)
            {
                if (!b.ItemBuff) removebuff.Add(b);
            }

            foreach (Buff b in removebuff)
            {
                this.RemoveBuff(b, world, false);
            }

            this.Map.RemovePlayer(this, world);

            this.State = States.NotLoggedIn;

            Pet.LoginIDToPet[this.LoginID] = null;
            this.Map.SetCharacter(null, this.MapX, this.MapY);
            this.Map = null;
        }

        public override string MKCString()
        {
            return "MKC" + this.LoginID + ",12," +
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
                            (this.CurrentBodyID >= 100 ? 3 : this.BodyState) + "," +
                            (this.CurrentBodyID >= 100 ? "" : this.HairID + ",") +
                            (this.CurrentBodyID >= 100 ? "" : this.EquippedItems + ",") +
                            (this.CurrentBodyID >= 100 ? "" : this.HairR + "," + HairG + "," + HairB + "," + HairA + ",") +
                            "0" + "," + // Invis thing
                            (this.CurrentBodyID >= 100 ? "" : this.FaceID + ",") +
                            "320," + // Move Speed
                            "0" + "," + // Player Name Color
                            (this.CurrentBodyID >= 100 ? "" : "0,0,0,0"); // Mount
        }

        /**
         * CHPString, update character string
         * 
         */
        public override string CHPString()
        {
            return "CHP" +
                    this.LoginID + "," +
                    this.CurrentBodyID + "," +
                    this.BodyR + "," + // Body Color R
                    this.BodyG + "," + // Body Color G
                    this.BodyB + "," + // Body Color B
                    this.BodyA + "," + // Body Color A
                    (this.CurrentBodyID >= 100 ? 3 : this.BodyState) + "," +
                    (this.CurrentBodyID >= 100 ? "" : this.HairID + ",") +
                    (this.CurrentBodyID >= 100 ? "" : this.EquippedItems + ",") +
                    (this.CurrentBodyID >= 100 ? "" : this.HairR + ",") +
                    (this.CurrentBodyID >= 100 ? "" : this.HairG + ",") +
                    (this.CurrentBodyID >= 100 ? "" : this.HairB + ",") +
                    (this.CurrentBodyID >= 100 ? "" : this.HairA + ",") +
                    (this.CurrentBodyID >= 100 ? "" : "0" + ",") + // Invis thing
                    (this.CurrentBodyID >= 100 ? "" : this.FaceID + ",") +
                    "320," + // Move Speed
                    (this.CurrentBodyID >= 100 ? "" : "0,0,0,0"); // Mount
        }

        public void AddMoveEvent(GameWorld world)
        {
            if (this.MoveEvent == null)
            {
                PetMoveEvent ev = new PetMoveEvent();
                ev.Ticks += (long)(this.MoveSpeed * world.TimerFrequency);
                ev.Player = this;

                world.EventHandler.AddEvent(ev);

                this.MoveEvent = ev;
            }
        }

        /**
         * NextStepTo, returns direction to go to get to x,y
         * 
         * 1,2,3,4 = up,right,down,left
         * 
         */
        public int NextStepTo(int x, int y, GameWorld world)
        {
            int nx, ny;
            int dx, dy;

            dx = x - this.MapX;
            dy = y - this.MapY;

            if (dx == 0 && dy == -1) return 1;
            if (dx == 0 && dy == 1) return 3;
            if (dx == 1 && dy == 0) return 2;
            if (dx == -1 && dy == 0) return 4;

            int shortestpath = Math.Abs(x - (this.MapX)) + Math.Abs(y - (this.MapY));
            int shortest = 0;

            int temp;
            int f1 = 0, f2 = 0, f3 = 0, f4 = 0;
            int d1 = 0, d2 = 0, d3 = 0, d4 = 0;
            nx = this.MapX;
            ny = this.MapY - 1;
            if ((temp = (Math.Abs(x - nx) + Math.Abs(y - ny))) <= shortestpath)
            {
                d1 = 1;
                if (this.CanMoveTo(nx, ny))
                {
                    shortestpath = temp;
                    shortest = 1;
                    f1++;
                }
            }

            nx = this.MapX + 1;
            ny = this.MapY;
            if ((temp = (Math.Abs(x - nx) + Math.Abs(y - ny))) <= shortestpath)
            {
                d2 = 1;
                if (this.CanMoveTo(nx, ny))
                {
                    shortestpath = temp;
                    shortest = 2;
                    f2++;
                }
            }

            nx = this.MapX;
            ny = this.MapY + 1;
            if ((temp = (Math.Abs(x - nx) + Math.Abs(y - ny))) <= shortestpath)
            {
                d3 = 1;
                if (this.CanMoveTo(nx, ny))
                {
                    shortestpath = temp;
                    shortest = 3;
                    f3++;
                }
            }

            nx = this.MapX - 1;
            ny = this.MapY;
            if ((temp = (Math.Abs(x - nx) + Math.Abs(y - ny))) <= shortestpath)
            {
                d4 = 1;
                if (this.CanMoveTo(nx, ny))
                {
                    shortestpath = temp;
                    shortest = 4;
                    f4++;
                }
            }
            nx = this.MapX;
            ny = this.MapY;
            if ((f1 == f2) && (f1 == 1)) { shortest = (Math.Abs(x - nx) > Math.Abs(y - ny)) ? 2 : 1; }
            if ((f1 == f4) && (f1 == 1)) { shortest = (Math.Abs(x - nx) > Math.Abs(y - ny)) ? 4 : 1; }
            if ((f2 == f3) && (f2 == 1)) { shortest = (Math.Abs(x - nx) > Math.Abs(y - ny)) ? 2 : 3; }
            if ((f3 == f4) && (f3 == 1)) { shortest = (Math.Abs(x - nx) > Math.Abs(y - ny)) ? 4 : 3; }

            int rand = 0;

            if (shortestpath == Math.Abs(x - (this.MapX)) + Math.Abs(y - (this.MapY)))
            {
                rand = world.Random.Next(1, 3);
                if (d1 == 1) { shortest = (rand == 1) ? 2 : 4; }
                if (d2 == 1) { shortest = (rand == 1) ? 1 : 3; }
                if (d3 == 1) { shortest = (rand == 1) ? 2 : 4; }
                if (d4 == 1) { shortest = (rand == 1) ? 1 : 3; }
            }
            return shortest;
        }

        /**
         * FaceTo, faces to direction
         * 
         */
        public void FaceTo(int direction, GameWorld world)
        {
            if (this.Facing == direction) return;

            this.Facing = direction;
            string packet = "CHH" + this.LoginID + "," + this.Facing;
            List<Player> range = this.Map.GetPlayersInRange(this);
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }
        }

        public void AddAttackEvent(GameWorld world)
        {
            if (this.AttackEvent == null)
            {
                PetAttackEvent ev = new PetAttackEvent();
                ev.Ticks += (long)(this.AttackSpeed * world.TimerFrequency);
                ev.Player = this;

                world.EventHandler.AddEvent(ev);

                this.AttackEvent = ev;
            }
        }

        /**
         * Player was attacked by character
         * 
         */
        public override void Attacked(ICharacter character, long damage, GameWorld world)
        {
            if (!this.IsAlive) return;

            List<Player> range = this.Map.GetPlayersInRange(this);

            string packet;

            if (damage == 0)
            {
                packet = "BT" + this.LoginID + ",21,," + character.Name;
                foreach (Player p in range)
                {
                    world.Send(p, packet);
                }
                return;
            }

            double dodge = this.MaxStats.Dexterity / 100.0;
            if (dodge > 50) dodge = 50;

            if (world.Random.Next(0, 10001) <= dodge * 100)
            {
                packet = "BT" + this.LoginID + ",20,," + character.Name;
                foreach (Player p in range)
                {
                    world.Send(p, packet);
                }
                return;
            }

            if (damage > 0)
            {
                // pvp 1/2 damage
                if (character is Player) damage /= 2;
                packet = "BT" + this.LoginID + ",1," + (-damage) + "," + character.Name + "\x1";
            }
            else
            {
                packet = "BT" + this.LoginID + ",7,+" + (-damage) + "," + character.Name + "\x1";
            }

            this.CurrentHP -= damage;

            if (this.CurrentHP <= 0)
            {
                this.Destroy(world);
            }
            else
            {
                packet += this.VPUString();
                this.AddRegenEvent(world);
            }

            foreach (Player p in range)
            {
                world.Send(p, packet);
            }
        }

        public override void AddExperience(long exp, GameWorld world, ExperienceMessage message)
        {
            if (GameSettings.Default.ExperienceCap > 0 &&
                this.Experience + this.ExperienceSold > GameSettings.Default.ExperienceCap)
            {
                return;
            }

            // Experience modifier for everyone under the limit
            if (!(GameSettings.Default.ExperienceModifierLimit > 0 &&
                this.Experience + this.ExperienceSold > GameSettings.Default.ExperienceModifierLimit))
            {
                exp = (long)(exp * GameSettings.Default.ExperienceModifier);
            }

            this.Experience += exp;

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
                return;
            }

            this.RemoveStats(this.Class.GetLevel(this.Level).BaseStats, world);
            this.Level += levels;
            this.AddStats(this.Class.GetLevel(this.Level).BaseStats, world);
            this.CurrentHP = this.MaxHP;
            this.CurrentMP = this.MaxMP;

            this.WeaponDamage += (levels * 3) + (world.Random.Next(0, 3) * levels);

            List<Player> range = this.Map.GetPlayersInRange(this);
            string packet = "BT" + this.LoginID + ",60,Level Up!," + this.Name;
            world.Send(this, packet);
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }
        }

        /**
         * WPSSTring, weapon speed string
         * Overrides to fix a bug since pets don't have a weapondelay
         * 
         */
        public override string WPSString()
        {
            int wps = (int)(this.AttackSpeed * 1000);

            return "WPS" + wps;
        }

        public override int WeaponDelay
        {
            get
            {
                return (int)this.AttackSpeed;
            }
            set
            {
                
            }
        }
    }
}
