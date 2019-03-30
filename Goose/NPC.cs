using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Goose.Events;
using Goose.Quests;
using Goose.Scripting;

namespace Goose
{
    /**
     * NPC
     * 
     */
    public class NPC : ICharacter
    {
        public enum States
        {
            Alive = 0,
            Dead
        }
        public States State { get; set; }

        public class Aggro
        {
            public long Taunt;
            public long Damage;

            public Aggro(long damage, long taunt)
            {
                this.Taunt = taunt;
                this.Damage = damage;
            }

            public static bool operator >(Aggro lhs, Aggro rhs)
            {
                if (lhs.Taunt + lhs.Damage > rhs.Taunt + rhs.Damage) return true;
                return false;
            }
            public static bool operator <(Aggro lhs, Aggro rhs)
            {
                if (lhs.Taunt + lhs.Damage < rhs.Taunt + rhs.Damage) return true;
                return false;
            }
        }

        public bool ShouldRespawn { get; set; }

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
         * Stats from base, items, buffs
         */
        public AttributeSet MaxStats { get; set; }
        /**
         * Current HP
         */
        int currentHP;
        public int CurrentHP
        {
            get { return this.currentHP; }
            set
            {
                this.currentHP = value;
                if (this.currentHP > this.MaxStats.HP) this.currentHP = this.MaxStats.HP;
            }
        }
        /**
         * Current MP
         */
        int currentMP;
        public int CurrentMP
        {
            get { return this.currentMP; }
            set
            {
                currentMP = value;
                if (this.currentMP > this.MaxStats.MP) this.currentMP = this.MaxStats.MP;
            }
        }
        /**
          * Current SP
          */
        int currentSP;
        public int CurrentSP
        {
            get { return this.currentSP; }
            set
            {
                this.currentSP = value;
                if (this.currentSP > this.MaxStats.SP) this.currentSP = this.MaxStats.SP;
            }
        }

        /**
         * spawn map x
         */
        public int SpawnX { get; set; }
        /**
         * spawn map y
         */
        public int SpawnY { get; set; }


        public NPCTemplate.Types NPCType { get; set; }
        /**
         * Template ID
         */
        public int NPCTemplateID { get; set; }
        /**
         * 
         */
        public NPCTemplate NPCTemplate { get; set; }
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
         * Facing direction
         */
        public int Facing { get; set; }
        /**
         * BaseStats, stats loaded from database
         */
        public AttributeSet BaseStats { get; set; }
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
         * Body ID
         */
        public int CurrentBodyID { get; set; }
        /**
         * Body state/pose
         */
        public int BodyState { get; set; }
        /**
         * Experience
         */
        public long Experience { get; set; }
        /**
         * Level
         */
        public int Level { get; set; }
        /**
         * Class ID
         */
        public int ClassID { get; set; }
        /**
         * Class object
         */
        public Class Class { get; set; }
        /**
         * Respawn time in seconds
         */
        public int RespawnTime { get; set; }

        public long LastSpawnTime { get; set; }

        public int LastRespawnTimeSeconds { get; set; }

        /**
         * Aggro range in tiles
         */
        public int AggroRange { get; set; }
        /**
         * Attack range in tiles
         */
        public int AttackRange { get; set; }
        /**
         * Does Root effect this npc
         */
        public bool CanBeRooted { get; set; }
        /**
         * Does Stun effect this npc
         */
        public bool CanBeStunned { get; set; }
        /**
         * Does snare effect this npc
         */
        public bool CanBeSlowed { get; set; }
        /**
         * Is npc invincible
         */
        public bool CanBeKilled { get; set; }
        /**
         * Attack speed in seconds
         */
        public decimal AttackSpeed { get; set; }
        /**
         * Move speed in seconds
         */
        public decimal MoveSpeed { get; set; }
        /**
         * Stationary
         */
        public bool CanMove { get; set; }
        /**
         * Items part of MKC String
         */
        public string EquippedItems { get; set; }
        /**
         * Weapon damage
         */
        public int WeaponDamage { get; set; }
        /**
         * So regen event doesn't double up
         */
        public bool RegenEventExists { get; set; }

        /**
         * AggroTarget, the player the npc is aggro at if any
         */
        public Player AggroTarget { get; set; }
        /**
         * AggroValue, the amount of aggro the npc has against target
         */
        public Aggro AggroValue { get; set; }
        /**
         * Mapping of all aggros
         */
        public Dictionary<Player, Aggro> AggroTargetToValue { get; set; }

        public List<NPCTemplate> Allies { get { return this.NPCTemplate.Allies; } }

        /**
         * So move event doesn't double up
         */
        public Event MoveEvent { get; set; }

        /**
          * So attack event doesn't double up
          */
        public Event AttackEvent { get; set; }

        public List<Buff> Buffs { get; set; }

        public NPCVendorSlot[] VendorItems { get { return this.NPCTemplate.VendorItems; } }

        public NPCTemplate.BehaviourTypes Behaviour { get { return this.NPCTemplate.Behaviour; } }
        public long BehaviourTimeout { get { return this.NPCTemplate.BehaviourTimeout; } }
        public long LastAttackTime { get; set; }

