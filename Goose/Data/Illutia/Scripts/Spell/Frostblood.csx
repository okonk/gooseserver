using System;
using System.Globalization;
using Goose;
using Goose.Scripting;

public class Frostblood : BaseSpellEffectScript
{
    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
    {
        double lifesteal = 0.45;
        double maxHpPerHit = 0.008;
        foreach (var param in (thisEffect.ScriptParams ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = param.Split('=', 2);
            if (kv.Length != 2 || !double.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) continue;
            if (kv[0] == "ls") lifesteal = value;
            else if (kv[0] == "hp") maxHpPerHit = value;
        }

        (int x, int y)[] tiles =
        [
            (caster.MapX, caster.MapY),
            (caster.MapX, caster.MapY - 1),
            (caster.MapX + 1, caster.MapY),
            (caster.MapX, caster.MapY + 1),
            (caster.MapX - 1, caster.MapY),
        ];

        long totalDamage = 0;
        int targetsHit = 0;
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
            long dealt = Math.Max(0, hpBefore - occupant.CurrentHP);
            totalDamage += dealt;
            if (dealt > 0) targetsHit++;
        }

        if (caster is Player player && targetsHit > 0)
        {
            long heal = (long)(totalDamage * lifesteal + player.MaxHP * maxHpPerHit * targetsHit);
            player.CurrentHP = Math.Min(player.MaxHP, player.CurrentHP + heal);
            packet += P.BattleTextHeal(player, -heal);
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
