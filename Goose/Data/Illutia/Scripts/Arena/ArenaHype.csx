#load "ArenaConfig.csx"
#load "ArenaState.csx"
using System;
using System.Collections.Generic;
using System.Linq;
using Goose;

public static class ArenaHype
{
    public static void Mayor(GameWorld world, ArenaConfig cfg, string key, params (string Name, string Value)[] vars)
    {
        if (cfg.Mayor.TemplateId == 0 || !cfg.Mayor.Lines.TryGetValue(key, out var lines) || lines.Length == 0) return;
        if (!world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var map)) return;
        var mayor = map.NPCs.FirstOrDefault(n => n.NPCTemplateID == cfg.Mayor.TemplateId && n.State == NPC.States.Alive && !n.Hidden);
        if (mayor is null) return;
        world.SendToMap(map, P.Chat(mayor.LoginID, mayor.Name, Fill(lines[world.Random.Next(lines.Length)], vars)));
    }

    public static void MayorMaybe(GameWorld world, ArenaConfig cfg, double chance, string key, params (string Name, string Value)[] vars)
    {
        if (world.Random.NextDouble() < chance) Mayor(world, cfg, key, vars);
    }

    public static void CrowdTick(GameWorld world, ArenaConfig cfg, ArenaRun run, DateTime nowUtc)
    {
        var shouts = cfg.Crowd;
        if (shouts.EverySeconds.Length != 2) return;
        if (run.NextCrowdUtc == default)
        {
            run.NextCrowdUtc = nowUtc.AddSeconds(NextDelay(world, shouts));
            ArenaState.Save(world, run);
            return;
        }
        if (nowUtc < run.NextCrowdUtc) return;
        run.NextCrowdUtc = nowUtc.AddSeconds(NextDelay(world, shouts));
        ArenaState.Save(world, run);

        if (!world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var map)) return;
        var targets = map.Players
            .Where(p => !p.IsGM && run.Participants.TryGetValue(p.PlayerID, out var part) && part.Alive)
            .ToList();
        if (targets.Count == 0) return;
        var target = targets[world.Random.Next(targets.Count)];

        bool boo = world.Random.NextDouble() < shouts.BooChance;
        var lines = boo ? shouts.Boos : shouts.Cheers;
        if (lines.Length == 0) return;

        var near = map.NPCs
            .Where(n => n.State == NPC.States.Alive && !n.Hidden && IsCrowd(n, cfg))
            .OrderBy(n => Math.Max(Math.Abs(n.MapX - target.MapX), Math.Abs(n.MapY - target.MapY)))
            .Take(shouts.NearestCrowd)
            .ToList();
        if (near.Count == 0) return;
        var fan = near[world.Random.Next(near.Count)];

        string packet = P.Chat(fan.LoginID, fan.Name, Fill(lines[world.Random.Next(lines.Length)], ("player", target.Name)));
        foreach (var player in map.GetPlayersInRange(fan))
            world.Send(player, packet);
    }

    private static bool IsCrowd(NPC npc, ArenaConfig cfg)
        => npc.NPCTemplateID != cfg.Mayor.TemplateId
           && !npc.Properties.ContainsKey("arena.run")
           && npc.Script?.Object.GetType().Name == "ArenaCrowd";

    private static double NextDelay(GameWorld world, ArenaCrowdShouts shouts)
        => shouts.EverySeconds[0] + world.Random.NextDouble() * Math.Max(0, shouts.EverySeconds[1] - shouts.EverySeconds[0]);

    private static string Fill(string line, params (string Name, string Value)[] vars)
    {
        foreach (var (name, value) in vars)
            line = line.Replace("{" + name + "}", value);
        return line;
    }
}
