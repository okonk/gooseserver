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
		player.MaxStats.Haste = GameWorld.Settings.BaseHaste;
		player.MaxStats.SpellDamage = GameWorld.Settings.BaseSpellDamage;
		player.MaxStats.SpellCrit = GameWorld.Settings.BaseSpellCrit;
		player.MaxStats.MeleeDamage = GameWorld.Settings.BaseMeleeDamage;
		player.MaxStats.MeleeCrit = GameWorld.Settings.BaseMeleeCrit;
		player.MaxStats.DamageReduction = GameWorld.Settings.BaseDamageReduction;
		player.MaxStats.HPPercentRegen = GameWorld.Settings.BaseHPPercentRegen;
		player.MaxStats.HPStaticRegen = GameWorld.Settings.BaseHPStaticRegen;
		player.MaxStats.MPPercentRegen = GameWorld.Settings.BaseMPPercentRegen;
		player.MaxStats.MPStaticRegen = GameWorld.Settings.BaseMPStaticRegen;

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