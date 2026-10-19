#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
#load "../Global/Halloween/HalloweenBoss.csx"
using System;
using System.Linq;
using Goose;
using Goose.Events;
using Goose.Scripting;

public class GroundskeeperState
{
    public int SwingCount { get; set; }
    public bool BigHitPending { get; set; }
    public long LastPlayerSeenAt { get; set; }
    public long NextJumpAt { get; set; }
    public long NextDarkAt { get; set; }
}

public class Groundskeeper : BaseNPCScript
{
    public override void OnSpawnEvent(NPC npc, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        state.LastPlayerSeenAt = world.TimeNow;
        state.NextJumpAt = world.TimeNow + Halloween.Ticks(world, cfg.Groundskeeper.JumpSeconds);
        npc.Properties["gkPhase"] = 0;
        Halloween.AddPermanentBuff(npc, cfg.Effects.GroundskeeperCurve, world);
        Rescale(npc, 1.0, world);
        Announce(world, "[Event] The Groundskeeper has risen in the basement!");
        // A /reloadscripts swap replaces Script.Object with a freshly compiled type, so the
        // timer closure must not call this compilation by typed reference; re-resolve and
        // invoke EmptyCheck reflectively.
        ScriptTimerEvent timer = null!;
        timer = ScriptTimerEvent.Create(() =>
        {
            if (npc.State != NPC.States.Alive) return;
            try
            {
                var current = npc.Script?.Object;
                if (current is not null)
                    current.GetType().GetMethod("EmptyCheck")!.Invoke(current, new object?[] { npc, world.TimeNow, world });
            }
            finally
            {
                // ScriptTimerEvent.Ready swallows exceptions, so reschedule in a finally
                // or one bad tick kills the empty-despawn chain permanently.
                timer.Reschedule(TimeSpan.FromSeconds(5), world);
            }
        }, TimeSpan.FromSeconds(5), world);
    }

    public override void OnAttackEvent(NPC npc, GameWorld world)
    {
        AttackStep(npc, world.TimeNow, world);
        npc.AddAttackEvent(world);
    }

    public override void OnKilledEvent(NPC npc, ICharacter killer, GameWorld world)
    {
        DespawnAdds(npc, world);
        Announce(world, "[Event] The Groundskeeper has fallen!");
    }

    public void Rescale(NPC npc, double fraction, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        int players = Math.Max(Math.Max(1, PlayersNear(npc)), PeakPlayers(npc));
        npc.Properties["gkPeakPlayers"] = players;
        npc.TemporaryMaxHP = cfg.Groundskeeper.HpPerPlayer * players;
        npc.CurrentHP = (long)(fraction * npc.MaxHP);
        foreach (var p in npc.Map.GetPlayersInRange(npc))
            world.Send(p, P.VitalsPercentage(npc));
    }

    public void GrowCheck(NPC npc, GameWorld world)
    {
        if (PlayersNear(npc) <= PeakPlayers(npc)) return;
        Rescale(npc, (double)npc.CurrentHP / npc.MaxHP, world);
    }

    private static int PlayersNear(NPC npc) => HalloweenBoss.PlayersNear(npc, Map.RANGE_X).Count;

    private static int PeakPlayers(NPC npc) =>
        npc.Properties.TryGetProperty<int>("gkPeakPlayers", out var peak) ? peak : 0;

    public void AttackStep(NPC npc, long now, GameWorld world)
    {
        CheckPhases(npc, world);
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        FinalDarkCheck(npc, state, cfg, now, world);
        if (npc.Buffs.Any(b => b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)) return;
        if (cfg.Groundskeeper.JumpSeconds > 0 && now >= state.NextJumpAt)
        {
            state.NextJumpAt = now + Halloween.Ticks(world, cfg.Groundskeeper.JumpSeconds);
            Vanish(npc, world);
        }
        if (npc.AggroTarget is not Player target || npc.Map != target.Map) return;
        if (Math.Abs(npc.MapX - target.MapX) > npc.AttackRange || Math.Abs(npc.MapY - target.MapY) > npc.AttackRange) return;
        state.SwingCount++;
        if (state.SwingCount % cfg.Groundskeeper.BigHitEvery == 0 && !state.BigHitPending)
        {
            state.BigHitPending = true;
            string packet = P.Notice(NoticeType.Info, "The Groundskeeper revs his chainsaw!");
            foreach (var p in npc.Map.GetPlayersInRange(npc))
                world.Send(p, packet);
            // Same reload-safe pattern as the empty-check timer: re-resolve and invoke
            // BigHit reflectively.
            ScriptTimerEvent.Create(() =>
            {
                var current = npc.Script?.Object;
                if (current is not null)
                    current.GetType().GetMethod("BigHit")!.Invoke(current, new object?[] { npc, world });
            }, TimeSpan.FromSeconds(cfg.Groundskeeper.BigHitWarnSeconds), world);
            return;
        }
        double percent = cfg.Groundskeeper.HitPercent * (HalloweenBoss.IsStraggler(target, cfg.Weeper.StragglerRadius)
            ? cfg.Weeper.StragglerMultiplier : 1);
        HalloweenBoss.PercentHit(npc, target, percent, world);
    }

