using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Goose
{
    /**
     * Buff holds info about a buff on a player
     * 
     */
    public class Buff
    {
        public ICharacter Caster { get; set; }
        public ICharacter Target { get; set; }
        public SpellEffect SpellEffect { get; set; }
        public long TimeCast { get; set; }
        public bool ItemBuff { get; set; }
        public Event BuffExpireEvent { get; set; }
    }
}
