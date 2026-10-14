using System.Globalization;
using Goose;
using Goose.Scripting;

public class TickSnare : BaseSpellEffectScript
{
    public override void OnBuffAdded(Buff buff, GameWorld world) => Snare(buff, world);

    // A scripted buff replaces the engine's tick handling (BuffTickEvent), so the formula tick is cast here.
    // %ccmp is pinned to the caster's MP when the buff was cast, not their MP at tick time.
    public override void OnBuffTick(Buff buff, GameWorld world)
    {
        var castMP = buff.CasterMPAtCast.ToString(CultureInfo.InvariantCulture);
        var tick = new SpellEffect(buff.SpellEffect)
        {
            HPFormula = buff.SpellEffect.HPFormula.Replace("%ccmp", castMP),
            MPFormula = buff.SpellEffect.MPFormula.Replace("%ccmp", castMP),
        };
        tick.CastFormulaSpell(buff.Caster, buff.Target, world);
        Snare(buff, world);
    }

    private static void Snare(Buff buff, GameWorld world)
    {
        if (!int.TryParse(buff.SpellEffect.ScriptParams, out int snareEffectId)) return;
        world.SpellHandler.GetSpellEffect(snareEffectId)?.CastSpell(buff.Caster, buff.Target, world);
    }
}

return typeof(TickSnare);
