using Goose;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class EasterEvent : BaseNPCScript
{
	List<Tuple<string, int, int>> events = new List<Tuple<string, int, int>>
	{
		Tuple.Create("You see red tracks appear on the ground, looks like bunny prints!", 455, 20),
		Tuple.Create("Boing Boing! *Ground shakes*", 456, 10),
		Tuple.Create("You see blue tracks appear on the ground, looks like bunny prints!", 457, 20),
		Tuple.Create("Loki yells: GET BACK HERE YOU THIEF!", 458, 20),
	};

	public override void OnSpawnEvent(NPC npc, GameWorld world)
	{
		DoEvent(npc, world);
	}

	public void DoEvent(NPC npc, GameWorld world)
	{
		var e = events[world.Random.Next(0, events.Count)];

		// check if any are still alive and  delete them?
		var npcs = ((List<NPC>)npc.ScriptStore) ?? new List<NPC>();
		foreach (var bunny in npcs)
		{
			if (bunny.State == NPC.States.Alive)
			{
				bunny.Map.SetCharacter(null, bunny.MapX, bunny.MapY);
				bunny.State = NPC.States.Dead;
				bunny.MoveEvent = null;
				bunny.CurrentHP = 0;
				string packet = P.EraseCharacter(bunny.LoginID);
				foreach (var player in bunny.Map.GetPlayersInRange(bunny))
				{
					world.Send(player, packet);
				}

				bunny.Map.RemoveNPC(bunny);
			}
		}
		npcs.Clear();

		NPCTemplate template = world.NPCHandler.GetNPCTemplate(e.Item2);

		for (int i = 0; i < e.Item3; i++)
		{
			int x = world.Random.Next(30, 170);
			int y = world.Random.Next(30, npc.Map.Height - 30);

			var spawned = new NPC();
			spawned.LoadFromTemplate(world, npc.Map.ID, x, y, template, shouldRespawn: false);
			npcs.Add(spawned);
		}

		npc.ScriptStore = npcs;

		world.SendToAll(P.ServerMessage(e.Item1));

		Goose.Events.ScriptTimerEvent.Create(() =>
		{
			var type = npc.Script.Object.GetType();
			var methodInfo = type.GetMethod("DoEvent");
			methodInfo.Invoke(npc.Script.Object, new object[] { npc, world });
		}, TimeSpan.FromMinutes(30), world);
	}
}

return typeof(EasterEvent);