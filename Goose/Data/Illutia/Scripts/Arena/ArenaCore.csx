#load "ArenaConfig.csx"
#load "ArenaMath.csx"
#load "ArenaState.csx"
using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Scripting;

// Each .csx compiles into its own assembly, so statics are not shared between arena scripts; all shared state therefore goes through WorldState JSON strings (ArenaState).
public static class ArenaCore
{
    private const int SpawnAttempts = 50;

    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
    private static string? currencyWarned;

    public static void StartRun(GameWorld world, DateTime nowUtc)
    {
        if (ArenaState.Load(world) is not null) return;
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Enabled(world)) return;
        if (cfg.ArenaRect[2] <= 0 || cfg.ArenaRect[3] <= 0)
        {
            log.Error("Arena rect {0} has no positive dimensions; run not started", string.Join(",", cfg.ArenaRect));
            return;
        }

        var lobby = world.PlayerHandler.Players
            .Where(p => !p.IsGM && p.State == Player.States.Ready && p.Map is not null && p.Map.ID == cfg.LobbyMap)
            .ToList();
        if (lobby.Count == 0) return;

        var run = new ArenaRun
        {
            RunId = Guid.NewGuid().ToString("N"),
            StartedUtc = nowUtc,
            Phase = ArenaPhase.Countdown,
            PhaseUntilUtc = nowUtc.AddSeconds(cfg.CountdownSeconds),
            Round = 0,
            Warned = false,
            Participants = lobby.ToDictionary(p => p.PlayerID, p => new ArenaParticipant
            {
                Id = p.PlayerID,
                Name = p.Name,
                Alive = true,
            }),
        };
        ArenaState.Save(world, run);

        var arenaMap = world.MapHandler.Maps[cfg.ArenaMap];
        foreach (var player in lobby)
        {
            var (x, y) = PickFreeTile(world, arenaMap, cfg.ArenaRect);
            player.WarpTo(world, arenaMap, x, y);
        }

        world.SendToAll(P.ServerMessage("[Arena] The arena is open. Get ready!"));
    }

    public static void Eliminate(GameWorld world, int playerId, string reason)
    {
        var run = ArenaState.Load(world);
        if (run is null) return;
        if (!run.Participants.TryGetValue(playerId, out var p) || !p.Alive) return;

        p.Alive = false;
        ArenaState.Save(world, run);

        var player = FindPlayer(world, playerId);
        if (player is not null)
            world.Send(player, P.ServerMessage("[Arena] You were eliminated (" + reason + "). Rounds cleared: " + p.RoundsCleared + ". Arena Coins: " + p.Coins + "."));

        EndIfEmpty(world, DateTime.UtcNow);
    }

    public static void EndIfEmpty(GameWorld world, DateTime nowUtc)
    {
        var run = ArenaState.Load(world);
        if (run is null) return;
        if (!run.Participants.Values.Any(p => p.Alive))
            EndRun(world, "the last participant was eliminated");
    }

    public static void EndRun(GameWorld world, string reason)
    {
        var run = ArenaState.Load(world);
        if (run is null) return;
        var cfg = ArenaConfig.Get(world);

        if (world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var arenaMap))
        {
            foreach (var npc in arenaMap.NPCs.ToList())
            {
                if (npc.State == NPC.States.Alive &&
                    npc.Properties.TryGetProperty<string>("arena.run", out var tag) && tag == run.RunId)
                    npc.Despawn(world);
            }
        }

        foreach (var p in run.Participants.Values)
            p.Alive = false;
        ArenaState.Save(world, run);

        foreach (var p in run.Participants.Values)
        {
            var player = FindPlayer(world, p.Id);
            if (player is not null && player.Map is not null && player.Map.ID == cfg.ArenaMap &&
                world.MapHandler.Maps.TryGetValue(cfg.LobbyMap, out var lobbyMap))
                player.WarpTo(world, lobbyMap, cfg.LobbySpawn[0], cfg.LobbySpawn[1]);
        }

        foreach (var p in run.Participants.Values)
            Pay(world, p.Id, ArenaMath.MilestoneBonus(p.RoundsCleared, cfg), "milestone bonus");

        run = ArenaState.Load(world) ?? run;

        var bestRound = run.Participants.Values.Select(p => p.RoundsCleared).DefaultIfEmpty(0).Max();
        ArenaState.SaveLastRun(world, new ArenaLastRun
        {
            RunId = run.RunId,
            EndedUtc = DateTime.UtcNow,
            BestRound = bestRound,
            Results = run.Participants.Values
                .Select(p => new ArenaResult
                {
                    Name = p.Name,
                    RoundsCleared = p.RoundsCleared,
                    Coins = p.Coins,
                    Kills = p.Kills,
                })
                .ToList(),
        });

        RecordResults(world, run);
        ArenaState.Clear(world);
        world.SendToAll(P.ServerMessage("[Arena] The arena is closed. Best round: " + bestRound + "."));
    }

    public static void Pay(GameWorld world, int playerId, long amount, string reason)
    {
        var run = ArenaState.Load(world);
        if (run is null || amount <= 0 || !run.Participants.TryGetValue(playerId, out var p)) return;

        p.Coins += amount;
        ArenaState.Save(world, run);

        var player = FindPlayer(world, playerId);
        if (player is null) return;

        var cfg = ArenaConfig.Get(world);
        var currency = world.CurrencyHandler.Get(cfg.CurrencyId);
        if (currency is null)
        {
            if (currencyWarned != cfg.CurrencyId)
            {
                currencyWarned = cfg.CurrencyId;
                log.Warn("Arena currency {0} not found; coin payouts skipped", cfg.CurrencyId);
            }
            return;
        }

        currency.Add(player, amount, world);
        world.Send(player, P.ServerMessage("+" + amount + " Arena Coins (" + reason + ")"));
    }

    public static bool IsParticipant(GameWorld world, int playerId)
    {
        var run = ArenaState.Load(world);
        return run is not null && run.Participants.TryGetValue(playerId, out var p) && p.Alive;
    }

    public static void RecordResults(GameWorld world, ArenaRun run)
    {
    }

    public static Player? FindPlayer(GameWorld world, int playerId)
        => world.PlayerHandler.Players.FirstOrDefault(p => p.PlayerID == playerId);

    private static (int X, int Y) PickFreeTile(GameWorld world, Map map, int[] rect)
    {
        for (int i = 0; i < SpawnAttempts; i++)
        {
            int x = rect[0] + world.Random.Next(rect[2]);
            int y = rect[1] + world.Random.Next(rect[3]);
            if (!map.IsTileBlocked(null, x, y)) return (x, y);
        }
        return (rect[0] + rect[2] / 2, rect[1] + rect[3] / 2);
    }
}
