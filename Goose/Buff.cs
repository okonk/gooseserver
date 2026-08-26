using System.Text;

namespace Goose
{
    /**
     * Buff holds info about a buff on a player
     * 
     */
    public class Buff
    {
        public ICharacter Caster { get; set; } = null!;
        public ICharacter Target { get; set; } = null!;
        public SpellEffect SpellEffect { get; set; } = null!;
        public long TimeCast { get; set; }
        public bool ItemBuff { get; set; }
        public Event? BuffExpireEvent { get; set; }
    }
}
