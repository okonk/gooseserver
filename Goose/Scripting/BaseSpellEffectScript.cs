using System.Text;

namespace Goose.Scripting
{
    public class BaseSpellEffectScript : ISpellEffectScript
    {
        public BaseSpellEffectScript() { }

        public virtual bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world, SpellCast? cast = null)
        {
            return true;
        }

        public virtual void OnCastCompleted(SpellEffect thisEffect, SpellCast cast, GameWorld world)
        {

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

        public virtual IEnumerable<string>? GetItemDescription(SpellEffect thisEffect, GameWorld world)
        {
            return null;
        }

        public virtual long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
        {
            return currentDamage;
        }
    }
}
