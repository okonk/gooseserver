#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
using System;
using System.Linq;
using Goose;
using Goose.Scripting;

public class HalloweenDamageCurve : BaseSpellEffectScript
{
    public override long InterceptDamage(Buff buff, ICharacter attacker, long rawDamage, long currentDamage, GameWorld world)
    {
        if (currentDamage <= 0) return currentDamage;
        var cfg = HalloweenConfig.Get(world);
        if (cfg.Effects.GroundskeeperCurve == 0) return currentDamage;
        var curve = cfg.Groundskeeper.Curve;
        if (currentDamage <= curve.Floor) return currentDamage;
        double scaled = curve.Floor + curve.Band * Math.Pow((currentDamage - curve.Floor) / (double)curve.Band, curve.Exponent);
        long result = (long)Math.Round(scaled);
        if (BloodyGhostAlive(buff))
            result = (long)Math.Round(result * cfg.Groundskeeper.BloodyDamageMultiplier);
        return result;
    }

    private static bool BloodyGhostAlive(Buff buff)
    {
        var map = buff.Target.Map;
        if (map is null) return false;
        return map.NPCs.Any(n => n.State == NPC.States.Alive &&
            n.Properties.TryGetProperty<string>("gkAdd", out var add) && add == "bloody");
    }
}

return typeof(HalloweenDamageCurve);
