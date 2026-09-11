using System;
using Goose;
using Goose.Scripting;

public class Backstab : BaseSpellEffectScript
{
    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
    {
        var (x, y) = caster.Facing switch
        {
            Direction.Up => (caster.MapX, caster.MapY - 1),
            Direction.Right => (caster.MapX + 1, caster.MapY),
            Direction.Down => (caster.MapX, caster.MapY + 1),
            Direction.Left => (caster.MapX - 1, caster.MapY),
            _ => (caster.MapX, caster.MapY)
        };

        var packet = string.Join("\x1", P.Attack(caster),
            P.SpellTile(x, y, thisEffect.Animation, thisEffect.AnimationFile));
        if (caster is Player player)
            world.Send(player, packet);
        foreach (var nearbyPlayer in caster.Map.GetPlayersInRange(caster))
            world.Send(nearbyPlayer, packet);

        var occupant = caster.Map.GetCharacterAt(x, y);
        if (occupant is null || !thisEffect.CanCastSpell(caster, occupant))
            return true;

        var (hpResult, _) = thisEffect.CalculateFormulaResults(
            thisEffect.HPFormula, "0", caster, occupant, world);
        var damage = -hpResult;
        if (caster.Facing == occupant.Facing)
            damage = (long)(damage * 1.5);
        if (damage > 0)
            occupant.Attacked(caster, damage, world);

        return true;
    }
}

return typeof(Backstab);
