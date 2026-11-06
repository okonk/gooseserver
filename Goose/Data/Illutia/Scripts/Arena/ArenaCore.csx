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

    public static void Tick(GameWorld world, DateTime nowUtc)
    {
        var run = ArenaState.Load(world);
        if (run is null) return;
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Enabled(world)) return;

        switch (run.Phase)
        {
            case ArenaPhase.Countdown when nowUtc >= run.PhaseUntilUtc:
                StartRound(world, cfg, run, 1, nowUtc);
                break;
            case ArenaPhase.Fighting:
                TickFighting(world, cfg, run, nowUtc);
                break;
            case ArenaPhase.Break when nowUtc >= run.PhaseUntilUtc:
                StartRound(world, cfg, run, run.Round + 1, nowUtc);
                break;
        }
    }

    public static void OnNpcKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)
    {
        if (!npc.Properties.TryGetProperty<string>("arena.run", out var runId)) return;
        var run = ArenaState.Load(world);
        if (run is null || run.RunId != runId) return;
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Valid) return;

        foreach (var p in credited)
            if (run.Participants.TryGetValue(p.PlayerID, out var part))
                part.Kills++;
        ArenaState.Save(world, run);

        const string DmgPrefix = "arena.dmg.";
        var dmg = new Dictionary<int, long>();
        foreach (var key in npc.Properties.Keys)
        {
            if (!key.StartsWith(DmgPrefix)) continue;
            if (!int.TryParse(key.Substring(DmgPrefix.Length), out var id)) continue;
            if (run.Participants.ContainsKey(id))
                dmg[id] = npc.Properties.GetProperty<long>(key, 0);
        }

        var split = ArenaMath.SplitBounty(ArenaMath.Bounty((MobTier)npc.Properties.GetProperty<int>("arena.tier", 0), cfg), dmg);
        foreach (var (id, amount) in split)
            Pay(world, id, amount, "kill bounty");
    }

    private static void TickFighting(GameWorld world, ArenaConfig cfg, ArenaRun run, DateTime nowUtc)
    {
        var mobs = AliveArenaMobs(world, cfg, run.RunId);
        if (mobs.Count == 0)
        {
            foreach (var p in run.Participants.Values)
                if (p.Alive) p.RoundsCleared = run.Round;
            ArenaState.Save(world, run);
            foreach (var p in run.Participants.Values)
                if (p.Alive) Pay(world, p.Id, ArenaMath.RoundPayout(run.Round, cfg), "round " + run.Round + " clear");
            run.Phase = ArenaPhase.Break;
            run.PhaseUntilUtc = nowUtc.AddSeconds(cfg.BreakSeconds);
            ArenaState.Save(world, run);
            return;
        }

        if (nowUtc >= run.PhaseUntilUtc)
        {
            EndRun(world, "time ran out on round " + run.Round);
            return;
        }

        foreach (var npc in mobs)
        {
            var target = npc.AggroTarget;
            if (target is not null && !IsAliveParticipant(run, target))
                npc.RemoveAggro(target);
            if (npc.AggroTarget is null || !IsAliveParticipant(run, npc.AggroTarget))
            {
                var nearest = NearestAliveParticipant(world, cfg, run, npc.MapX, npc.MapY);
                if (nearest is not null)
                    npc.AddAggro(nearest, 1, world);
            }
        }

        long left = (long)Math.Floor((run.PhaseUntilUtc - nowUtc).TotalSeconds);
        if (left <= cfg.TimeoutWarningSeconds && !run.Warned)
        {
            if (world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var arenaMap))
            {
                world.SendToMap(arenaMap, P.ServerMessage("[Arena] " + mobs.Count + " enemies remain! " + Math.Max(0, left) + "s left!"));
                if (mobs.Count <= cfg.StragglerCount)
                {
                    var alive = run.Participants.Values.Where(p => p.Alive).ToList();
                    foreach (var npc in mobs)
                    {
                        if (alive.Count == 0) break;
                        var player = FindPlayer(world, alive[world.Random.Next(alive.Count)].Id);
                        if (player is null) continue;
                        for (int i = 0; i < SpawnAttempts; i++)
                        {
                            int x = player.MapX + world.Random.Next(-2, 3);
                            int y = player.MapY + world.Random.Next(-2, 3);
                            if (!arenaMap.IsTileBlocked(null, x, y))
                            {
                                npc.Hide(world);
                                npc.Reveal(world, x, y);
                                break;
                            }
                        }
                    }
                }
            }
            run.Warned = true;
            ArenaState.Save(world, run);
        }
    }

    private static void StartRound(GameWorld world, ArenaConfig cfg, ArenaRun run, int n, DateTime nowUtc)
    {
        int alive = run.Participants.Values.Count(p => p.Alive);
        if (alive == 0)
        {
            EndRun(world, "no participants remain");
            return;
        }

        int count = ArenaMath.WaveCount(n, alive, cfg);
        var band = ArenaMath.BandFor(n, cfg);
        bool isBoss = ArenaMath.IsBossRound(n, cfg);

        var plan = new List<(MobTier Tier, int TemplateId)>();
        if (isBoss) plan.Add((MobTier.Boss, band.Boss));
        while (plan.Count < count)
        {
            var tier = world.Random.NextDouble() < ArenaMath.EliteChance(n, cfg) ? MobTier.Elite : MobTier.Normal;
            plan.Add((tier, band.Normals[world.Random.Next(band.Normals.Length)]));
        }

        foreach (var (tier, templateId) in plan)
        {
            var template = world.NPCHandler.GetNPCTemplate(templateId);
            if (template is null)
            {
                log.Error("Arena mob template {0} not found; mob skipped", templateId);
                continue;
            }
            var tile = PickSpawnTile(world, cfg, run);
            if (tile is null) continue;
            var npc = world.NPCHandler.SpawnNPC(world, cfg.ArenaMap, tile.Value.X, tile.Value.Y, template, false,
                new PropertiesDictionary
                {
                    ["arena.run"] = run.RunId,
                    ["arena.round"] = n,
                    ["arena.tier"] = (int)tier,
                    ["arena.alive"] = alive,
                });
            if (npc is null) continue;
            var nearest = NearestAliveParticipant(world, cfg, run, tile.Value.X, tile.Value.Y);
            if (nearest is not null)
                npc.AddAggro(nearest, 1, world);
        }

        run.Round = n;
        run.Phase = ArenaPhase.Fighting;
        run.PhaseUntilUtc = nowUtc.AddSeconds(cfg.RoundSeconds);
        run.Warned = false;
        ArenaState.Save(world, run);

        if (world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var arenaMap))
            world.SendToMap(arenaMap, P.ServerMessage(isBoss ? "[Arena] Boss round!" : "[Arena] Round " + n + "!"));
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

    private static List<NPC> AliveArenaMobs(GameWorld world, ArenaConfig cfg, string runId)
    {
        if (!world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var map)) return [];
        return map.NPCs
            .Where(npc => npc.State == NPC.States.Alive && npc.Properties.TryGetProperty<string>("arena.run", out var tag) && tag == runId)
            .ToList();
    }

    private static bool IsAliveParticipant(ArenaRun run, Player p)
        => run.Participants.TryGetValue(p.PlayerID, out var part) && part.Alive;

    private static Player? NearestAliveParticipant(GameWorld world, ArenaConfig cfg, ArenaRun run, int x, int y)
    {
        Player? best = null;
        int bestDist = int.MaxValue;
        foreach (var p in world.PlayerHandler.Players)
        {
            if (p.MapID != cfg.ArenaMap || !IsAliveParticipant(run, p)) continue;
            int dist = Chebyshev(x, y, p.MapX, p.MapY);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = p;
            }
        }
        return best;
    }

    private static (int X, int Y)? PickSpawnTile(GameWorld world, ArenaConfig cfg, ArenaRun run)
    {
        if (!world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var map)) return null;
        var players = world.PlayerHandler.Players
            .Where(p => p.MapID == cfg.ArenaMap && IsAliveParticipant(run, p))
            .ToList();
        for (int i = 0; i < SpawnAttempts; i++)
        {
            int x = cfg.ArenaRect[0] + world.Random.Next(cfg.ArenaRect[2]);
            int y = cfg.ArenaRect[1] + world.Random.Next(cfg.ArenaRect[3]);
            if (!map.IsTileBlocked(null, x, y) && FarEnough(x, y)) return (x, y);
        }
        for (int i = 0; i < SpawnAttempts; i++)
        {
            int x = cfg.ArenaRect[0] + world.Random.Next(cfg.ArenaRect[2]);
            int y = cfg.ArenaRect[1] + world.Random.Next(cfg.ArenaRect[3]);
            if (!map.IsTileBlocked(null, x, y)) return (x, y);
        }
        return null;

        bool FarEnough(int x, int y)
            => players.All(p => Chebyshev(x, y, p.MapX, p.MapY) >= cfg.MinPlayerDistance);
    }

    private static int Chebyshev(int x1, int y1, int x2, int y2)
        => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));

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
