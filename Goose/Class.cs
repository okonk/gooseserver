using System;
using System.Collections.Generic;
using System.Linq;
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
        public string ClassName { get; set; }
        public decimal ACMultiplier { get; set; }
        public long VitaCost { get; set; }
        public long ManaCost { get; set; }

        public ClassLevel GetLevel(int level)
        {
            return this.levels.TryGetValue(level, out var classLevel) ? classLevel : null;
        }

        public void AddLevel(ClassLevel c)
        {
            this.levels[c.Level] = c;
        }

        public int MaxLevel { get { return this.levels.Count; } }

        public bool CanUse(long classRestrictions)
        {
            return ((classRestrictions & Convert.ToInt64(Math.Pow(2.0, (double)this.ClassID))) == 0);
        }
    }
}
