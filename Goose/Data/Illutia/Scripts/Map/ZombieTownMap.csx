using Goose;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

public class ZombieTownMap : BaseMapScript
{
	int[] blockedTiles = new int[]
	{
		// up/down
		45,193, 45,207, 271194,2487, 1,
		58,193, 58,200, 271194,2487, 1,
		64,175, 64,180, 271194,2487, 1,
		63,109, 63,129, 271194,2487, 1,
		55,140, 55,141, 271194,2487, 1,
		44,109, 44,120, 271194,2487, 1,
		36,121, 36,165, 271194,2487, 1,
		56,162, 56,165, 271194,2487, 1,
		47,87, 47,101, 271194,2487, 1,
		63,87, 63,101, 271194,2487, 1,
		14,163, 14,180, 271194,2487, 1,

		// left/right
		14,162, 35,162, 271185,2487, 1,
		36,120, 43,120, 271185,2487, 1,

		// blockers
		54,82, 55,83, 51416,2347, 1,
		54,79, 55,80, 51416,2347, 1,
		54,76, 55,77, 51416,2347, 1,

		// tiles to unblock
		49,196, 54,198, 0,0, 0,
		46,62, 48,64, 0,0, 0,
		60,68, 62,69, 0,0, 0,
		61,70, 61,70, 0,0, 0,
	};

	public override void OnLoad(Map map, GameWorld world)
	{
		map.ScriptStore = new MapScriptData();
	}

	public override void OnLoadTile(Map map, int x, int y, int layerNumber, int graphic, short sheet, int flags, GameWorld world)
	{
		var scriptData = (MapScriptData)map.ScriptStore;

		for (int i = 0; i < blockedTiles.Length / 6; i++)
		{
			int o = i * 7;
			if (x >= blockedTiles[o + 0] && x <= blockedTiles[o + 2] &&
				y >= blockedTiles[o + 1] && y <= blockedTiles[o + 3])
			{
				DynamicTile tile = scriptData.GetDynamicTile(x, y, map.Width);
				if (tile == null)
				{
					tile = new DynamicTile(x, y, flags);
					tile.Blocked = blockedTiles[o + 6] == 1;
					scriptData.SetDynamicTile(x, y, map.Width, tile);
				}

				if (layerNumber == 2 && blockedTiles[o + 4] != 0)
				{
					tile.LayerInfo.Add(blockedTiles[o + 4]);
					tile.LayerInfo.Add(blockedTiles[o + 5]);
				}
				else
				{
					tile.LayerInfo.Add(graphic);
					tile.LayerInfo.Add(sheet);
				}
			}
		}
	}

	public override void OnFinishedLoad(Map map, GameWorld world)
	{
		var scriptData = (MapScriptData)map.ScriptStore;

		foreach (var dynamicTile in scriptData.DynamicTiles.Values)
		{
			map.SetTile(dynamicTile.X, dynamicTile.Y, (dynamicTile.Blocked ? new BlockedTile() : null));
		}
	}

	public override void OnPlayerEntered(Map map, Player player, GameWorld world)
	{
		var scriptData = (MapScriptData)map.ScriptStore;

		foreach (var dynamicTile in scriptData.DynamicTiles.Values)
		{
			world.Send(player, dynamicTile.GetTUPPacket());
		}

		var debuff = world.SpellHandler.GetSpellEffect(359);
		debuff.Cast(player, player, world);
	}

	public override void OnPlayerLeft(Map map, Player player, GameWorld world)
	{
		var debuff = player.Buffs.FirstOrDefault(b => b.SpellEffect.ID == 359);
		if (debuff != null)
			player.RemoveBuff(debuff, world);
	}

	private static readonly int[] blockerLocations = new int[] { 54, 82, 55, 83, 54, 79, 55, 80, 54, 76, 55, 77 };
	private static readonly int[] npcsToOpenBlockers = new int[] { 452, 453, 454 };
	public override void OnNPCKilledEvent(Map map, NPC npc, ICharacter killer, GameWorld world)
	{
		if (!npcsToOpenBlockers.Contains(npc.NPCTemplateID)) return;

		var scriptData = (MapScriptData)map.ScriptStore;

		if (!npcsToOpenBlockers.Where(n => n != npc.NPCTemplateID).Any(i => map.NPCs.Any(n => n.NPCTemplateID == i && n.State == NPC.States.Alive)))
		{
			// open blockers
			for (int i = 0; i < blockerLocations.Length / 4; i++)
			{
				int o = i * 4;

				for (int y = blockerLocations[o + 1]; y <= blockerLocations[o + 3]; y++)
				{
					for (int x = blockerLocations[o + 0]; x <= blockerLocations[o + 2]; x++)
					{
						DynamicTile tile = scriptData.GetDynamicTile(x, y, map.Width);
						tile.Blocked = false;
						tile.LayerInfo[2 * 2] = 0;
						tile.LayerInfo[2 * 2 + 1] = 0;

						map.SetTile(tile.X, tile.Y, null);

						world.SendToMap(map, tile.GetTUPPacket());
					}
				}
			}
		}
	}

	public override void OnNPCSpawnEvent(Map map, NPC npc, GameWorld world)
	{
		if (!npcsToOpenBlockers.Contains(npc.NPCTemplateID)) return;

		var scriptData = (MapScriptData)map.ScriptStore;

		// close blockers
		for (int i = 0; i < blockerLocations.Length / 4; i++)
		{
			int o = i * 4;

			for (int y = blockerLocations[o + 1]; y <= blockerLocations[o + 3]; y++)
			{
				for (int x = blockerLocations[o + 0]; x <= blockerLocations[o + 2]; x++)
				{
					DynamicTile tile = scriptData.GetDynamicTile(x, y, map.Width);
					tile.Blocked = false;
					tile.LayerInfo[2 * 2] = 51416;
					tile.LayerInfo[2 * 2 + 1] = 2347;

					map.SetTile(tile.X, tile.Y, new BlockedTile());

					world.SendToMap(map, tile.GetTUPPacket());
				}
			}
		}
	}
}

return typeof(ZombieTownMap);