    public void BigHit(NPC npc, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        state.BigHitPending = false;
        if (npc.State != NPC.States.Alive) return;
        var targets = npc.Map.Players.OfType<Player>()
            .Where(p => p.State == Player.States.Ready && !p.IsGM &&
                Math.Max(Math.Abs(p.MapX - npc.MapX), Math.Abs(p.MapY - npc.MapY)) <= npc.AttackRange)
            .ToList();
        foreach (var p in targets)
            HalloweenBoss.PercentHit(npc, p, cfg.Groundskeeper.BigHitPercent, world);
    }

    public void CheckPhases(NPC npc, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        var phases = cfg.Groundskeeper.Phases;
        int phase = npc.Properties.TryGetProperty<int>("gkPhase", out var p) ? p : 0;
        double fraction = (double)npc.CurrentHP / npc.MaxHP;
        while (phase < phases.Length && fraction <= phases[phase])
        {
            RunPhase(npc, phase, fraction, world);
            phase++;
            npc.Properties["gkPhase"] = phase;
        }
    }

    public void EmptyCheck(NPC npc, long now, GameWorld world)
    {
        if (npc.State != NPC.States.Alive) return;
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        bool present = npc.Map.Players.OfType<Player>().Any(p => p.State == Player.States.Ready && !p.IsGM);
        if (present)
        {
            state.LastPlayerSeenAt = now;
            GrowCheck(npc, world);
            return;
        }
        if (state.LastPlayerSeenAt == 0)
        {
            // A /reloadscripts swap rebuilds State, so 0 means uninitialized; start a fresh window.
            state.LastPlayerSeenAt = now;
            return;
        }
        if (now - state.LastPlayerSeenAt < Halloween.Ticks(world, cfg.Groundskeeper.EmptyDespawnSeconds)) return;
        world.SendToMap(npc.Map, P.Notice(NoticeType.Info, "The Groundskeeper returns to the dark."));
        DespawnAdds(npc, world);
        npc.Despawn(world);
    }

    private void FinalDarkCheck(NPC npc, GroundskeeperState state, HalloweenConfig cfg, long now, GameWorld world)
    {
        if (cfg.Groundskeeper.FinalLightsOutEverySeconds is not [var min, var max]) return;
        int phase = npc.Properties.TryGetProperty<int>("gkPhase", out var p) ? p : 0;
        if (phase < 3) return;
        int seconds = FinalLightsOutSeconds(cfg);
        if (state.NextDarkAt != 0 && now < state.NextDarkAt) return;
        if (state.NextDarkAt != 0) LightsOut(npc, world, seconds);
        state.NextDarkAt = now + Halloween.Ticks(world, seconds + Random.Shared.Next(min, max + 1));
    }

    private static int FinalLightsOutSeconds(HalloweenConfig cfg) =>
        cfg.Groundskeeper.FinalLightsOutSeconds > 0 ? cfg.Groundskeeper.FinalLightsOutSeconds : cfg.Groundskeeper.LightsOutSeconds;

    public void LightsOut(NPC npc, GameWorld world, int seconds)
    {
        var cfg = HalloweenConfig.Get(world);
        var map = npc.Map;
        var ambience = map.Ambience;
        int alpha = cfg.Groundskeeper.LightsOutAlpha;
        world.SendToMap(map, P.MapAmbience(alpha, alpha, ambience.Lighting.TintColor, ambience.Lighting.FillColor, 0, 0, ambience.CurrentWeather));
        // Engine-only action (MapAmbience is engine code), so no script re-resolution is
        // needed in this closure.
        ScriptTimerEvent.Create(() => map.Ambience.Broadcast(map, world),
            TimeSpan.FromSeconds(seconds), world);
    }

