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
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public SpellTargets Target { get; set; }
        public long ClassRestrictions { get; set; }
        /**
         * Aether in milliseconds
         */
        public long Aether { get; set; }
        public long Graphic { get; set; }
        public int GraphicFile { get; set; }

        public int HPStaticCost { get; set; }
        public decimal HPPercentCost { get; set; }
        public int MPStaticCost { get; set; }
        public decimal MPPercentCost { get; set; }
        public int SPStaticCost { get; set; }
        public decimal SPPercentCost { get; set; }

        public int SpellEffectID { get; set; }
        public SpellEffect SpellEffect { get; set; } = null!;

        public Spell() { }

        /// <summary>Copy constructor for script-generated dimension variants. SpellEffect is a
        /// shared reference; the caller repoints it at the same dimension's effect clone.</summary>
        public Spell(Spell other)
        {
            this.ID = other.ID;
            this.Name = other.Name;
            this.Description = other.Description;
            this.Target = other.Target;
            this.ClassRestrictions = other.ClassRestrictions;
            this.Aether = other.Aether;
            this.Graphic = other.Graphic;
            this.GraphicFile = other.GraphicFile;
            this.HPStaticCost = other.HPStaticCost;
            this.HPPercentCost = other.HPPercentCost;
            this.MPStaticCost = other.MPStaticCost;
            this.MPPercentCost = other.MPPercentCost;
            this.SPStaticCost = other.SPStaticCost;
            this.SPPercentCost = other.SPPercentCost;
            this.SpellEffectID = other.SpellEffectID;
            this.SpellEffect = other.SpellEffect;
        }
    }
}
