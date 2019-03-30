using System;
using Goose;
using Goose.Scripting;

public class TestSpell2 : ISpellEffectScript
{
	public bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		int casterId = (caster as Player)?.PlayerID ?? 0;

		int id = 409;

		NPCTemplate template = world.NPCHandler.GetNPCTemplate(id);
		if (template == null) return false;

		new NPC().LoadFromTemplate(world, caster.Map.ID, caster.MapX, caster.MapY, template, shouldRespawn: false);

		if (casterId > 0)
			world.LogHandler.Log(Log.Types.SpawnedNPC,
				casterId, template.Name,
				template.NPCTemplateID, caster.Map.ID, caster.MapX, caster.MapY);

		return true;
	}
}

return typeof(TestSpell2);