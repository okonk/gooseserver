using System;
using Goose;

// Placeholder until the ambush implementation lands; the controller compiles against these.
public static class HalloweenAmbush
{
    public static bool OnStep(GameWorld world, Map map, Player player, long now, Random rng) => false;

    public static void Trigger(GameWorld world, Map map, Player player, string outcome, long now, Random rng) { }

    public static int LiveAmbushMobs(Map map) => 0;

    public static int LivePhantoms(Map map) => 0;
}
