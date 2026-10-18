namespace Goose
{
    public class SpellCast
    {
        public Spell Spell { get; }
        public Player Caster { get; }

        public long HPPercentCostPaid { get; set; }
        public long MPPercentCostPaid { get; set; }

        public long UsefulDamage { get; set; }
        public long TotalDamage { get; set; }

        public SpellCast(Spell spell, Player caster)
        {
            this.Spell = spell;
            this.Caster = caster;
        }
    }
}
