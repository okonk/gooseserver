using Goose;
using Goose.Scripting;
using System.Collections.Generic;
using System.Linq;

public class BodyStealerNPC : BaseNPCScript
{
	public override void OnAttackEvent(NPC npc, GameWorld world)
	{
		if (npc.Name != "Pig")
		{
			base.OnAttackEvent(npc, world);
			return;
		}

		if (npc.ScriptStore == null)
		{
			var allPlayers = world.PlayerHandler.GetAllPlayerData().Where(p => p.Level == 50).ToList();
			var player = allPlayers[world.Random.Next(0, allPlayers.Count)];
			npc.ScriptStore = player;

			npc.MaxStats.HP = player.MaxStats.HP;
			npc.CurrentHP = player.MaxStats.HP;
		}

		if (npc.AggroTarget != null)
		{
			var transPlayer = (Player)npc.ScriptStore;

			int pose = transPlayer.BodyState;
			ItemSlot weapon = transPlayer.Inventory.GetEquippedSlot(Inventory.EquipSlots.Weapon);
			if (weapon != null)
			{
				pose = weapon.Item.BodyState;
			}

			string chp = "CHP" +
				npc.LoginID + "," +
				transPlayer.CurrentBodyID + "," +
				transPlayer.BodyR + "," + // Body Color R
				transPlayer.BodyG + "," + // Body Color G
				transPlayer.BodyB + "," + // Body Color B
				transPlayer.BodyA + "," + // Body Color A
				(transPlayer.CurrentBodyID >= 100 ? 3 : pose) + "," +
				(transPlayer.CurrentBodyID >= 100 ? "" : transPlayer.HairID + ",") +
				(transPlayer.CurrentBodyID >= 100 ? "" : transPlayer.Inventory.EquippedDisplay()) + // Note: EquippedDisplay() adds it's own , on end
				(transPlayer.CurrentBodyID >= 100 ? "" : transPlayer.HairR + ",") +
				(transPlayer.CurrentBodyID >= 100 ? "" : transPlayer.HairG + ",") +
				(transPlayer.CurrentBodyID >= 100 ? "" : transPlayer.HairB + ",") +
				(transPlayer.CurrentBodyID >= 100 ? "" : transPlayer.HairA + ",") +
				"0" + "," + // Invis thing
				(transPlayer.CurrentBodyID >= 100 ? "" : transPlayer.FaceID + ",") +
				transPlayer.CalculateMoveSpeed() + "," + // Move Speed
				(transPlayer.CurrentBodyID >= 100 ? "" : transPlayer.Inventory.MountDisplay()); // Mount

			foreach (Player p in npc.Map.GetPlayersInRange(npc))
			{
				world.Send(p, chp);
			}
		}

		base.OnAttackEvent(npc, world);
	}
}

return typeof(BodyStealerNPC);