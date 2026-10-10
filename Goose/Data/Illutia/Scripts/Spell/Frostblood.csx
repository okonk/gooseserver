using System;
using Goose;
using Goose.Scripting;

public class Frostblood : BaseSpellEffectScript
{
    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
    {
        double lifesteal = 0.45;
        var p = thisEffect.ScriptParams;
        if (p != null && p.StartsWith("ls=") && double.TryParse(p[3..], out double parsed))
            lifesteal = parsed;

        (int x, int y)[] tiles =
        [
            (caster.MapX, caster.MapY),
            (caster.MapX, caster.MapY - 1),
            (caster.MapX + 1, caster.MapY),
            (caster.MapX, caster.MapY + 1),
            (caster.MapX - 1, caster.MapY),
        ];

        long totalDamage = 0;
        string packet = "";

        foreach (var (x, y) in tiles)
        {
            var occupant = caster.Map.GetCharacterAt(x, y);
            if (occupant is null || occupant == caster) continue;
            if (!thisEffect.CanCastSpell(caster, occupant)) continue;

            var (hpResult, _) = thisEffect.CalculateFormulaResults(
                thisEffect.HPFormula, "0", caster, occupant, world);
            long damage = -hpResult;
            if (damage <= 0) continue;

            if (thisEffect.Animation != 0)
                packet += P.SpellTile(x, y, thisEffect.Animation, thisEffect.AnimationFile) + "\x1";

            long hpBefore = occupant.CurrentHP;
            occupant.Attacked(caster, damage, world);
            totalDamage += Math.Max(0, hpBefore - occupant.CurrentHP);
        }

        long heal = (long)(totalDamage * lifesteal);
        if (heal > 0 && caster is Player player)
        {
            player.CurrentHP = Math.Min(player.MaxHP, player.CurrentHP + heal);
            packet += P.BattleTextHeal(player, heal);
            world.Send(player, packet + "\x1" + P.VitalsPercentage(player));
            foreach (var other in caster.Map.GetPlayersInRange(caster))
                world.Send(other, packet);
        }
        else if (packet != "")
        {
            foreach (var other in caster.Map.GetPlayersInRange(caster))
                world.Send(other, packet);
        }

        return true;
    }
}

return typeof(Frostblood);
