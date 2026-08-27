using System.Text;

namespace Goose
{
    /**
     * Class, holds class information
     * 
     */
    public class Class 
    {
        private Dictionary<int, ClassLevel> levels = new();

        public int ClassID { get; set; }
        public string ClassName { get; set; } = null!;
        public decimal ACMultiplier { get; set; }
        public long VitaCost { get; set; }
        public long ManaCost { get; set; }

        public ClassLevel? GetLevel(int level)
        {
            return this.levels.TryGetValue(level, out var classLevel) ? classLevel : null;
        }

        public void AddLevel(ClassLevel c)
        {
            this.levels[c.Level] = c;
        }

        public int MaxLevel { get => this.levels.Count; }

        internal IEnumerable<int> LevelIds => this.levels.Keys;

        /**
         * class_restrictions is an ALLOW list: the bit at index class_id is set for every class
         * that CAN use the thing. 0 is the one special case and means "no restriction at all",
         * so a row nobody has thought about stays usable by everyone.
         *
         * The inverse (a set bit meaning "cannot use") is what this used to be, and it made
         * adding a class a data migration: a new class could use every existing item until every
         * row that meant to exclude it was found and updated. This way round a new class starts
         * with access to the unrestricted rows only.
         *
         */
        public bool CanUse(long classRestrictions)
        {
            return classRestrictions == 0 || (classRestrictions & (1L << this.ClassID)) != 0;
        }
    }
}
