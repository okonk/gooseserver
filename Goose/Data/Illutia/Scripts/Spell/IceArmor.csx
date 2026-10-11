using System;
using System.Globalization;
using Goose;
using Goose.Scripting;

public class IceArmor : BaseSpellEffectScript
{
    public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
    {
        int snareEffectId = 306;
        double maxHpPerHit = 0.01;
        foreach (var param in (thisEffect.ScriptParams ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = param.Split('=', 2);
            if (kv.Length != 2 || !double.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) continue;
            if (kv[0] == "snare") snareEffectId = (int)value;
            else if (kv[0] == "hp") maxHpPerHit = value;
        }

        world.SpellHandler.GetSpellEffect(snareEffectId)?.CastSpell(caster, target, world);

        if (target is Player || caster is not Player player) return true;

        long heal = (long)(player.MaxHP * maxHpPerHit);
        if (heal <= 0) return true;

        player.CurrentHP = Math.Min(player.MaxHP, player.CurrentHP + heal);
        string packet = P.BattleTextHeal(player, -heal);
        world.Send(player, packet + "\x1" + P.VitalsPercentage(player));
        foreach (var other in player.Map.GetPlayersInRange(player))
            world.Send(other, packet);

        return true;
    }
}

return typeof(IceArmor);