        /// <summary>
        /// Does this NPC deal in credits instead of gold
        /// </summary>
        public bool CreditDealer { get { return this.NPCTemplate.CreditDealer; } }

        internal List<Quest> Quests { get; set; }

        public Script<INPCScript> Script { get { return this.NPCTemplate.Script; } }

        /**
         * MKCString, see Player.MKCString for details
         * 
         * 
         */
        public string MKCString()
        {
            return "MKC" + this.LoginID + "," +
                        (int)this.NPCType + "," +
                        this.Name + "," +
                        this.Title + "," +
                        this.Surname + "," +
                        "" + "," + // Guild name
                        this.MapX + "," +
                        this.MapY + "," +
                        this.Facing + "," +
                        (int)(((float)this.CurrentHP / this.MaxStats.HP) * 100) + "," + // HP %
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
         * CanMoveTo, checks if character can move to the specified x,y
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

        public void OnMoveEvent(GameWorld world)
        {
            try
            {
                this.Script.Object.OnMoveEvent(this, world);
            }
            catch (Exception e)
            {
                // TODO: need a logging system
            }
        }

        public void HandleMoveEvent(GameWorld world)
        {
            foreach (Buff b in this.Buffs)
            {
                // can't move when stunned or rooted
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun ||
                    b.SpellEffect.EffectType == SpellEffect.EffectTypes.Root)
                {
                    this.AddMoveEvent(world);
                    return;
                }
            }

            int direction;
            if (this.AggroTarget == null)
            {
                if (this.MoveSpeed <= Decimal.Zero) return;
                if (!this.CanMove)
                {
                    // Return to spawn point if stationary npc
                    if (this.MapX != this.SpawnX || this.MapY != this.SpawnY)
                    {
                        direction = this.NextStepTo(this.SpawnX, this.SpawnY, world);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    // if outside of spawn area, try to move back to it
                    if (Math.Abs(this.MapX - this.SpawnX) > 10 || Math.Abs(this.MapY - this.SpawnY) > 10)
                    {
                        direction = this.NextStepTo(this.SpawnX, this.SpawnY, world);
                    }
                    else
                    {
                        direction = world.Random.Next(1, 5);
                    }
                }
            }
            else
            {
                // Fix being hit from across map bug.
                // If the player isn't on same map/in screen range then lose aggro
                if (this.AggroTarget.Map == this.Map &&
                    Math.Abs(this.AggroTarget.MapX - this.MapX) < Map.RANGE_X &&
                    Math.Abs(this.AggroTarget.MapY - this.MapY) < Map.RANGE_Y)
                {
                    direction =
                        this.NextStepTo(this.AggroTarget.MapX, this.AggroTarget.MapY, world);
                }
                else
                {
                    this.AggroTargetToValue.Remove(this.AggroTarget);
                    this.AggroTarget = null;

                    List<Player> remove = new List<Player>();

                    Player highest = null;
                    NPC.Aggro aggro = new NPC.Aggro(0, 0);
                    foreach (KeyValuePair<Player, NPC.Aggro> p in this.AggroTargetToValue)
                    {
                        if (p.Value > aggro)
                        {
                            if (p.Key.Map == this.Map &&
                                Math.Abs(p.Key.MapX - this.MapX) < Map.RANGE_X &&
                                Math.Abs(p.Key.MapY - this.MapY) < Map.RANGE_Y)
                            {
                                aggro = p.Value;
                                highest = p.Key;
                            }
                            else
                            {
                                remove.Add(p.Key);
                            }
                        }
                    }

                    foreach (Player p in remove)
                    {
                        this.AggroTargetToValue.Remove(p);
                    }

                    if (highest == null)
                    {
                        this.AddMoveEvent(world);
                        return;
                    }
                    else
                    {
                        this.AggroTarget = highest;
                        this.AggroValue = aggro;

                        direction =
                            this.NextStepTo(this.AggroTarget.MapX, this.AggroTarget.MapY, world);
                    }
                }
            }


            int ox = this.MapX;
            int oy = this.MapY;
            int x = ox;
            int y = oy;

            switch (direction)
            {
                case 1:
                    y--;
                    break;
                case 2:
                    x++;
                    break;
                case 3:
                    y++;
                    break;
                case 4:
                    x--;
                    break;
            }

            if (this.CanMoveTo(x, y))
            {
                this.MoveTo(world, x, y);
                this.Facing = direction;
            }
            else
            {
                this.FaceTo(direction, world);
            }

            this.AddMoveEvent(world);
        }

        /**
         * MoveTo, moves character
         * 
         */
        public void MoveTo(GameWorld world, int x, int y)
        {
            List<Player> beforeRange = this.Map.GetPlayersInRange(this);
            // move off this square so null
            this.Map.SetCharacter(null, this.MapX, this.MapY);
            this.MapX = x;
            this.MapY = y;
            // moveto this square so this
            this.Map.SetCharacter(this, this.MapX, this.MapY);

            List<Player> afterRange = this.Map.GetPlayersInRange(this);

            string mkc = this.MKCString();
            // Send to all people that are in after but aren't in before MKC
            foreach (Player player in afterRange.Except<Player>(beforeRange))
            {
                world.Send(player, mkc);
            }
            // Send to everyone MOC
            string packet = "MOC" + this.LoginID + "," + this.MapX + "," + this.MapY;
            foreach (Player player in afterRange.Union<Player>(beforeRange).Distinct<Player>())
            {
                world.Send(player, packet);

                if (!player.IsGMInvisible)
                    this.AggroIfInRange(player, world);
            }
            string erc = "ERC" + this.LoginID;
            // Send to all people that aren't in after but are in before ERC
            foreach (Player player in beforeRange.Except<Player>(afterRange))
            {
                world.Send(player, erc);
                this.RemoveAggro(player);
            }

            if (this.AggroTarget != null)
            {
                packet = "";

                List<NPC> npcs = this.Map.GetNPCsInRange(this);
                foreach (NPC npc in npcs)
                {
                    if (!this.Allies.Contains(npc.NPCTemplate)) continue;
                    if (npc.AggroTarget != null) continue;

                    if (Math.Abs(npc.MapX - this.MapX) <= npc.AggroRange &&
                        Math.Abs(npc.MapY - this.MapY) <= npc.AggroRange)
                    {
                        npc.AddAggro(this.AggroTarget, 1, world);
                        packet += "EMOT" + npc.LoginID + ",1087,9\x1";

                        npc.AddMoveEvent(world);
                        npc.AddAttackEvent(world);
                    }
                }
                if (packet.Equals("")) return;
                packet = packet.TrimEnd("\x1".ToCharArray());
                foreach (Player p in afterRange.Union<Player>(beforeRange).Distinct<Player>())
                {
                    world.Send(p, packet);
                }
            }
        }

        /**
         * LoadFromTemplate
         * 
         */
        public bool LoadFromTemplate(GameWorld world, int map_id, int map_x, int map_y, NPCTemplate template, bool shouldRespawn)
        {
            this.ShouldRespawn = shouldRespawn;

            this.Map = world.MapHandler.GetMap(map_id);
            if (this.Map == null) return false;
            this.MapID = map_id;
            this.MapX = map_x;
            this.MapY = map_y;
            this.State = States.Dead;

            this.AggroRange = template.AggroRange;
            this.AttackRange = template.AttackRange;
            this.AttackSpeed = template.AttackSpeed;
            this.BaseStats = new AttributeSet();
            this.BaseStats += template.BaseStats;
            this.BodyID = template.BodyID;
            this.BodyA = template.BodyA;
            this.BodyB = template.BodyB;
            this.BodyG = template.BodyG;
            this.BodyR = template.BodyR;
            this.BodyState = template.BodyState;
            this.CanBeKilled = template.CanBeKilled;
            this.CanBeRooted = template.CanBeRooted;
            this.CanBeSlowed = template.CanBeSlowed;
            this.CanBeStunned = template.CanBeStunned;
            this.CanMove = template.CanMove;
            this.ClassID = template.ClassID;
            this.EquippedItems = template.EquippedItems;
            this.Experience = template.Experience;
            this.FaceID = template.FaceID;
            this.Facing = template.Facing;
            this.HairA = template.HairA;
            this.HairB = template.HairB;
            this.HairG = template.HairG;
            this.HairID = template.HairID;
            this.HairR = template.HairR;
            this.Level = template.Level;
            this.MaxStats = new AttributeSet();
            this.MaxStats += this.BaseStats;
            this.MoveSpeed = template.MoveSpeed;
            this.Name = template.Name;
            this.NPCTemplateID = template.NPCTemplateID;
            this.NPCTemplate = template;
            this.NPCType = template.NPCType;
            this.RespawnTime = template.RespawnTime;
            this.LastRespawnTimeSeconds = this.RespawnTime;
            this.Surname = template.Surname;
            this.Title = template.Title;
            this.WeaponDamage = template.WeaponDamage;
            this.Class = world.ClassHandler.GetClass(this.ClassID);
            this.MaxStats += this.Class.GetLevel(this.Level).BaseStats;
            this.Quests = template.Quests;

            this.SpawnX = this.MapX;
            this.SpawnY = this.MapY;

            this.Buffs = new List<Buff>();

            this.Map.AddNPC(this);

            this.Spawn(world);

            return true;
        }

        /**
         * Spawn, spawns npc
         * 
         * adds it to map list, adds a movement event if npc can move
         * 
         */
        public void Spawn(GameWorld world)
        {
            world.NPCHandler.AssignNewId(world, this);

            this.LastSpawnTime = world.TimeNow;

            this.MapX = this.SpawnX;
            this.MapY = this.SpawnY;
            //ICharacter old = this.Map.GetCharacterAt(this.MapX, this.MapY);

            this.Map.PlaceCharacter(this);
            this.State = States.Alive;
            // move to this square so this
            this.Map.SetCharacter(this, this.MapX, this.MapY);

            this.CurrentHP = this.MaxStats.HP;
            this.CurrentMP = this.MaxStats.MP;
            this.CurrentSP = this.MaxStats.SP;

            this.CurrentBodyID = this.BodyID;

            this.Facing = this.NPCTemplate.Facing;

            this.MoveEvent = null;

            this.AggroTarget = null;
            this.AggroValue = new Aggro(0, 0);
            this.AggroTargetToValue = new Dictionary<Player, Aggro>();

            List<Player> range = this.Map.GetPlayersInRange(this);
            string packet = this.MKCString();
            foreach (Player player in range)
            {
                world.Send(player, packet);

                if (!player.IsGMInvisible)
                    this.AggroIfInRange(player, world);
            }

            this.AddMoveEvent(world);
        }

        /**
         * AddMoveEvent, adds move event to event handler
         * 
         */
        public void AddMoveEvent(GameWorld world)
        {
            if (this.MoveEvent == null && this.MoveSpeed > Decimal.Zero)
            {
                decimal snared = 0;

                foreach (Buff buff in this.Buffs)
                {
                    if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Snare)
                    {
                        if (snared == 0) snared = (buff.SpellEffect.SnarePercent / (decimal)100.0);
                        else snared *= (buff.SpellEffect.SnarePercent / (decimal)100.0);
                    }
                }

                NPCMoveEvent ev = new NPCMoveEvent();
                ev.Ticks += (long)((this.MoveSpeed * (1 + snared)) * world.TimerFrequency);
                ev.NPC = this;

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
            int f1=0, f2=0, f3=0, f4=0;
            int d1=0, d2=0, d3=0, d4=0;
            nx = this.MapX;
            ny = this.MapY - 1;
            if ((temp = (Math.Abs(x - nx) + Math.Abs(y - ny))) <= shortestpath)
            {
                d1=1;
                if (this.CanMoveTo(nx,ny)) {
                    shortestpath = temp;
                    shortest = 1;
                    f1++;    
                }
            }

            nx = this.MapX + 1;
            ny = this.MapY;
            if ((temp = (Math.Abs(x - nx) + Math.Abs(y - ny))) <= shortestpath)
            {
                d2=1;
                if (this.CanMoveTo(nx, ny)) {
                    shortestpath = temp;
                    shortest = 2;
                    f2++;
                }
            }

            nx = this.MapX;
            ny = this.MapY + 1;
            if ((temp = (Math.Abs(x - nx) + Math.Abs(y - ny))) <= shortestpath)
            {
                d3=1;
                if (this.CanMoveTo(nx, ny)) {
                    shortestpath = temp;
                    shortest = 3;
                    f3++;
                }
            }

            nx = this.MapX - 1;
            ny = this.MapY;
            if ((temp = (Math.Abs(x - nx) + Math.Abs(y - ny))) <= shortestpath)
            {
                d4=1;
                if (this.CanMoveTo(nx, ny)) {
                    shortestpath = temp;
                    shortest = 4;
                    f4++;
                }
            }
            nx = this.MapX;
            ny = this.MapY;
            if ((f1==f2) && (f1 == 1)) { shortest = (Math.Abs(x-nx)>Math.Abs(y-ny))? 2 : 1; }
            if ((f1==f4) && (f1 == 1)) { shortest = (Math.Abs(x-nx)>Math.Abs(y-ny))? 4 : 1; }
            if ((f2==f3) && (f2 == 1)) { shortest = (Math.Abs(x-nx)>Math.Abs(y-ny))? 2 : 3; }
            if ((f3==f4) && (f3 == 1)) { shortest = (Math.Abs(x-nx)>Math.Abs(y-ny))? 4 : 3; }

            int rand = 0;

            if (shortestpath == Math.Abs(x - (this.MapX)) + Math.Abs(y - (this.MapY)))
            {
                rand = world.Random.Next(1,3);
                if (d1==1)  { shortest = (rand == 1)? 2 : 4; }
                if (d2==1)  { shortest = (rand == 1)? 1 : 3; }
                if (d3==1)  { shortest = (rand == 1)? 2 : 4; }
                if (d4==1)  { shortest = (rand == 1)? 1 : 3; }    
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

        /**
         * AddRegenEvent, adds regen event to eventhandler if needed
         * 
         */
        public void AddRegenEvent(GameWorld world)
        {
            if (this.RegenEventExists) return;
            if (this.State == States.Dead) return;

            if ((this.CurrentHP == this.MaxStats.HP) &&
                (this.CurrentMP == this.MaxStats.MP))
            {
                // Already max stats
                return;
            }

            RegenEvent ev = new RegenEvent();
            ev.Ticks += (long)(GameSettings.Default.RegenSpeed * world.TimerFrequency);
            ev.NPC = this;

            this.RegenEventExists = true;

            world.EventHandler.AddEvent(ev);
        }

        /**
         * 
         * 
         */
        public void AddAggro(Player player, long value, GameWorld world)
        {
            this.AddAggro(player, value, false, world);
        }

        /**
         * AddAggro, adds aggro to player
         * 
         */
        public void AddAggro(Player player, long value, bool wastaunt, GameWorld world)
        {
            this.AddMoveEvent(world);
            this.AddAttackEvent(world);

            // no target so just add to max/mapping
            if (this.AggroTarget == null)
            {
                this.AggroTarget = player;
                if (wastaunt) this.AggroValue = new Aggro(0, value);
                else this.AggroValue = new Aggro(value, 0);

                this.AggroTargetToValue[this.AggroTarget] = this.AggroValue;

                // Set last attack time so it doesn't teleport/whatever right away
                this.LastAttackTime = world.TimeNow;
            }
            // already got max aggro so increase it
            else if (this.AggroTarget == player)
            {
                if (wastaunt) this.AggroValue.Taunt += value;
                else this.AggroValue.Damage += value;

                this.AggroTargetToValue[player] = this.AggroValue;
            }
            else
            {
                Aggro current = new Aggro(0, 0);
                // if player exists increment their aggro and check for max
                if (this.AggroTargetToValue.TryGetValue(player, out current))
                {
                    if (wastaunt) current.Taunt += value;
                    else current.Damage += value;

                    if (current > this.AggroValue)
                    {
                        this.AggroTarget = player;
                        this.AggroValue = current;
                    }
                    else
                    {
                        this.AggroTargetToValue[player] = current;
                    }
                }
                // else player didn't exist so check if they're the new max and add them
                else
                {
                    current = new Aggro(0, 0);

                    if (wastaunt) current.Taunt += value;
                    else current.Damage += value;
                    if (current > this.AggroValue)
                    {
                        this.AggroTarget = player;
                        this.AggroValue = current;

                        this.AggroTargetToValue[player] = this.AggroValue;
                    }
                    else
                    {
                        this.AggroTargetToValue[player] = current;
                    }
                }
            }


        }

        /**
         * RemoveAggro, removes aggro towards player
         * 
         */
        public void RemoveAggro(Player player)
        {
            this.AggroTargetToValue.Remove(player);

            if (this.AggroTarget == player)
            {
                // Set aggro to next highest
                Player newaggro = null;
                Aggro highest = new Aggro(0, 0);

                foreach (KeyValuePair<Player, Aggro> p in this.AggroTargetToValue)
                {
                    if (p.Value > highest)
                    {
                        highest = p.Value;
                        newaggro = p.Key;
                    }
                }

                this.AggroTarget = newaggro;
                this.AggroValue = highest;
            }
        }

        /**
         * AggroIfInRange, sets aggro to player if needed
         * 
         */
        public void AggroIfInRange(Player player, GameWorld world)
        {
            if (this.AggroRange == 0) return;
            if (this.AggroTarget != null) return;

            if (Math.Abs(this.MapX - player.MapX) <= this.AggroRange &&
                Math.Abs(this.MapY - player.MapY) <= this.AggroRange)
            {
                string packet = "EMOT" + this.LoginID + ",1087,9\x1";

                this.AddAggro(player, 1, world);
                
                List<NPC> npcs = this.Map.GetNPCsInRange(this);
                foreach (NPC npc in npcs)
                {
                    if (!this.Allies.Contains(npc.NPCTemplate)) continue;
                    if (npc.AggroTarget != null) continue;

                    if (Math.Abs(npc.MapX - this.MapX) <= npc.AggroRange &&
                        Math.Abs(npc.MapY - this.MapY) <= npc.AggroRange)
                    {
                        npc.AddAggro(player, 1, world);
                        packet += "EMOT" + npc.LoginID + ",1087,9\x1";

                        npc.AddMoveEvent(world);
                        npc.AddAttackEvent(world);
                    }
                }

                List<Player> range = this.Map.GetPlayersInRange(this);
                packet = packet.TrimEnd("\x1".ToCharArray());
                foreach (Player p in range)
                {
                    world.Send(p, packet);
                }

                this.AddMoveEvent(world);
                this.AddAttackEvent(world);
            }
        }

        /**
         * NPC was attacked by character
         * 
         * BattleText ids:
         * 1 - Red text
         * 7 - Green text
         * 10 - STUNNED
         * 11 - ROOTED
         * 20 - DODGE
         * 21 - MISS
         * 60 - Yellow text
         * 
         */
        public void Attacked(ICharacter character, double damage, GameWorld world)
        {
            if (this.State == States.Dead) return;

            if (character is Player)
            {
                List<Player> range = this.Map.GetPlayersInRange(this);

                Player player = (Player)character;
                string packet;

                if (!this.CanBeKilled)
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

                packet = "";
                int dmg = (int)damage;

                if (dmg <= 0)
                {
                    packet = "BT" + this.LoginID + ",21,," + character.Name;
                    foreach (Player p in range)
                    {
                        world.Send(p, packet);
                    }
                    this.AddAggro(player, 1, world);
                    this.AddAttackEvent(world);
                    return;
                }

                this.CurrentHP -= dmg;
                packet += "BT" + this.LoginID + ",1," + (-dmg) + "," + character.Name + "\x1";

                this.AddAggro(player, dmg, world);
                this.AddAttackEvent(world);

                if (this.CurrentHP <= 0)
                {
                    // move off this square so null
                    this.Map.SetCharacter(null, this.MapX, this.MapY);
                    this.State = States.Dead;
                    this.MoveEvent = null;
                    this.CurrentHP = 0;
                    packet = "ERC" + this.LoginID;

                    if (this.ShouldRespawn)
                    {
                        this.AddRespawnEvent(world);
                    }
                    else
                    {
                        this.Map.RemoveNPC(this);
                    }

                    Dictionary<Object, long> damages = new Dictionary<Object, long>();
                    foreach (KeyValuePair<Player, Aggro> p in this.AggroTargetToValue)
                    {
                        if (p.Key.Group != null)
                        {
                            if (damages.ContainsKey(p.Key.Group))
                            {
                                damages[p.Key.Group] = (long)damages[p.Key.Group] + p.Value.Damage;
                            }
                            else
                            {
                                damages[p.Key.Group] = p.Value.Damage;
                            }
                        }
                        else
                        {
                            damages[p.Key] = p.Value.Damage;
                        }
                    }

                    Object highest = null;
                    long highestdamage = 0;
                    foreach (KeyValuePair<Object, long> p in damages)
                    {
                        if (p.Value > highestdamage)
                        {
                            highestdamage = p.Value;
                            highest = p.Key;
                        }
                    }

                    if (highest is Group)
                    {
                        ((Group)highest).GainExperience(this, world);
                    }
                    else
                    {
                        Player.ExperienceMessage expMessage = Player.ExperienceMessage.Normal;
                        long experience = this.Experience;

                        if (this.Level + 9 < ((Player)highest).Level)
                        {
                            experience = (long)(this.Experience * 0.10);
                            expMessage = Player.ExperienceMessage.TooHigh;
                        }

                        if (highest is Pet)
                        {
                            ((Pet)highest).Owner.Killed(this, world);
                            ((Pet)highest).Owner.AddExperience(experience, world, expMessage);
                            ((Player)highest).AddExperience(experience, world, expMessage);
                        }
                        else
                        {
                            ((Player)highest).Killed(this, world);
                            ((Player)highest).AddExperience(experience, world, expMessage);
                        }
                    }

                    List<Buff> removebuff = new List<Buff>();

                    foreach (Buff b in this.Buffs)
                    {
                        removebuff.Add(b);
                    }

                    foreach (Buff b in removebuff)
                    {
                        this.RemoveBuff(b, world);
                    }

                    if (highest is Group) 
                        this.DropItems(((Group)highest).Players[0], world);
                    else
                        this.DropItems(((Player)highest), world);
                }
                else
                {
                    packet += this.VPUString();
                    this.AddRegenEvent(world);
                }

                packet = packet.TrimEnd("\x1".ToCharArray());
                foreach (Player p in range)
                {
                    world.Send(p, packet);
                }
            }
        }

        /**
         * AddRespawnEvent, adds respawn event
         * 
         */
        public void AddRespawnEvent(GameWorld world)
        {
            int respawnTime = this.LastRespawnTimeSeconds;

            if (this.RespawnTime < 120)
            {
                // Some more variance to hopefully force people to move around rather than macro in 1 spot
                long timeNow = world.TimeNow;
                long timeSinceSpawned = (timeNow - this.LastSpawnTime) / world.TimerFrequency;

                respawnTime = Math.Min(Math.Max(this.RespawnTime, (int)((respawnTime * GameSettings.Default.RespawnTimeBackoff) - timeSinceSpawned)), 300);
                this.LastRespawnTimeSeconds = respawnTime;
            }

            respawnTime = world.Random.Next((int)(respawnTime * 0.85), (int)(respawnTime * 1.15) + 1);

            NPCSpawnEvent ev = new NPCSpawnEvent();
            ev.Ticks += (long)(respawnTime * world.TimerFrequency);
            ev.NPC = this;

            world.EventHandler.AddEvent(ev);
        }

        /**
         * VPUString, returns regen event string
         */
        public string VPUString()
        {
            return "VPU" + this.LoginID + "," +
                   (int)(((float)this.CurrentHP / this.MaxStats.HP) * 100) + "," +
                   (int)(((float)this.CurrentMP / this.MaxStats.MP) * 100);
        }

        public void OnAttackEvent(GameWorld world)
        {
            try
            {
                this.Script.Object.OnAttackEvent(this, world);
            }
            catch (Exception e)
            {
                // TODO: need a logging system
            }
        }

        public void HandleAttackEvent(GameWorld world)
        {
            bool rooted = false;

            foreach (Buff b in this.Buffs)
            {
                // can't attack when stunned
                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)
                {
                    this.LastAttackTime = world.TimeNow;
                    this.AddAttackEvent(world);
                    return;
                }

                if (b.SpellEffect.EffectType == SpellEffect.EffectTypes.Root)
                {
                    rooted = true;
                    this.LastAttackTime = world.TimeNow;
                }

            }

            // Can't tele when rooted
            if (!rooted && this.LastAttackTime + this.BehaviourTimeout * world.TimerFrequency <
                world.TimeNow)
            {
                switch (this.Behaviour)
                {
                    case NPCTemplate.BehaviourTypes.TeleportAggro:
                        bool loseaggro = true;
                        this.AggroTarget.WarpTo(world, this.Map,
                            this.MapX, this.MapY, !loseaggro);
                        // reset attack time so doesn't keep teleporting if it can't attack
                        this.LastAttackTime = world.TimeNow;
                        break;
                    case NPCTemplate.BehaviourTypes.TeleportToAggro:
                        // move off this square so null
                        this.Map.SetCharacter(null, this.MapX, this.MapY);
                        this.MapX = this.AggroTarget.MapX;
                        this.MapY = this.AggroTarget.MapY;
                        this.Map.PlaceCharacter(this);
                        this.MoveTo(world, this.MapX, this.MapY);
                        // kinda hackish, moveto will set the aggrotarget char to null so have to reset it
                        //this.Map.SetCharacter(this.AggroTarget, 
                        //    this.AggroTarget.MapX, this.AggroTarget.MapX);
                        // reset attack time so doesn't keep teleporting if it can't attack
                        this.LastAttackTime = world.TimeNow;
                        break;
                }
            }

            Player aggro = this.AggroTarget;
            // Try to attack main aggro first
            if (this.Map == aggro.Map &&
                Math.Abs(this.MapX - aggro.MapX) <= this.AttackRange &&
                Math.Abs(this.MapY - aggro.MapY) <= this.AttackRange)
            {
                this.Attack(aggro, world);
                this.AddAttackEvent(world);

                return;
            }

            foreach (Player player in this.AggroTargetToValue.Keys)
            {
                if (player == aggro) continue;

                if (this.Map == player.Map &&
                    Math.Abs(this.MapX - player.MapX) <= this.AttackRange &&
                    Math.Abs(this.MapY - player.MapY) <= this.AttackRange)
                {
                    this.Attack(player, world);
                    this.AddAttackEvent(world);

                    return;
                }
            }

            this.AddAttackEvent(world);
        }

        /**
         * AddAttackEvent, adds attack event if none exist
         * 
         */
        public void AddAttackEvent(GameWorld world)
        {
            if (this.AttackEvent != null) return;
            if (this.AggroTarget == null) return;
            if (this.AttackSpeed <= Decimal.Zero || this.AttackRange <= 0) return;

            decimal snared = 0;

            foreach (Buff buff in this.Buffs)
            {
                if (buff.SpellEffect.EffectType == SpellEffect.EffectTypes.Snare)
                {
                    if (snared == 0) snared = (buff.SpellEffect.SnarePercent / (decimal)100.0);
                    else snared *= (buff.SpellEffect.SnarePercent / (decimal)100.0);
                }
            }

            NPCAttackEvent ev = new NPCAttackEvent();
            ev.Ticks += (long)((this.AttackSpeed * (1 + snared)) * world.TimerFrequency);
            ev.NPC = this;

            this.AttackEvent = ev;

            world.EventHandler.AddEvent(ev);
        }

        /**
         * Attack, attack character
         * 
         */
        public void Attack(ICharacter character, GameWorld world)
        {
            /*
            double damage = ((double)this.MaxStats.Strength * 0.05 + 5) * 2 *
                ((double)this.WeaponDamage / 10);
            //double absorb = (double)character.MaxStats.AC / 20;
            */

            double damage = this.MaxStats.Strength + 
                            this.WeaponDamage + 
                            this.Level + 
                            (this.Level - character.Level);

            double maxac = GameSettings.Default.MaxAC;
            double absorb = (1 - ((double)(character.MaxStats.AC * character.Class.ACMultiplier) / maxac));

            if (world.Random.Next(1, 10001) <= this.MaxStats.MeleeCrit * 10000) damage *= 2;
            //damage *= (double)GameSettings.Default.DamageModifier; npcs don't get a modifier lol
            damage *= (1 + (double)this.MaxStats.MeleeDamage);
            damage *= (1 - (double)character.MaxStats.DamageReduction);
            damage *= absorb;
            damage -= (double)(character.MaxStats.AC * character.Class.ACMultiplier / 25);

            List<Player> range = this.Map.GetPlayersInRange(this);

            string packet = "ATT" + this.LoginID;
            foreach (Player player in range)
            {
                world.Send(player, packet);
            }

            if (damage > 0)
            {
                character.Attacked(this, damage, world);

                character.OnMeleeHit(this, world);
                this.OnMeleeAttack(character, world);
            }
            else
            {
                packet = "BT" + character.LoginID + ",21,," + this.Name;
                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }
            }

            if (character == this.AggroTarget) this.LastAttackTime = world.TimeNow;
        }

        /**
         * DropItems, drop items if any
         * 
         */
        public void DropItems(Player player, GameWorld world)
        {
            foreach (NPCDropInfo dropinfo in this.NPCTemplate.Drops)
            {
                if (world.Random.Next(1, 1000001) <= 
                    GameSettings.Default.DropRateModifier * dropinfo.DropRate * 10000)
                {
                    ItemSlot drop = new ItemSlot();
                    if (dropinfo.ItemTemplate.ID == GameSettings.Default.GoldItemID)
                    {
                        drop.Item = world.ItemHandler.GetGold();
                    }
                    else
                    {
                        drop.Item = new Item();
                        if ((dropinfo.ItemTemplate.UseType == ItemTemplate.UseTypes.Armor || dropinfo.ItemTemplate.UseType == ItemTemplate.UseTypes.Weapon) &&
                            world.Random.Next(1, 100001) <= 1000)
                        {
                            drop.Item.LoadFromTemplate(dropinfo.ItemTemplate);
                            drop.Item.Name = "Powerful " + dropinfo.ItemTemplate.Name;
                            drop.Item.StatMultiplier = 2;
                            drop.Item.TotalStats = dropinfo.ItemTemplate.BaseStats;
                            drop.Item.TotalStats *= drop.Item.StatMultiplier;
                            drop.Item.TotalStats += drop.Item.BaseStats;
                        }
                        else
                        {
                            drop.Item.LoadFromTemplate(dropinfo.ItemTemplate);
                        }
                        world.ItemHandler.AddItem(drop.Item);
                    }
                    drop.Stack = dropinfo.Stack;

                    ItemTile tile = new ItemTile();
                    tile.ItemSlot = drop;
                    tile.X = this.MapX;
                    tile.Y = this.MapY;
                    if (player is Pet) tile.Owner = ((Pet)player).Owner;
                    else tile.Owner = player;
                    tile.PickupTime = 
                        world.TimeNow + (GameSettings.Default.ItemProtectedTime * world.TimerFrequency);
                    this.Map.PlaceItem(tile);

                    // tile can stack
                    ItemTile maptile = (ItemTile)this.Map.GetTile(tile.X, tile.Y);
                    if (maptile != null && maptile is ItemTile)
                    {
                        if (drop.Item.ItemID != 
                            GameSettings.Default.ItemIDStartpoint + GameSettings.Default.GoldItemID)
                        {
                            drop.Item.Delete = true;
                        }
                        maptile.ItemSlot.Stack += drop.Stack;
                        maptile.Owner = tile.Owner;
                        maptile.PickupTime = tile.PickupTime;

                        world.SendToMap(this.Map, maptile.MOBString());
                    }
                    else
                    {
                        this.Map.AddItem(tile, world);
                    }
                }
            }
        }

        public void AddBuff(Buff buff, GameWorld world)
        {
            List<Player> range = this.Map.GetPlayersInRange(this);
            string packet;

            foreach (Buff b in this.Buffs)
            {
                if (buff.SpellEffect.BuffDoesntStackOver.Contains(b.SpellEffect)) return;

                // already have that buff so renew the time cast
                if (buff.SpellEffect == b.SpellEffect ||
                    buff.SpellEffect.BuffStacksOver.Contains(b.SpellEffect))
                {
                    b.TimeCast = world.TimeNow;
                    b.SpellEffect = buff.SpellEffect;
                    b.Caster = buff.Caster;

                    if (buff.SpellEffect.Animation != 0)
                    {
                        packet = "SPP" + this.LoginID + "," + buff.SpellEffect.Animation;
                        if (buff.SpellEffect.DoAttackAnimation)
                            packet += "\x1ATT" + this.LoginID; // kinda weird but k

                        foreach (Player player in range)
                        {
                            world.Send(player, packet);
                        }
                    }

                    return;
                }
            }

            if (buff.SpellEffect.Duration > 0)
            {
                // else we don't have the buff. add it
                Event ev = new BuffExpireEvent();
                ev.Ticks += buff.SpellEffect.Duration * world.TimerFrequency;
                ev.NPC = this;
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
                    ev.NPC = this;
                    ev.Ticks += (long)(GameSettings.Default.SpellEffectPeriod * world.TimerFrequency);

                    world.EventHandler.AddEvent(ev);
                }
            }

            this.Buffs.Add(buff);

            // Add/remove stats
            this.MaxStats += buff.SpellEffect.Stats;

            this.AddRegenEvent(world);

            packet = this.VPUString();

            if (buff.SpellEffect.Animation != 0)
                packet += "\x1SPP" + this.LoginID + "," + buff.SpellEffect.Animation;
            if (buff.SpellEffect.DoAttackAnimation) packet += "\x1ATT" + this.LoginID; // kinda weird but k

            foreach (Player player in range)
            {
                world.Send(player, packet);
            }
        }

