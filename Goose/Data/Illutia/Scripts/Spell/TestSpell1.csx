using System;
using Goose;
using Goose.Scripting;

public class TestSpell1 : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		//target.Attacked(caster, 1000000000, world);
        var player = (Player)caster;
        for (int i = 1; i <= GameWorld.Settings.SpellbookSize; i++)
            player.Spellbook.SetSlotLastCast(i, 0);

		return true;
	}
}

return typeof(TestSpell1);