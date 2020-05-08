using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;

public class HairCutItem : BaseItemScript
{
	public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
	{
		player.HairID = Convert.ToInt32(item.ScriptParams);

		string packet = P.UpdateCharacter(player);
		world.Send(player, packet);
		foreach (var p in player.Map.GetPlayersInRange(player))
		{
			world.Send(p, packet);
		}

		return true;
	}
}

return typeof(HairCutItem);