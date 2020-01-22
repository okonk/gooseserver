using System;
using Goose;
using Goose.Scripting;

public class LustStatDrain : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		if (target is NPC) return false;

		double drainPercent = 0.05;
		string packet = P.SpellPlayer(target.LoginID, thisEffect.Animation, thisEffect.AnimationFile);
		packet += '\x01' + P.VitalsPercentage(target);

		if (target.CurrentMP / (double)target.MaxMP >= drainPercent)
		{
			// mp drain
			long drainAmount = (long)(target.MaxMP * drainPercent);
			drainAmount = Math.Min(target.CurrentMP - 1, drainAmount);
			if (drainAmount == 0) return false;

			target.CurrentMP -= drainAmount;

			packet += '\x01' + P.BattleTextYellow(target, "-" + drainAmount);

			caster.CurrentHP += drainAmount;
			packet += '\x01' + P.BattleTextHeal(caster, drainAmount);
		}
		else
		{
			// health drain
			long drainAmount = (long)(target.MaxHP * drainPercent);
			drainAmount = Math.Min(target.CurrentHP - 1, drainAmount);
			if (drainAmount == 0) return false;

			target.CurrentHP -= drainAmount;

			packet += '\x01' + P.BattleTextDamage(target, drainAmount);

			caster.CurrentHP += drainAmount;
			packet += '\x01' + P.BattleTextHeal(caster, drainAmount);
		}

		foreach (var player in target.Map.GetPlayersInRange(target))
		{
			world.Send(player, packet);
		}

		var p = (Player)target;
		world.Send(p, P.StatusInfo(p));
		world.Send(p, packet);

		p.AddRegenEvent(world);

		return true;
	}
}

return typeof(LustStatDrain);