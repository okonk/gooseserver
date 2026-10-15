using Goose;
using Goose.Scripting;

public class WanderingChestNPC : BaseNPCScript
{
    public override void OnAttackedEvent(NPC npc, ICharacter attacker, long damage, GameWorld world)
    {
        npc.ScriptStore = world.TimeNow;
    }

    public override void OnKilledEvent(NPC npc, ICharacter killer, GameWorld world)
    {
        string name = killer is Pet pet ? pet.Owner.Name : killer.Name;
        world.SendToAll(P.ServerMessage($"[Event] {name} has cracked open the Wandering Chest!"));
    }
}

return typeof(WanderingChestNPC);
