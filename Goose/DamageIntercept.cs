using Goose.Scripting;

namespace Goose
{
    internal static class DamageIntercept
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public static long Apply(ICharacter target, ICharacter attacker, long damage, GameWorld world)
        {
            long raw = damage;

            foreach (Buff buff in target.Buffs)
            {
                Script<ISpellEffectScript>? script = buff.SpellEffect?.Script;
                if (script is null) continue;

                try
                {
                    damage = script.Object.InterceptDamage(buff, attacker, raw, damage, world);
                }
                catch (Exception e)
                {
                    log.Error(e, "SpellEffect InterceptDamage {0} ({1}) target {2} ({3}) Exception",
                        buff.SpellEffect.Name, buff.SpellEffect.ID, target.Name, target.LoginID);
                }
            }

            return damage;
        }
    }
}