    private void RunPhase(NPC npc, int phase, double fraction, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        switch (phase)
        {
            case 0:
                Rescale(npc, fraction, world);
                LightsOut(npc, world, cfg.Groundskeeper.LightsOutSeconds);
                Vanish(npc, world);
                SpawnAdds(npc, "placeSettings", world);
                break;
            case 1:
                Rescale(npc, fraction, world);
                LightsOut(npc, world, cfg.Groundskeeper.LightsOutSeconds);
                Vanish(npc, world);
                SpawnAdds(npc, "ghosts", world);
                break;
            case 2:
                Rescale(npc, fraction, world);
                LightsOut(npc, world, FinalLightsOutSeconds(cfg));
                Vanish(npc, world);
                SpawnAdds(npc, "bloody", world);
                break;
            case 3:
                npc.AttackSpeed = cfg.Groundskeeper.EnrageAttackSpeed;
                break;
        }
    }

    private void Vanish(NPC npc, GameWorld world)
    {
        var map = npc.Map;
        var players = map.Players.OfType<Player>()
            .Where(p => p.State == Player.States.Ready && !p.IsGM).ToList();
        if (players.Count == 0) return;
        var target = players[world.Random.Next(players.Count)];
        (int X, int Y)? tile = TileBehind(map, target);
        tile ??= FreeAdjacentTile(map, target);
        if (tile is null) return;
        npc.MoveTo(world, tile.Value.X, tile.Value.Y);
        npc.AddAggro(target, npc.AggroValue.Damage + npc.AggroValue.Taunt + 1, wastaunt: true, world);
    }

    private void SpawnAdds(NPC npc, string kind, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (!cfg.Groundskeeper.Adds.TryGetValue(kind, out var entries) || entries is null) return;
        var map = npc.Map;
        foreach (var entry in entries)
        {
            if (entry is not { Length: 2 } || entry[0] == 0) continue;
            var template = world.NPCHandler.GetNPCTemplate(entry[0]);
            if (template is null) continue;
            for (int i = 0; i < entry[1]; i++)
            {
                var tile = FreeTileNear(map, world, npc.MapX, npc.MapY, 6);
                if (tile is null) return;
                var add = world.NPCHandler.SpawnNPC(world, map.ID, tile.Value.X, tile.Value.Y, template,
                    shouldRespawn: false, new PropertiesDictionary { ["gkAdd"] = kind });
                if (add is null) continue;
                if (kind is "ghosts")
                    Halloween.AddPermanentBuff(add, cfg.Effects.GhostSnareOnHit, world);
                AggroAdd(add, world);
            }
        }
    }

    private static (int X, int Y)? TileBehind(Map map, Player target)
    {
        var (dx, dy) = target.Facing switch
        {
            Direction.Up => (0, 1),
            Direction.Down => (0, -1),
            Direction.Right => (-1, 0),
            Direction.Left => (1, 0),
            _ => (0, 1),
        };
        int x = target.MapX + dx;
        int y = target.MapY + dy;
        return map.IsTileBlocked(null, x, y) ? null : (x, y);
    }

    private static (int X, int Y)? FreeAdjacentTile(Map map, Player target)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int x = target.MapX + dx;
                int y = target.MapY + dy;
                if (!map.IsTileBlocked(null, x, y)) return (x, y);
            }
        return null;
    }

    private static (int X, int Y)? FreeTileNear(Map map, GameWorld world, int x, int y, int radius)
    {
        List<(int X, int Y)> free = [];
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                int tx = x + dx;
                int ty = y + dy;
                if (!map.IsTileBlocked(null, tx, ty)) free.Add((tx, ty));
            }
        if (free.Count == 0) return null;
        return free[world.Random.Next(free.Count)];
    }

    private static void AggroAdd(NPC add, GameWorld world)
    {
        var players = HalloweenBoss.PlayersNear(add, 6);
        if (players.Count == 0) return;
        add.AddAggro(players[world.Random.Next(players.Count)], 1, wastaunt: true, world);
    }

    private static void DespawnAdds(NPC npc, GameWorld world)
    {
        foreach (var add in npc.Map.NPCs.ToList())
            if (add != npc && add.State == NPC.States.Alive && add.Properties.TryGetProperty<string>("gkAdd", out _))
                add.Despawn(world);
    }

    private static void Announce(GameWorld world, string message)
    {
        string packet = P.Notice(NoticeType.Info, message);
        foreach (var p in world.PlayerHandler.Players)
            if (p.State == Player.States.Ready)
                world.Send(p, packet);
    }

    private static GroundskeeperState State(NPC npc)
    {
        if (npc.ScriptStore is GroundskeeperState state) return state;
        state = new GroundskeeperState();
        npc.ScriptStore = state;
        return state;
    }
}

return typeof(Groundskeeper);
