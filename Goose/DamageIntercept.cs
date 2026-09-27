using Goose.Scripting;

namespace Goose
{
    internal static class DamageIntercept
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static long Apply(ICharacter target, ICharacter attacker, long damage, GameWorld world)
        {
            long raw = damage;

            foreach (Buff buff in target.Buffs.ToArray())
            {
                SpellEffect effect = buff.SpellEffect;
                if (effect is null) continue;

                Script<ISpellEffectScript>? script = effect.Script;
                if (script is null) continue;

                try
                {
                    damage = script.Object.InterceptDamage(buff, attacker, raw, damage, world);
                }
                catch (Exception e)
                {
                    log.Error(e, "SpellEffect InterceptDamage {0} ({1}) target {2} ({3}) Exception",
                        effect.Name, effect.ID, target.Name, target.LoginID);
                }
            }

            return damage;
        }
    }
}
