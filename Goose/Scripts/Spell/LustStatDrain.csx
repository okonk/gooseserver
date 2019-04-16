using System;
using Goose;
using Goose.Scripting;

public class LustStatDrain : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		if (target is NPC) return false;

		double drainPercent = 0.05;
		string packet = thisEffect.SPPString(target.LoginID);
		packet += '\x01' + target.VPUString();

		if (target.CurrentMP / (double)target.MaxMP >= drainPercent)
		{
			// mp drain
			long drainAmount = (long)(target.MaxMP * drainPercent);
			drainAmount = Math.Min(target.CurrentMP - 1, drainAmount);
			if (drainAmount == 0) return false;

			target.CurrentMP -= drainAmount;

			packet += string.Format("\x0001BT{0},60,-{1},{2}", target.LoginID, drainAmount, target.Name);

			caster.CurrentHP += drainAmount;
			packet += string.Format("\x0001BT{0},7,+{1},{2}", caster.LoginID, drainAmount, caster.Name);
		}
		else
		{
			// health drain
			long drainAmount = (long)(target.MaxHP * drainPercent);
			drainAmount = Math.Min(target.CurrentHP - 1, drainAmount);
			if (drainAmount == 0) return false;

			target.CurrentHP -= drainAmount;

			packet += string.Format("\x0001BT{0},1,-{1},{2}", target.LoginID, drainAmount, target.Name);

			caster.CurrentHP += drainAmount;
			packet += string.Format("\x0001BT{0},7,+{1},{2}", caster.LoginID, drainAmount, caster.Name);
		}

		foreach (var player in target.Map.GetPlayersInRange(target))
		{
			world.Send(player, packet);
		}

		var p = (Player)target;
		world.Send(p, p.SNFString());
		world.Send(p, packet);

		p.AddRegenEvent(world);

		return true;
	}
}

return typeof(LustStatDrain);