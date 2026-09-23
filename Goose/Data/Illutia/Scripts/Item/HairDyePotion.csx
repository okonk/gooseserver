using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;

public class HairDyePotion : BaseItemScript
{
	public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
	{
		// ScriptParams holds the dye colour as "r,g,b,a", written per item by /hairdye create.
		string[] parts = (item.ScriptParams ?? "").Split(',');

		// fail-closed: a potion with damaged params is kept, never consumed
		if (parts.Length != 4) return false;

		int[] rgba = new int[4];
		for (int i = 0; i < 4; i++)
		{
			if (!int.TryParse(parts[i], out rgba[i])) return false;
			if (rgba[i] < 0 || rgba[i] > 255) return false;
		}

		player.HairR = rgba[0];
		player.HairG = rgba[1];
		player.HairB = rgba[2];
		player.HairA = rgba[3];

		player.SendCHPString(world);

		return true;
	}
}

return typeof(HairDyePotion);
