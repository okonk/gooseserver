using Goose;
using Goose.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

public class ArenaMap : BaseMapScript
{
	public override void OnPlayerEntered(Map map, Player player, GameWorld world)
	{
		var debuff = world.SpellHandler.GetSpellEffect(359);
		debuff.Cast(player, player, world);
	}

	public override void OnPlayerLeft(Map map, Player player, GameWorld world)
	{
		var debuff = player.Buffs.FirstOrDefault(b => b.SpellEffect.ID == 359);
		if (debuff != null)
			player.RemoveBuff(debuff, world);
	}
}

return typeof(ArenaMap);