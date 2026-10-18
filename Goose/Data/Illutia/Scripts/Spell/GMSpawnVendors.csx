using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;

public class GMSpawnVendors : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world, SpellCast? cast)
	{
		if (caster is not Player player || player.Map is null) return false;

		List<int> templateIds = new List<int>();
		foreach (string token in thisEffect.ScriptParams.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
		{
			if (!int.TryParse(token, out int id) || id <= 0) return false;
			templateIds.Add(id);
		}
		if (templateIds.Count == 0) return false;

		int spawned = 0;
		foreach (int id in templateIds)
		{
			NPCTemplate? template = world.NPCHandler.GetNPCTemplate(id);
			if (template is null)
			{
				world.Send(player, P.ServerMessage("Unknown NPC template " + id + "."));
				continue;
			}

			if (world.NPCHandler.SpawnNPC(world, player.Map.ID, player.MapX, player.MapY,
				template, shouldRespawn: false) is null) continue;

			spawned++;
			world.LogHandler.Log(Log.Types.SpawnedNPC,
				player.PlayerID, template.Name,
				template.NPCTemplateID, player.Map.ID, player.MapX, player.MapY);
		}

		if (spawned < templateIds.Count)
		{
			world.Send(player, P.ServerMessage($"Spawned {spawned} of {templateIds.Count} vendors."));
		}

		return spawned > 0;
	}
}

return typeof(GMSpawnVendors);
