using Goose;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

public class ZombieNPC : BaseNPCScript
{
	public override void OnSpawnEvent(NPC npc, GameWorld world)
	{
		string[] t = npc.ScriptParams.Split(' ');

		Player player = null;

		string title = "Zombie";

		if (t.Length < 2 || t[0] != "rank")
		{
			var allPlayers = world.PlayerHandler.GetAllPlayerData().Where(p => p.Level == 50 && (int)p.Access >= 2).ToList();
			player = allPlayers[world.Random.Next(0, allPlayers.Count)];

			if (t.Length >= 1 && t[0] == "elite")
				title = "Elite Zombie";
		}
		else
		{
			int i = Convert.ToInt32(t[1]);
			if (world.RankHandler.All.RanksList.Count < i)
				player = world.RankHandler.All.RanksList.Last();
			else
				player = world.RankHandler.All.RanksList[i];

			title = string.Format("Rank {0} Zombie", i + 1);

			npc.Class = player.Class;
			npc.ClassID = player.ClassID;
		}

		int pose = player.BodyState;
		ItemSlot weapon = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
		if (weapon != null)
		{
			pose = weapon.Item.BodyState;
		}

		npc.Title = title;
		npc.Name = player.Name;
		npc.CurrentBodyID = 21;
		npc.BodyR = 0;
		npc.BodyG = 0;
		npc.BodyB = 0;
		npc.BodyA = 0;
		npc.BodyState = pose;
		npc.HairID = player.HairID;
		npc.HairR = player.HairR;
		npc.HairG = player.HairG;
		npc.HairB = player.HairB;
		npc.HairA = player.HairA;
		npc.EquippedItems = player.Inventory.EquippedDisplay().TrimEnd(',');
		npc.FaceID = player.FaceID;
	}

	public override void OnMoveEvent(NPC npc, GameWorld world)
	{
		if (npc.Title[0] != 'R')
		{
			npc.HandleMoveEvent(world);
			return;
		}

		HealBuddy(npc, world);

		npc.HandleMoveEvent(world);
	}

	public override void OnAttackEvent(NPC npc, GameWorld world)
	{
		if (npc.Title[0] != 'R')
		{
			npc.HandleAttackEvent(world);
			return;
		}

		HealBuddy(npc, world);

		if (!CastAttackSpell(npc, world))
			npc.HandleAttackEvent(world);
	}

	private static readonly int[] buddies = new int[] { 450, 451, 452, 453, 454 };

	private NPC GetBuddy(NPC npc)
	{
		var buddy = (NPC)npc.ScriptStore;

		if (buddy == npc) return null;
		if (buddy != null) return buddy;

		buddy = npc.Map.GetNPCsInRange(npc).Where(n => buddies.Contains(n.NPCTemplateID)).FirstOrDefault();
		npc.ScriptStore = buddy ?? npc;

		return buddy;
	}

	private void HealBuddy(NPC npc, GameWorld world)
	{
		if (npc.Class.ClassName != "Priest") return;

		var target = GetBuddy(npc);
		if (target == null || target.State == NPC.States.Dead || !Map.InRange(npc, target))
			target = npc;

		if (target.CurrentHP == target.MaxStats.HP) return;

		long healAmount = 70000;
		long numberOfHeals = 3;
		target.CurrentHP += (healAmount * numberOfHeals);

		string packet = P.Cast(npc);
		packet += '\x1' + P.SpellPlayer(target.LoginID, 65000, 407);
		packet += '\x1' + P.VitalsPercentage(target);
		string healString = P.BattleTextHeal(target, healAmount);
		for (int i = 0; i < numberOfHeals; i++)
		{
			packet += healString;
		}

		foreach (var player in npc.Map.GetPlayersInRange(npc))
		{
			world.Send(player, packet);
		}
	}

	private bool CastAttackSpell(NPC npc, GameWorld world)
	{
		switch (npc.Class.ClassName)
		{
			case "Priest":
				return false;
			case "Magus":
				return CastMagusAttackSpell(npc, world);
			case "Warrior":
				return CastNonTargetAttackSpell(npc, world, 379);
			case "Rogue":
				return CastNonTargetAttackSpell(npc, world, 378);
			default:
				return false;
		}
	}

	private bool CastMagusAttackSpell(NPC npc, GameWorld world)
	{
		var playersInRange = npc.Map.GetPlayersInRange(npc);
		if (!playersInRange.Any())
			return false;

		ICharacter target = npc.AggroTarget;
		if (target == null || !Map.InRange(npc, target) || world.Random.Next(0, 100) <= 20)
			target = playersInRange[world.Random.Next(0, playersInRange.Count)];

		var attack = world.SpellHandler.GetSpellEffect(377);
		attack.Cast(npc, target, world);

		npc.AddAttackEvent(world);

		return true;
	}

	private bool CastNonTargetAttackSpell(NPC npc, GameWorld world, int spellid)
	{
		var playersInRange = npc.Map.GetPlayersInRange(npc);
		if (!playersInRange.Any())
			return false;

		ICharacter target = npc.AggroTarget;
		if (target == null || !Map.InRange(npc, target))
			return false;

		var attack = world.SpellHandler.GetSpellEffect(spellid);
		attack.Cast(npc, npc, world);

		int x = npc.MapX;
		int y = npc.MapY;

		switch (npc.Facing)
		{
			case 1: y--; break;
			case 2: x++; break;
			case 3: y++; break;
			case 4: x--; break;
		}

		if (target.MapX != x || target.MapY != y)
		{
			long healAmount = 1000000;
			npc.CurrentHP += healAmount;

			string packet = P.NPCAngryEmote(npc);
		    packet += '\x1' + P.SpellPlayer(npc.LoginID, 65000, 407);
		    packet += '\x1' + P.VitalsPercentage(npc);
		    packet += '\x1' + P.BattleTextHeal(npc, healAmount);

			foreach (var player in playersInRange)
			{
				world.Send(player, packet);
			}
		}

		npc.AddAttackEvent(world);

		return true;
	}
}

return typeof(ZombieNPC);