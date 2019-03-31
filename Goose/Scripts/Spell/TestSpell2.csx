using System;
using Goose;
using Goose.Scripting;

public class TestSpell2 : ISpellEffectScript
{
	public bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		var item = world.ItemHandler.GetItem(449778);
		item.StatMultiplier = 2;


		return true;
	}
}

return typeof(TestSpell2);