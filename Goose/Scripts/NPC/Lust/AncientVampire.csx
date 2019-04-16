using Goose;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

public class AncientVampire : BaseNPCScript
{
	public override void OnAttackEvent(NPC npc, GameWorld world)
	{
		CastDrainSpell(npc, world);

		if (!CastAB(npc, world))
			npc.HandleAttackEvent(world);
	}

	private bool CastAB(NPC npc, GameWorld world)
	{
		var playersInRange = npc.Map.GetPlayersInRange(npc);
		if (!playersInRange.Any())
			return false;

		ICharacter target = npc.AggroTarget;
		if (target == null || !Map.InRange(npc, target) || world.Random.Next(0, 100) <= 20)
			target = playersInRange[world.Random.Next(0, playersInRange.Count)];

		var attack = world.SpellHandler.GetSpellEffect(387);
		attack.Cast(npc, target, world);

		npc.AddAttackEvent(world);

		return true;
	}

	private void CastDrainSpell(NPC npc, GameWorld world)
	{
		var timeNow = world.TimeNow;
		var lastCast = (long?)npc.ScriptStore ?? 0;
		if (((timeNow - lastCast) / world.TimerFrequency) >= 10)
		{
			var attack = world.SpellHandler.GetSpellEffect(383);
			attack.Cast(npc, npc, world);

			npc.ScriptStore = timeNow;
		}
	}
}

return typeof(AncientVampire);