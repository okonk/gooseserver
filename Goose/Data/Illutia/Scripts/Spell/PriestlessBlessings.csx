using System;
using System.Linq;
using Goose;
using Goose.Scripting;

public class PriestlessBlessings : BaseSpellEffectScript
{
	public override void OnBuffTick(Buff buff, GameWorld world)
	{
		var player = buff.Target as Player;
		if (player == null) return;

		if (player.Group != null && player.Group.Players.Any(p => p.Class.ClassName == "Priest"))
			return;

		buff.SpellEffect.CastFormulaSpell(buff.Caster, buff.Target, world);
	}
}

return typeof(PriestlessBlessings);