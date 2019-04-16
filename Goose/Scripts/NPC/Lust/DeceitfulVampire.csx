using Goose;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

public class DeceitfulVampire : BaseNPCScript
{
	public override void OnSpawnEvent(NPC npc, GameWorld world)
	{
		npc.ScriptStore = new Tuple<NPC, long>(null, 0);
	}

	public override void OnAttackEvent(NPC npc, GameWorld world)
	{
		HealBuddy(npc, world);

		CastDrainSpell(npc, world);

		npc.HandleAttackEvent(world);
	}

	private static readonly int[] buddies = new int[] { 466 };

	private NPC GetBuddy(NPC npc)
	{
		var scriptStore = ((Tuple<NPC, long>)npc.ScriptStore);
		var buddy = scriptStore.Item1;

		if (buddy != null && buddy.State == NPC.States.Alive && Map.InRange(npc, buddy))
		{
			return buddy;
		}

		buddy = npc.Map.GetNPCsInRange(npc).Where(n => buddies.Contains(n.NPCTemplateID)).FirstOrDefault();
		npc.ScriptStore = new Tuple<NPC, long>(buddy, scriptStore.Item2);

		return buddy;
	}

	private void HealBuddy(NPC npc, GameWorld world)
	{
		var target = GetBuddy(npc);
		if (target == null || target.State == NPC.States.Dead || !Map.InRange(npc, target))
		{
			// refresh buddy
			var scriptStore = ((Tuple<NPC, long>)npc.ScriptStore);
			npc.ScriptStore = new Tuple<NPC, long>(null, scriptStore.Item2);
			target = GetBuddy(npc);
			return;
		}

		if (target.CurrentHP == target.MaxStats.HP) return;

		long healAmount = 999999;
		long numberOfHeals = 3;
		target.CurrentHP += (healAmount * numberOfHeals);

		string packet = "CST" + npc.LoginID;
		packet += string.Format("\x1SPP{0},65000,407,0,0", target.LoginID);
		packet += '\x1' + target.VPUString();
		string healString = string.Format("\x0001BT{0},7,+{1},{2}", target.LoginID, healAmount, target.Name);
		for (int i = 0; i < numberOfHeals; i++)
		{
			packet += healString;
		}

		foreach (var player in npc.Map.GetPlayersInRange(npc))
		{
			world.Send(player, packet);
		}
	}

	private void CastDrainSpell(NPC npc, GameWorld world)
	{
		var scriptStore = ((Tuple<NPC, long>)npc.ScriptStore);
		var timeNow = world.TimeNow;
		var lastCast = scriptStore.Item2;
		if (((timeNow - lastCast) / world.TimerFrequency) >= 10)
		{
			var attack = world.SpellHandler.GetSpellEffect(383);
			attack.Cast(npc, npc, world);

			npc.ScriptStore = new Tuple<NPC, long>(scriptStore.Item1, timeNow);
		}
	}
}

return typeof(DeceitfulVampire);