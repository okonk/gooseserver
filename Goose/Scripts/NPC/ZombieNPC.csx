using Goose;
using Goose.Scripting;
using System.Collections.Generic;
using System.Linq;

public class ZombieNPC : BaseNPCScript
{
	public override void OnSpawnEvent(NPC npc, GameWorld world)
	{
		var allPlayers = world.PlayerHandler.GetAllPlayerData().Where(p => p.Level == 50).ToList();
		var player = allPlayers[world.Random.Next(0, allPlayers.Count)];
		npc.ScriptStore = player;

		npc.MaxStats.HP = player.MaxStats.HP;
		npc.CurrentHP = player.MaxStats.HP;

		int pose = player.BodyState;
		ItemSlot weapon = player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
		if (weapon != null)
		{
			pose = weapon.Item.BodyState;
		}

		npc.Title = "Zombie";
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
}

return typeof(ZombieNPC);