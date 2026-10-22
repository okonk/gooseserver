#load "Halloween/HalloweenConfig.csx"
#load "Halloween/HalloweenCommon.csx"
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Goose;
using Goose.Scripting;

public class HalloweenInvasionController : BaseGlobalScript
{
    private const string FileName = "HalloweenInvasion.csx";
    private static readonly TimeSpan TickPeriod = TimeSpan.FromSeconds(60);
    private const int SpawnAttempts = 50;

    private long nextAt;

    public override void OnLoaded(GameWorld world)
    {
        ScriptTimerEvent? timer = null;
        timer = ScriptTimerEvent.Create(() =>
        {
            try
            {
                TickCurrent(world, world.TimeNow, world.Random);
            }
            finally
            {
                timer!.Reschedule(TickPeriod, world);
            }
        }, TickPeriod, world);
    }

    public void Tick(GameWorld world, long now, Random rng)
    {
        DespawnExpired(world, now);
        var cfg = HalloweenConfig.Get(world);
        if (!Halloween.Active(world) || !Halloween.Tier(world, 3)) return;
        if (nextAt != 0 && now < nextAt) return;
        nextAt = now + Halloween.Ticks(world, cfg.Invasion.EveryMinutes * 60.0);
        SpawnWave(world, now, rng);
    }

    public void DespawnExpired(GameWorld world, long now)
    {
        var cfg = HalloweenConfig.Get(world);
        long lifetime = Halloween.Ticks(world, cfg.Invasion.LifetimeMinutes * 60.0);
        foreach (var map in Maps(world))
            foreach (var npc in map.NPCs.ToList())
                if (IsInvasion(npc) && npc.State == NPC.States.Alive &&
                    npc.Properties.TryGetProperty<long>("spawnedAt", out long spawnedAt) &&
                    now >= spawnedAt + lifetime)
                    npc.Despawn(world);
    }

    public void SpawnWave(GameWorld world, long now, Random rng)
    {
        var cfg = HalloweenConfig.Get(world);
        var invasion = cfg.Invasion;
        if (invasion.Towns.Count == 0 || invasion.Templates.Length == 0 || invasion.Count <= 0) return;
        var town = invasion.Towns[rng.Next(invasion.Towns.Count)];
        if (!world.MapHandler.Maps.TryGetValue(town.Map, out var map) || map is null) return;

        foreach (var m in Maps(world))
            foreach (var npc in m.NPCs.ToList())
                if (IsInvasion(npc) && npc.State == NPC.States.Alive)
                    npc.Despawn(world);

        for (int i = 0; i < invasion.Count; i++)
        {
            var template = world.NPCHandler.GetNPCTemplate(invasion.Templates[rng.Next(invasion.Templates.Length)]);
            if (template is null) continue;
            var tile = FreeTile(map, town.Rect, rng);
            if (tile is null) continue;
            var npc = world.NPCHandler.SpawnNPC(world, map.ID, tile.Value.X, tile.Value.Y, template,
                shouldRespawn: false,
                new PropertiesDictionary
                {
                    ["invasion"] = 1,
                    ["spawnedAt"] = now,
                    ["canMove"] = true,
                });
            if (npc is not null && Home(map, town, tile.Value, rng) is { } home)
            {
                npc.SpawnX = home.X;
                npc.SpawnY = home.Y;
            }
        }

        world.SendToAll(P.ServerMessage("[Event] The party has spilled into " + map.Name + "!"));
    }

    private static (int X, int Y)? FreeTile(Map map, int[] rect, Random rng)
    {
        for (int i = 0; i < SpawnAttempts; i++)
        {
            int x = rect[0] + rng.Next(rect[2]);
            int y = rect[1] + rng.Next(rect[3]);
            if (!map.IsTileBlocked(null, x, y)) return (x, y);
        }
        return null;
    }

    private static (int X, int Y)? Home(Map map, HalloweenTown town, (int X, int Y) from, Random rng)
    {
        if (town.Center is not { Length: 2 } center) return null;
        for (int i = 0; i < SpawnAttempts; i++)
        {
            int x = center[0] + rng.Next(-town.HomeRadius, town.HomeRadius + 1);
            int y = center[1] + rng.Next(-town.HomeRadius, town.HomeRadius + 1);
            if (map.FindPath(from.X, from.Y, x, y) is not null) return (x, y);
        }
        return null;
    }

    private static bool IsInvasion(NPC npc) => npc.Properties.TryGetProperty<int>("invasion", out _);

    private static List<Map> Maps(GameWorld world)
    {
        var maps = new List<Map>();
        foreach (var map in world.MapHandler.Maps.Values)
            if (map is not null) maps.Add(map);
        return maps;
    }

    // A /reloadscripts swap replaces Object with an instance of a freshly compiled type, so
    // Tick cannot be called statically typed; re-resolve through the registry and invoke.
    private static void TickCurrent(GameWorld world, long now, Random rng)
    {
        var current = Current(world);
        if (current is null) return;
        current.GetType().GetMethod("Tick")!.Invoke(current, new object?[] { world, now, rng });
    }

    private static IGlobalScript? Current(GameWorld world)
    {
        foreach (var script in world.ScriptHandler.GetGlobalScripts())
            if (Path.GetFileName(script.FilePath) == FileName)
                return script.Object;
        return null;
    }
}

return typeof(HalloweenInvasionController);
