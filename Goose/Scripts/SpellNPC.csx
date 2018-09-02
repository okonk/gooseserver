using Goose;
using Goose.Scripting;

public class SpellNPC : INPCScript
{
	public SpellNPC() { }

	public void OnMoveEvent(NPC npc, GameWorld world)
	{
		npc.HandleMoveEvent(world);
	}

	public void OnAttackEvent(NPC npc, GameWorld world)
	{
		if (npc.AggroTarget != null)
		{
			var spell = world.SpellHandler.GetSpellByName("Lightning Strike");
			spell.SpellEffect.Cast(npc, npc.AggroTarget, world);
		}

		npc.AddAttackEvent(world);
	}
}

return typeof(SpellNPC);