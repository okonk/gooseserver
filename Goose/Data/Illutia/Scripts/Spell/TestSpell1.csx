using System;
using Goose;
using Goose.Scripting;

public class TestSpell1 : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		NPCTemplate template = world.NPCHandler.GetNPCTemplate(331);
		if (template == null) return false;

		for (int i = 0; i < 100; i++)
		{
			new NPC().LoadFromTemplate(world, caster.Map.ID, caster.MapX, caster.MapY, template, shouldRespawn: false);
		}

		return true;
	}
}

return typeof(TestSpell1);