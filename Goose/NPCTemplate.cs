using Goose.Quests;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * NPCTemplate, holds all of the information to create an npc
     * 
     */
    public class NPCTemplate
    {
        public enum Types
        {
            Monster = 2,
            Vendor = 10,
            Banker = 11,
            Quest = 12
        }
        public Types NPCType { get; set; }
        /**
         * BehaviourTypes specifies the behaviour of the npc when it hasn't attacked
         * for the specified time
         * 
         */
        public enum BehaviourTypes
        {
            DoNothing = 0,
            TeleportToAggro,
            TeleportAggro,
        }
        public BehaviourTypes Behaviour { get; set; }
        public long BehaviourTimeout { get; set; }

        /**
         * Template ID
         */
        public int NPCTemplateID { get; set; }
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
         * Respawn time in seconds
         */
        public int RespawnTime { get; set; }
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
        public long WeaponDamage { get; set; }
        /**
         * Allies, space delimited list of template ids
         */
        public string AlliesString { get; set; }
        /**
         * Allies, list of npctemplates
         */
        public List<NPCTemplate> Allies { get; set; }

        /**
         * Drops, holds a list of the drops
         * 
         */
        public List<NPCDropInfo> Drops { get; set; }

        /**
         * Holds the items this npc is selling
         */
        public NPCVendorSlot[] VendorItems { get; set; }

        /// <summary>
        /// Does this NPC deal in credits instead of gold?
        /// </summary>
        public bool CreditDealer { get; set; }

        public List<Quest> Quests { get; set; }

        public Script<INPCScript> Script { get; set; }

        public string ScriptParams { get; set; }

        public int ArmorPierce { get; set; }

        public NPCTemplate()
        {
            this.Quests = new List<Quest>();
        }

        /// <summary>Copy constructor for script-generated variants. Lists are new instances so a
        /// variant can attach its own quests/drops without mutating the template it came from.</summary>
        public NPCTemplate(NPCTemplate other) : this()
        {
            this.NPCType = other.NPCType;
            this.Behaviour = other.Behaviour;
            this.BehaviourTimeout = other.BehaviourTimeout;
            this.NPCTemplateID = other.NPCTemplateID;
            this.Name = other.Name;
            this.Title = other.Title;
            this.Surname = other.Surname;
            this.Facing = other.Facing;
            // AttributeSet has no Add method - operator+ (AttributeSet.cs:105) returns a new
            // instance with every field summed, so adding an empty set is the copy idiom.
            // BaseStats can be null on templates built in code, so guard it.
            this.BaseStats = other.BaseStats is null ? new AttributeSet() : other.BaseStats + new AttributeSet();
            this.HairID = other.HairID;   this.HairR = other.HairR;
            this.HairG = other.HairG;     this.HairB = other.HairB;   this.HairA = other.HairA;
            this.BodyR = other.BodyR;     this.BodyG = other.BodyG;
            this.BodyB = other.BodyB;     this.BodyA = other.BodyA;
            this.FaceID = other.FaceID;   this.BodyID = other.BodyID; this.BodyState = other.BodyState;
            this.Experience = other.Experience;
            this.Level = other.Level;
            this.ClassID = other.ClassID;
            this.RespawnTime = other.RespawnTime;
            this.AggroRange = other.AggroRange;
            this.AttackRange = other.AttackRange;
            this.CanBeRooted = other.CanBeRooted;
            this.CanBeStunned = other.CanBeStunned;
            this.CanBeSlowed = other.CanBeSlowed;
            this.CanBeKilled = other.CanBeKilled;
            this.AttackSpeed = other.AttackSpeed;
            this.MoveSpeed = other.MoveSpeed;
            this.CanMove = other.CanMove;
            this.EquippedItems = other.EquippedItems;
            this.WeaponDamage = other.WeaponDamage;
            this.AlliesString = other.AlliesString;
            this.CreditDealer = other.CreditDealer;
            this.Script = other.Script;
            this.ScriptParams = other.ScriptParams;
            this.ArmorPierce = other.ArmorPierce;
            this.VendorItems = other.VendorItems;
            this.Allies = other.Allies is null ? null : new List<NPCTemplate>(other.Allies);
            this.Drops = other.Drops is null ? null : new List<NPCDropInfo>(other.Drops);
            this.Quests = other.Quests is null ? new List<Quest>() : new List<Quest>(other.Quests);
        }
    }
}