        public void RemoveBuff(Buff buff, GameWorld world)
        {
            this.Buffs.Remove(buff);

            if (buff.BuffExpireEvent != null)
            {
                world.EventHandler.RemoveEvent(buff.BuffExpireEvent);
                buff.BuffExpireEvent = null;
            }

            // Add/remove stats
            this.MaxStats -= buff.SpellEffect.Stats;

            this.AddRegenEvent(world);

            if (this.State == States.Alive)
            {
                List<Player> range = this.Map.GetPlayersInRange(this);
                string packet = this.VPUString();

                foreach (Player player in range)
                {
                    world.Send(player, packet);
                }
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
                        b.SpellEffect.OnMeleeAttackSpell.Cast(this, hit, world);
                }
            }
        }


        /**
         * OpenVendorWindow, opens vendor window with player if needed
         * 
         */
        public void OpenVendorWindow(Player player, GameWorld world)
        {
            foreach (Window window in player.Windows)
            {
                if (window.Type == Window.WindowTypes.Vendor && window.NPC == this)
                {
                    window.Refresh(player, world);
                    return;
                }
            }

            Window w = new Window();
            w.Type = Window.WindowTypes.Vendor;
            w.Title = "Welcome to my shop!";
            w.Buttons = "0,1,0,0,0";
            w.NPC = this;

            player.Windows.Add(w);

            w.Create(player, world);
        }

        /**
         * CloseVendorWindow, removes vendor window from players info
         * 
         */
        public void CloseVendorWindow(Window window, Player player, GameWorld world)
        {
            player.Windows.Remove(window);

        }
    }
}
