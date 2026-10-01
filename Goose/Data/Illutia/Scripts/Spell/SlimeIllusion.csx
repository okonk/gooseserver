using Goose;
using Goose.Scripting;

public class SlimeIllusion : BaseSpellEffectScript
{
    private static readonly (int BodyId, double Weight)[] Options =
    [
        (111, 48.5),
        (113, 48.5),
        (117, 1.5),
        (233, 0.5),
        (276, 0.5),
        (282, 0.5),
    ];

    public override void OnBuffAdded(Buff buff, GameWorld world)
    {
        if (buff.Target is not Player player) return;

        double roll = world.Random.NextDouble() * 100.0;
        double cumulative = 0.0;

        foreach (var (bodyId, weight) in Options)
        {
            cumulative += weight;
            if (roll >= cumulative) continue;
            player.CurrentBodyID = bodyId;
            Send(player, world);
            return;
        }
    }

    public override void OnBuffRemoved(Buff buff, GameWorld world)
    {
        if (buff.Target is not Player player) return;

        player.CurrentBodyID = player.BodyID;
        Send(player, world);
    }

    private static void Send(Player player, GameWorld world)
    {
        string packet = P.UpdateCharacter(player);
        world.Send(player, packet);
        foreach (Player nearby in player.Map.GetPlayersInRange(player))
        {
            world.Send(nearby, packet);
        }
    }
}

return typeof(SlimeIllusion);
