using Goose;
using Goose.Scripting;
using Newtonsoft.Json;

private class HealerParams
{
    public int spellId;
    public int[] healIds;
}

public class HealerNPC : BaseNPCScript
{
    private static SpellEffect spell;
    private static int[] healIds;

    public override void OnSpawnEvent(NPC npc, GameWorld world)
	{
		var p = JsonConvert.DeserializeObject<HealerParams>(npc.ScriptParams);
        HealerNPC.healIds = p.healIds;
        HealerNPC.spell = world.SpellHandler.GetSpellEffect(p.spellId);
    }

	public override void OnAttackEvent(NPC npc, GameWorld world)
	{
        var buddies = npc.Map.GetNPCsInRange(npc).Where(n => HealerNPC.healIds.Contains(n.NPCTemplateID));
        foreach (var buddy in buddies)
        {
            if (buddy.CurrentHP == buddy.MaxStats.HP) continue;

            HealerNPC.spell.Cast(npc, buddy, world);
        }
		
        npc.HandleAttackEvent(world);
	}
}

return typeof(HealerNPC);