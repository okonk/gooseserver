using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Goose.Scripting
{
    public interface ISpellEffectScript
    {
        bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world);

        void OnBuffAdded(Buff buff, GameWorld world);

        void OnBuffRemoved(Buff buff, GameWorld world);

        void OnBuffTick(Buff buff, GameWorld world);
    }
}
