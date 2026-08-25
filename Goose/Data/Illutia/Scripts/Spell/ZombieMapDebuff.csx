using System;
using Goose;
using Goose.Scripting;

public class ZombieMapDebuff : BaseSpellEffectScript
{
	public override void OnBuffAdded(Buff buff, GameWorld world)
	{
		var player = (Player)buff.Target;

		player.TemporaryMaxHP = 100000;
		player.TemporaryMaxMP = 100000;

		player.CurrentHP = player.MaxHP;
		player.CurrentMP = player.MaxMP;

		world.Send(player, P.StatusInfo(player));
	}

	public override void OnBuffRemoved(Buff buff, GameWorld world)
	{
		var player = (Player)buff.Target;
		player.MaxStats = new AttributeSet();
		player.MaxStats.Haste = world.Configuration.BaseHaste;
		player.MaxStats.SpellDamage = world.Configuration.BaseSpellDamage;
		player.MaxStats.SpellCrit = world.Configuration.BaseSpellCrit;
		player.MaxStats.MeleeDamage = world.Configuration.BaseMeleeDamage;
		player.MaxStats.MeleeCrit = world.Configuration.BaseMeleeCrit;
		player.MaxStats.DamageReduction = world.Configuration.BaseDamageReduction;
		player.MaxStats.HPPercentRegen = world.Configuration.BaseHPPercentRegen;
		player.MaxStats.HPStaticRegen = world.Configuration.BaseHPStaticRegen;
		player.MaxStats.MPPercentRegen = world.Configuration.BaseMPPercentRegen;
		player.MaxStats.MPStaticRegen = world.Configuration.BaseMPStaticRegen;

		player.AddStats(player.Class.GetLevel(player.Level).BaseStats, world);
		player.AddStats(player.BaseStats, world);

		foreach (Inventory.EquipSlots slot in Enum.GetValues(typeof(Inventory.EquipSlots)))
		{
			var equipSlot = player.Inventory.GetEquippedSlot(slot);
			if (equipSlot == null) continue;

			player.AddStats(equipSlot.Item.TotalStats, world);
		}

		foreach (var otherBuff in player.Buffs)
		{
			if (otherBuff == buff) continue;

			player.AddStats(otherBuff.SpellEffect.Stats, world);
		}

		player.TemporaryMaxHP = null;
		player.TemporaryMaxMP = null;

		player.CurrentHP = player.MaxHP;
		player.CurrentMP = player.MaxMP;

		world.Send(player, P.StatusInfo(player));
	}
}

return typeof(ZombieMapDebuff);