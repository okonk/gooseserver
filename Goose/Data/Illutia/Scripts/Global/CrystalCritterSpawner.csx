using System;
using Goose;
using Goose.Scripting;

public class CrystalCritterSpawner : BaseGlobalScript
{
	public override void OnLoaded(GameWorld world)
	{
		//DoEvent(world);
	}

	public void DoEvent(GameWorld world)
	{
		//var e = events[world.Random.Next(0, events.Count)];

		//// check if any are still alive and  delete them?
		// var npcs = new List<NPC>();
		// foreach (var spawnedNPC in npcs)
		// {
		// 	if (spawnedNPC.State == NPC.States.Alive)
		// 	{
		// 		spawnedNPC.Map.SetCharacter(null, spawnedNPC.MapX, spawnedNPC.MapY);
		// 		spawnedNPC.State = NPC.States.Dead;
		// 		spawnedNPC.MoveEvent = null;
		// 		spawnedNPC.CurrentHP = 0;
		// 		string packet = "ERC" + spawnedNPC.LoginID;
		// 		foreach (var player in spawnedNPC.Map.GetPlayersInRange(spawnedNPC))
		// 		{
		// 			world.Send(player, packet);
		// 		}

		// 		spawnedNPC.Map.RemoveNPC(spawnedNPC);
		// 	}
		// }
		// npcs.Clear();

		// NPCTemplate template = world.NPCHandler.GetNPCTemplate(480);

		// for (int i = 0; i < e.Item3; i++)
		// {
		// 	int x = world.Random.Next(30, 170);
		// 	int y = world.Random.Next(30, npc.Map.Height - 30);

		// 	var spawned = new NPC();
		// 	spawned.LoadFromTemplate(world, npc.Map.ID, x, y, template, shouldRespawn: false);
		// 	npcs.Add(spawned);
		// }

		// npc.ScriptStore = npcs;

		// world.SendToAll("$7" + e.Item1);

		// Goose.Events.ScriptTimerEvent.Create(() =>
		// {
		// 	var type = npc.Script.Object.GetType();
		// 	var methodInfo = type.GetMethod("DoEvent");
		// 	methodInfo.Invoke(npc.Script.Object, new object[] { npc, world });
		// }, TimeSpan.FromMinutes(1), world);
	}
}

return typeof(CrystalCritterSpawner);