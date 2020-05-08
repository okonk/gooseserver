using System;
using System.IO;
using Goose;
using Goose.Scripting;
using System.Linq;

public class TestSpell3 : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
        ((Player)target).AdditionalExperienceModifier = 100;
		// target.HairID = (target.HairID + 1) % 47;

		// string packet = P.UpdateCharacter((Player)target);
		// world.Send(((Player)target), packet);
		// foreach (var player in target.Map.GetPlayersInRange(target))
		// {
		// 	world.Send(player, packet);
		// }

		return true;
	}
}

return typeof(TestSpell3);