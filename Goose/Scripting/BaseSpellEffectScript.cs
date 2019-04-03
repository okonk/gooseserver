using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public class BaseSpellEffectScript : ISpellEffectScript
    {
        public BaseSpellEffectScript() { }

        public virtual bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
        {
            return true;
        }

        public virtual void OnBuffAdded(Buff buff, GameWorld world)
        {

        }

        public virtual void OnBuffRemoved(Buff buff, GameWorld world)
        {

        }

        public virtual void OnBuffTick(Buff buff, GameWorld world)
        {

        }
    }
}
