using System;
using Goose;
using Goose.Scripting;

public class ArenaMapDebuff : BaseSpellEffectScript
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
		player.MaxStats.Haste = GameSettings.Default.BaseHaste;
		player.MaxStats.SpellDamage = GameSettings.Default.BaseSpellDamage;
		player.MaxStats.SpellCrit = GameSettings.Default.BaseSpellCrit;
		player.MaxStats.MeleeDamage = GameSettings.Default.BaseMeleeDamage;
		player.MaxStats.MeleeCrit = GameSettings.Default.BaseMeleeCrit;
		player.MaxStats.DamageReduction = GameSettings.Default.BaseDamageReduction;
		player.MaxStats.HPPercentRegen = GameSettings.Default.BaseHPPercentRegen;
		player.MaxStats.HPStaticRegen = GameSettings.Default.BaseHPStaticRegen;
		player.MaxStats.MPPercentRegen = GameSettings.Default.BaseMPPercentRegen;
		player.MaxStats.MPStaticRegen = GameSettings.Default.BaseMPStaticRegen;
		player.MaxStats.MoveSpeedIncrease = GameSettings.Default.BaseMoveSpeedIncrease;

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

return typeof(ArenaMapDebuff);