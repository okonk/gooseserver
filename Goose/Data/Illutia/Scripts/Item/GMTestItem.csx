using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;

public class GMTestItem : BaseItemScript
{
	public override bool OnUseConsumableEvent(Player player, Item item, GameWorld world)
	{
		int? classId = null;
		int level = 50;
		long? exp = null;
		long? gold = null;
		List<int> spells = new List<int>();

		foreach (string pair in (item.ScriptParams ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
		{
			string[] kv = pair.Split(new[] { '=' }, 2);
			if (kv.Length != 2) return false;

			string key = kv[0].Trim().ToLowerInvariant();
			string value = kv[1].Trim();

			if (key == "spells")
			{
				foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
				{
					if (!int.TryParse(part.Trim(), out int spellId) || spellId <= 0) return false;
					spells.Add(spellId);
				}
				continue;
			}

			if (key == "class")
			{
				if (!int.TryParse(value, out int c) || c <= 0) return false;
				classId = c;
			}
			else if (key == "level")
			{
				if (!int.TryParse(value, out int l) || l <= 0) return false;
				level = l;
			}
			else if (key == "exp")
			{
				if (!long.TryParse(value, out long e)) return false;
				exp = e;
			}
			else if (key == "gold")
			{
				if (!long.TryParse(value, out long g)) return false;
				gold = g;
			}
			else
			{
				return false;
			}
		}

		bool applied = false;

		if (classId is not null)
		{
			// ChangeClass refuses when the current class has no row for level-1 (a Commoner
			// only has levels 1-5), so hop through the highest level it does support first.
			if (player.Class.GetLevel(level - 1) is null)
			{
				int hop = 1;
				for (int i = level - 1; i >= 1; i--)
				{
					if (player.Class.GetLevel(i) is not null)
					{
						hop = i + 1;
						break;
					}
				}
				player.ChangeClass(classId.Value, hop, world);
			}

			player.ChangeClass(classId.Value, level, world);
			applied = true;
		}

		if (exp is not null)
		{
			if (classId is not null)
			{
				player.Experience = exp.Value;
				world.Send(player, P.StatusInfo(player));
				world.Send(player, P.ExpBar(player));
			}
			else
			{
				player.Experience += exp.Value;
				player.ProcessLevelUp(world);
			}
			applied = true;
		}

		foreach (int spellId in spells)
		{
			player.LearnSpell(spellId, world);
			applied = true;
		}

		if (gold is not null)
		{
			player.AddGold(gold.Value, world);
			applied = true;
		}

		return applied;
	}
}

return typeof(GMTestItem);
