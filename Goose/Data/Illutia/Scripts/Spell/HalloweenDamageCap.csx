#load "../Global/Halloween/HalloweenConfig.csx"
using System;
using Goose;
using Goose.Scripting;

public class HalloweenDamageCap : BaseSpellEffectScript
{
    public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
    {
        if (currentDamage <= 0) return currentDamage;
        var cfg = HalloweenConfig.Get(world);
        if (cfg.Effects.WeeperCap == 0) return currentDamage;
        return Math.Min(currentDamage, cfg.Weeper.CapPerHit);
    }
}

return typeof(HalloweenDamageCap);
