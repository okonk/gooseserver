using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;

public class ZombieLegIllusion : BaseItemScript
{
	private HashSet<int> blockedIds = new HashSet<int>() { 290, 291, 292, 293, 313, 319 };

	public override void OnCreateEvent(Item item, GameWorld world)
	{
		int npcId = 0;
		do
		{
			npcId = world.Random.Next(101, 337);
		} while (blockedIds.Contains(npcId));

		item.ScriptParams = npcId.ToString();
		item.Description = string.Format("*Special Melee Effect: BodyID {0}*", npcId);
	}

	public override void OnMeleeEvent(Player player, Item item, GameWorld world)
	{
		int x = player.MapX;
		int y = player.MapY;

		switch (player.Facing)
		{
			case 1: y--; break;
			case 2: x++; break;
			case 3: y++; break;
			case 4: x--; break;
		}

		var character = player.Map.GetCharacterAt(x, y);
		if (character == null || !(character is Player))
			return;

		var targetPlayer = character as Player;

		if (targetPlayer.Buffs.Any(b => b.SpellEffect.Name == "Zombie Leg Illusion"))
			return;

		var spellEffect = new SpellEffect();
		spellEffect.Name = "Zombie Leg Illusion";
		spellEffect.EffectType = SpellEffect.EffectTypes.Buff;
		spellEffect.BuffGraphic = 50756;
		spellEffect.BuffGraphicFile = 104;
		spellEffect.BuffCanBeRemoved = true;
		spellEffect.BodyID = Convert.ToInt32(item.ScriptParams);
		spellEffect.Duration = 1800;
		spellEffect.Stats = new AttributeSet();
		spellEffect.BuffStacksOver = new List<SpellEffect>();
		spellEffect.BuffDoesntStackOver = new List<SpellEffect>();
		spellEffect.OnEffectText = "";
		spellEffect.OffEffectText = "";

		var buff = new Buff();
		buff.Caster = player;
		buff.Target = targetPlayer;
		buff.TimeCast = world.TimeNow;
		buff.ItemBuff = false;
		buff.SpellEffect = spellEffect;

		targetPlayer.AddBuff(buff, world);
	}
}

return typeof(ZombieLegIllusion);