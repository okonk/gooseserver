using System;
using Goose;
using Goose.Scripting;

public class TestSpell2 : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		int casterId = (caster as Player)?.PlayerID ?? 0;

		int chance = 100;
		int number = 100;
		int id = 106;

		NPCTemplate template = world.NPCHandler.GetNPCTemplate(id);
		if (template == null) return false;

		for (int i = 0; i < number; i++)
		{
			if (i == 0 || world.Random.Next(1, 101) <= chance)
				new NPC().LoadFromTemplate(world, caster.Map.ID, caster.MapX, caster.MapY, template, shouldRespawn: false);
		}

		if (casterId > 0)
			world.LogHandler.Log(Log.Types.SpawnedNPC,
				casterId, template.Name,
				template.NPCTemplateID, caster.Map.ID, caster.MapX, caster.MapY);

		return true;
	}
}

return typeof(TestSpell2);