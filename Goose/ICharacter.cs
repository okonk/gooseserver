using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * ICharacter, interface for characters, NPCs and Players
     * 
     * Provides a common interface for NPCs and Players to implement
     * 
     */
    public interface ICharacter
    {

        int LoginID { get; set; }

        string Name { get; set; }

        /**
         * MKCString, returns the MKC packet string for this character
         */
        string MKCString();
        /**
         * Current map id
         */
        int MapID { get; set; }
        /**
         * Current map x
         */
        int MapX { get; set; }
        /**
         * Current map y
         */
        int MapY { get; set; }
        /**
         * Facing direction
         */
        int Facing { get; set; }
        /**
         * BaseStats, stats loaded from database
         */
        AttributeSet BaseStats { get; set; }
        /**
         * Stats from base, items, buffs
         */
        AttributeSet MaxStats { get; set; }
        /**
         * Current HP
         */
        int CurrentHP { get; set; }
        /**
         * Current MP
         */
        int CurrentMP { get; set; }
        /**
          * Current SP
          */
        int CurrentSP { get; set; }
        /**
         * Level
         */
        int Level { get; set; }
        /**
         * Class ID
         */
        int ClassID { get; set; }
        /**
          * Experience
          */
        long Experience { get; set; }

        bool CanMoveTo(int x, int y);
        void MoveTo(GameWorld world, int x, int y);

        void Attacked(ICharacter character, double damage, GameWorld world);

        Map Map { get; set; }
        string VPUString();

        int WeaponDamage { get; }

        /**
         * Hair style id
         */
        int HairID { get; set; }
        /**
         * Hair colour r
         */
        int HairR { get; set; }
        /**
         * Hair colour g
         */
        int HairG { get; set; }
        /**
         * Hair colour b
         */
        int HairB { get; set; }
        /**
         * Hair colour a
         */
        int HairA { get; set; }
        /**
         * Face id
         */
        int FaceID { get; set; }
        /**
         * Body ID
         */
        int BodyID { get; set; }
        /**
         * Current Body ID
         */
        int CurrentBodyID { get; set; }


        void AddBuff(Buff buff, GameWorld world);
        void RemoveBuff(Buff buff, GameWorld world);

        void OnMeleeHit(ICharacter attacker, GameWorld world);

        Class Class { get; set; }
    }
}
