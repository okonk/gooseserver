using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;

public class CustomTicket : BaseItemScript
{
	public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
	{
		if (CustomWindow.FindOpen(player) is not null)
		{
			world.Send(player, P.ServerMessage("You are already customising an item."));
			return false;
		}

		new CustomWindow(player, world, item);

		// false = do not consume: the ticket is only consumed on a successful create (CWC flow).
		return false;
	}
}

return typeof(CustomTicket);
