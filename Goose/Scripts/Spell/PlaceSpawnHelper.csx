using System;
using System.IO;
using Goose;
using Goose.Scripting;
using System.Linq;

public class PlaceSpawnHelper : BaseSpellEffectScript
{
	public override bool Cast(SpellEffect thisEffect, ICharacter caster, ICharacter target, GameWorld world)
	{
		using (var writer = new StreamWriter(@"spawns-" + caster.MapID + ".csv"))
		{
			foreach (var itemTile in caster.Map.Items.Where(i => i.ItemSlot.Item.TemplateID == 1 && !string.IsNullOrEmpty(i.ItemSlot.Item.ScriptParams)))
			{
				var item = itemTile.ItemSlot.Item;

				int npcId = Convert.ToInt32(item.ScriptParams);

				NPCTemplate template = world.NPCHandler.GetNPCTemplate(npcId);
				if (template == null) return false;

				writer.WriteLine("{0},{1},{2},{3}", npcId, caster.Map.ID, itemTile.X, itemTile.Y);

				if (thisEffect.ScriptParams == "spawn")
					new NPC().LoadFromTemplate(world, caster.Map.ID, itemTile.X, itemTile.Y, template, shouldRespawn: true);
			}
		}

		return true;
	}
}

return typeof(PlaceSpawnHelper);