using System.Text;

namespace Goose
{
    /**
     * ClassLevel, holds information about a level for a class
     * 
     */
    public class ClassLevel
    {
        /**
         * Experience of next level
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
         * BaseStats, stats loaded from database
         */
        public AttributeSet BaseStats { get; set; } = null!;

        public List<Spell> Spells { get; set; } = null!;
    }
}
