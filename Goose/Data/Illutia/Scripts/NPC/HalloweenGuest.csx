using System;
using System.Linq;
using Goose;
using Goose.Scripting;

public class HalloweenGuest : BaseNPCScript
{
    // The engine's wander box in NPC.HandleMoveEvent; past it the engine walks home greedily and sticks on walls.
    private const int WanderBox = 10;

    public override void OnMoveEvent(NPC npc, GameWorld world)
    {
        if (!Homing(npc))
        {
            base.OnMoveEvent(npc, world);
            return;
        }
        if (npc.PathStepTo(npc.SpawnX, npc.SpawnY, world) is Direction direction)
            npc.StepTo(direction, world);
        npc.AddMoveEvent(world);
    }

    private static bool Homing(NPC npc) =>
        npc.AggroTarget is null && !npc.Hidden &&
        npc.Properties.TryGetProperty<int>("invasion", out _) &&
        !npc.Buffs.Any(b => b.SpellEffect.EffectType is SpellEffect.EffectTypes.Stun or SpellEffect.EffectTypes.Root) &&
        (Math.Abs(npc.MapX - npc.SpawnX) > WanderBox || Math.Abs(npc.MapY - npc.SpawnY) > WanderBox);
}

return typeof(HalloweenGuest);
