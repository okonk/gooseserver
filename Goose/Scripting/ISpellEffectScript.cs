using System.Text;

namespace Goose.Scripting
{
    public interface ISpellEffectScript
    {
        bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world);

        void OnBuffAdded(Buff buff, GameWorld world);

        void OnBuffRemoved(Buff buff, GameWorld world);

        void OnBuffTick(Buff buff, GameWorld world);

        /// <summary>Lines to show in place of the built-in description. Return null or an empty
        /// sequence to fall through to SpellEffect's own switch.</summary>
        IEnumerable<string> GetItemDescription(SpellEffect thisEffect, GameWorld world);
    }
}
