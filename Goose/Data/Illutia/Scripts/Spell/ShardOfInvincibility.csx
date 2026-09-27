using Goose;
using Goose.Scripting;

public class ShardOfInvincibility : BaseSpellEffectScript
{
    public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
    {
        return currentDamage > 0 ? 0 : currentDamage;
    }

    public override IEnumerable<string>? GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        yield return "Reduces all damage taken to 0.";
    }
}

return typeof(ShardOfInvincibility);
