#load "Halloween/HalloweenConfig.csx"
#load "Halloween/HalloweenCommon.csx"
using System;
using Goose;
using Goose.Scripting;

public class HalloweenCandy : BaseGlobalScript
{
    public override void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)
    {
        AwardKill(npc, credited, world, world.TimeNow, world.Random);
    }

    public void AwardKill(NPC npc, IReadOnlyList<Player> credited, GameWorld world, long now, Random rng)
    {
        var cfg = HalloweenConfig.Get(world);
        if (!Halloween.Active(world) || cfg.Items.Candy == 0 || npc.Map.ID >= 100000) return;

        int min, max;
        bool gated;
        if (npc.Properties.TryGetProperty<int>("candyMin", out int tagMin) &&
            npc.Properties.TryGetProperty<int>("candyMax", out int tagMax))
        {
            min = tagMin;
            max = tagMax;
            gated = false;
        }
        else if (cfg.Candy.ByTemplate.TryGetValue(npc.NPCTemplateID, out int[]? range))
        {
            min = range[0];
            max = range[1];
            gated = false;
        }
        else if (npc.ShouldRespawn && npc.Level == cfg.Candy.LevelOnly)
        {
            min = cfg.Candy.BaseAmount[0];
            max = cfg.Candy.BaseAmount[1];
            gated = true;
        }
        else return;

        foreach (var player in credited)
        {
            if (gated && rng.NextDouble() >= cfg.Candy.BaseChance) continue;
            GiveCandy(world, player, rng.Next(min, max + 1));
        }
    }

    public void GiveCandy(GameWorld world, Player player, int amount)
    {
        Halloween.GiveCandy(world, player, amount);
    }
}

return typeof(HalloweenCandy);
