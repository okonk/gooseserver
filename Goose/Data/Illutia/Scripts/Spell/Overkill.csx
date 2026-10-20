using System;
using System.Globalization;
using Goose;
using Goose.Scripting;

public class Overkill : BaseSpellEffectScript
{
    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world, SpellCast? cast)
    {
        if (cast is null || target is not NPC npc || npc.State == NPC.States.Dead)
            return thisEffect.CastFormulaSpell(caster, target, world);

        long hpBefore = npc.CurrentHP;
        if (!thisEffect.CastFormulaSpell(caster, target, world, out long hpChange, out long critMultiplier))
            return false;

        long damage = -hpChange / Math.Max(1, critMultiplier);
        if (damage <= 0) return true;

        cast.TotalDamage += damage;
        cast.UsefulDamage += npc.State == NPC.States.Dead ? Math.Min(hpBefore, damage) : damage;

        return true;
    }

    public override void OnCastCompleted(SpellEffect thisEffect, SpellCast cast, GameWorld world)
    {
        if (cast.TotalDamage <= 0 || cast.UsefulDamage >= cast.TotalDamage) return;

        double wasted = Math.Min((double)(cast.TotalDamage - cast.UsefulDamage) / cast.TotalDamage, RefundCap(thisEffect));
        cast.Caster.CurrentHP += (long)(cast.HPPercentCostPaid * wasted);
        cast.Caster.CurrentMP += (long)(cast.MPPercentCostPaid * wasted);
    }

    public override IEnumerable<string>? GetItemDescription(SpellEffect thisEffect, GameWorld world)
    {
        if (!string.IsNullOrWhiteSpace(thisEffect.HPFormula) && thisEffect.HPFormula != "0")
            yield return thisEffect.GetFormulaDescription(thisEffect.HPFormula, "HP", "");
        if (!string.IsNullOrWhiteSpace(thisEffect.MPFormula) && thisEffect.MPFormula != "0")
            yield return thisEffect.GetFormulaDescription(thisEffect.MPFormula, "MP", "");
        if (thisEffect.TauntAggro > 0)
            yield return $"Taunt for {thisEffect.TauntAggro:N0}";
        double refundCap = RefundCap(thisEffect);
        yield return refundCap < 1
            ? $"Overkill refunds unused HP/MP cost (up to {refundCap:P0})"
            : "Overkill refunds unused HP/MP cost";
    }

    private static double RefundCap(SpellEffect thisEffect)
    {
        foreach (var param in (thisEffect.ScriptParams ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = param.Split('=', 2);
            if (kv.Length == 2 && kv[0] == "cap" &&
                double.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                return Math.Clamp(value, 0, 1);
        }
        return 1;
    }
}

return typeof(Overkill);
