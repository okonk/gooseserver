using System;
using Goose;
using Goose.Scripting;

public class TestSpell2 : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		var debuff = world.SpellHandler.GetSpellEffect(412);
		debuff.Cast(caster, target, world);

		return true;
	}
}

return typeof(TestSpell2);