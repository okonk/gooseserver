using System;
using Goose;
using Goose.Scripting;
using System.Linq;

public class TestSpell3 : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		foreach (var zombie in caster.Map.NPCs.Where(n => n.NPCTemplateID == 449))
		{
			zombie.Attacked(caster, 1000000000, world);
		}

		return true;
	}
}

return typeof(TestSpell3);