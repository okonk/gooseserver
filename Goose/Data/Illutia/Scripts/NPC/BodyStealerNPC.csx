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

			bool layered = BodyClassification.IsLayered(transPlayer.CurrentBodyID);

			string chp = "CHP" +
				npc.LoginID + "," +
				transPlayer.CurrentBodyID + "," +
				transPlayer.BodyR + "," + // Body Color R
				transPlayer.BodyG + "," + // Body Color G
				transPlayer.BodyB + "," + // Body Color B
				transPlayer.BodyA + "," + // Body Color A
				(layered ? pose : 3) + "," +
				(layered ? transPlayer.HairID + "," : "") +
				(layered ? transPlayer.Inventory.EquippedDisplay() : "") + // Note: EquippedDisplay() adds it's own , on end
				(layered ? transPlayer.HairR + "," : "") +
				(layered ? transPlayer.HairG + "," : "") +
				(layered ? transPlayer.HairB + "," : "") +
				(layered ? transPlayer.HairA + "," : "") +
				"0" + "," + // Invis thing
				(layered ? transPlayer.FaceID + "," : "") +
				transPlayer.CalculateMoveSpeed() + "," + // Move Speed
				(layered ? transPlayer.Inventory.MountDisplay() : ""); // Mount

			foreach (Player p in npc.Map.GetPlayersInRange(npc))
			{
				world.Send(p, chp);
			}
		}

		base.OnAttackEvent(npc, world);
	}
}

return typeof(BodyStealerNPC);
