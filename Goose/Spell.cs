using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * Spell, holds information for a spell
     * 
     */
    public class Spell
    {
        public enum SpellTargets
        {
            Target = 0,
            Self,
            Group
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public SpellTargets Target { get; set; }
        public long ClassRestrictions { get; set; }
        /**
         * Aether in milliseconds
         */
        public long Aether { get; set; }
        public long Graphic { get; set; }

        public int HPStaticCost { get; set; }
        public decimal HPPercentCost { get; set; }
        public int MPStaticCost { get; set; }
        public decimal MPPercentCost { get; set; }
        public int SPStaticCost { get; set; }
        public decimal SPPercentCost { get; set; }

        public int SpellEffectID { get; set; }
        public SpellEffect SpellEffect { get; set; }
    }
}
