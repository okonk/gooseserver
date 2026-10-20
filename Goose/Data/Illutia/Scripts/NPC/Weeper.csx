#load "../Global/Halloween/HalloweenConfig.csx"
#load "../Global/Halloween/HalloweenCommon.csx"
#load "../Global/Halloween/HalloweenBoss.csx"
using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Events;
using Goose.Scripting;

public class WeeperState : BossState
{
    public long LastCombatAt { get; set; }
    public long NextLungeAt { get; set; }
    public long NextFootstepsAt { get; set; }
    public long LeashStartedAt { get; set; }
    public int LeftRouteAtX { get; set; }
    public int LeftRouteAtY { get; set; }
    public long HiddenUntil { get; set; }
}

public class Weeper : BaseNPCScript
{
    private const int StuckStepsBeforeSkip = 5;
    private const int ApproachSearchLimit = 600;

    public override void OnSpawnEvent(NPC npc, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        state.NextFootstepsAt = world.TimeNow + Halloween.Ticks(world, cfg.Weeper.FootstepSeconds);
        Halloween.AddPermanentBuff(npc, cfg.Effects.WeeperCap, world);
        string? last = npc.Properties.TryGetProperty<string>("weeperLastLoop", out var l) ? l : null;
        npc.Properties["weeperLoop"] = PickLoop(cfg, world, last);
        npc.Properties["weeperWaypoint"] = 0;
        if (world.MapHandler.Maps.TryGetValue(cfg.Maps.House, out var house) && house is not null)
            world.SendToMap(house, P.Notice(NoticeType.Info, "[Event] A low weeping echoes through the house…"));
        // A /reloadscripts swap replaces Script.Object with a freshly compiled type, so the
        // timer closure re-resolves and invokes Tick reflectively, rescheduling in a finally.
        var tickPeriod = TimeSpan.FromSeconds(cfg.Weeper.BleedTickSeconds);
        ScriptTimerEvent timer = null!;
        timer = ScriptTimerEvent.Create(() =>
        {
            if (npc.State != NPC.States.Alive) return;
            try
            {
                var current = npc.Script?.Object;
                if (current is not null)
                    current.GetType().GetMethod("Tick")!.Invoke(current, new object?[] { npc, world.TimeNow, world });
            }
            finally
            {
                timer.Reschedule(tickPeriod, world);
            }
        }, tickPeriod, world);
    }

    public void Tick(NPC npc, long now, GameWorld world)
    {
        if (npc.Hidden && now >= State(npc).HiddenUntil) Emerge(npc, world);
        HalloweenBoss.TickBleeds(npc, now, world);
        FootstepsCheck(npc, now, world);
    }

    public override void OnMoveEvent(NPC npc, GameWorld world)
    {
        MoveStep(npc, world.TimeNow, world);
        npc.AddMoveEvent(world);
    }

    public override void OnAttackEvent(NPC npc, GameWorld world)
    {
        AttackStep(npc, world.TimeNow, world);
        npc.AddAttackEvent(world);
    }

    public override void OnAttackedEvent(NPC npc, ICharacter attacker, long damage, GameWorld world)
    {
        State(npc).LastCombatAt = world.TimeNow;
    }

    public void MoveStep(NPC npc, long now, GameWorld world)
    {
        if (npc.Hidden) return;
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        LeashCheck(npc, state, cfg, now, world);
        if (npc.AggroTarget is null)
        {
            // The engine can clear aggro without the leash path (target death, leaving the
            // view box), so the next chase must start a fresh leash window and exit point.
            state.LeashStartedAt = 0;
            state.LeftRouteAtX = 0;
            state.LeftRouteAtY = 0;
            ResetCheck(npc, state, cfg, now, world);
        }
        if (npc.AggroTarget is Player target)
            Chase(npc, state, target, world);
        else
            Patrol(npc, cfg, world);
    }

    public void AttackStep(NPC npc, long now, GameWorld world)
    {
        if (npc.Buffs.Any(b => b.SpellEffect.EffectType == SpellEffect.EffectTypes.Stun)) return;
        if (npc.AggroTarget is null || npc.Hidden) return;
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        if (now >= state.NextLungeAt)
        {
            if (world.Random.NextDouble() < cfg.Weeper.HideChance && Hide(npc, state, cfg, now, world)) return;
            if (Lunge(npc, state, cfg, now, world)) return;
        }
        Swing(npc, state, cfg, now, world);
    }

    public void FootstepsCheck(NPC npc, long now, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        if (now < state.NextFootstepsAt) return;
        state.NextFootstepsAt = now + Halloween.Ticks(world, cfg.Weeper.FootstepSeconds);
        foreach (var p in HalloweenBoss.PlayersNear(npc, cfg.Weeper.FootstepRadius))
        {
            if (npc.AggroTargetToValue.ContainsKey(p)) continue;
            world.Send(p, P.Notice(NoticeType.Info, "[Event] Something is dragging its feet… to the " +
                HalloweenBoss.Compass(p.MapX, p.MapY, npc.MapX, npc.MapY) + "."));
        }
    }

    private static void LeashCheck(NPC npc, WeeperState state, HalloweenConfig cfg, long now, GameWorld world)
    {
        if (npc.AggroTarget is null) return;
        bool nearby = HalloweenBoss.PlayersNear(npc, cfg.Weeper.LeashRadius).Count > 0;
        if (nearby)
        {
            state.LeashStartedAt = 0;
        }
        else if (state.LeashStartedAt == 0)
        {
            state.LeashStartedAt = now;
        }
        bool timedOut = state.LeashStartedAt != 0 &&
            now - state.LeashStartedAt >= Halloween.Ticks(world, cfg.Weeper.LeashSeconds);
        bool offRoute = state.LeftRouteAtX != 0 && state.LeftRouteAtY != 0 &&
            Math.Abs(npc.MapX - state.LeftRouteAtX) + Math.Abs(npc.MapY - state.LeftRouteAtY) > cfg.Weeper.LeashMaxDistance;
        if (timedOut || offRoute)
        {
            npc.AggroTargetToValue.Clear();
            npc.AggroTarget = null;
            state.LastCombatAt = now;
            state.LeashStartedAt = 0;
            state.LeftRouteAtX = 0;
            state.LeftRouteAtY = 0;
            NearestWaypoint(npc, cfg);
        }
    }

    private static void ResetCheck(NPC npc, WeeperState state, HalloweenConfig cfg, long now, GameWorld world)
    {
        if (now - state.LastCombatAt < Halloween.Ticks(world, cfg.Weeper.ResetSeconds)) return;
        if (npc.CurrentHP >= npc.MaxHP) return;
        npc.CurrentHP = npc.MaxHP;
        foreach (var p in npc.Map.GetPlayersInRange(npc))
            world.Send(p, P.VitalsPercentage(npc));
    }

    private static void Chase(NPC npc, WeeperState state, Player target, GameWorld world)
    {
        if (state.LeftRouteAtX == 0 && state.LeftRouteAtY == 0)
        {
            state.LeftRouteAtX = npc.MapX;
            state.LeftRouteAtY = npc.MapY;
        }
        int dx = Math.Abs(npc.MapX - target.MapX);
        int dy = Math.Abs(npc.MapY - target.MapY);
        if (dx + dy <= 1) return;
        if (dx == 1 && dy == 1)
        {
            foreach (var beside in new[] { (target.MapX, npc.MapY), (npc.MapX, target.MapY) })
                if (npc.CanMoveTo(beside.Item1, beside.Item2) &&
                    npc.StepTo(DirectionTo((npc.MapX, npc.MapY), beside), world))
                    return;
            return;
        }
        var dir = npc.PathStepTo(target.MapX, target.MapY, world,
            (x, y) => npc.Map.GetCharacterAt(x, y) is not null && npc.Map.GetCharacterAt(x, y) != target);
        dir ??= Approach(npc, target);
        if (dir is not null) npc.StepTo(dir.Value, world);
    }

    private static Direction? Approach(NPC npc, Player target)
    {
        var map = npc.Map;
        var start = (npc.MapX, npc.MapY);
        var cameFrom = new Dictionary<(int X, int Y), (int X, int Y)> { [start] = start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);
        var best = start;
        int bestDistance = Distance(start, target);
        while (queue.Count > 0 && cameFrom.Count < ApproachSearchLimit)
        {
            var tile = queue.Dequeue();
            foreach (var next in Neighbours(tile))
            {
                if (cameFrom.ContainsKey(next) || map.IsTileBlocked(npc, next.X, next.Y)) continue;
                cameFrom[next] = tile;
                queue.Enqueue(next);
                int distance = Distance(next, target);
                if (distance < bestDistance)
                {
                    best = next;
                    bestDistance = distance;
                }
            }
        }

        if (best == start)
        {
            var around = Neighbours(start)
                .Where(t => !map.IsTileBlocked(npc, t.X, t.Y) && Distance(t, target) <= bestDistance + 1)
                .ToList();
            if (around.Count == 0) return null;
            best = around[Random.Shared.Next(around.Count)];
        }
        else
        {
            while (cameFrom[best] != start) best = cameFrom[best];
        }
        return DirectionTo(start, best);
    }

    private static int Distance((int X, int Y) tile, Player target)
    {
        int dx = Math.Abs(tile.X - target.MapX);
        int dy = Math.Abs(tile.Y - target.MapY);
        return Math.Max(dx, dy) * 1000 + dx + dy;
    }

    private static IEnumerable<(int X, int Y)> Neighbours((int X, int Y) tile)
    {
        yield return (tile.X, tile.Y - 1);
        yield return (tile.X + 1, tile.Y);
        yield return (tile.X, tile.Y + 1);
        yield return (tile.X - 1, tile.Y);
    }

    private static Direction DirectionTo((int X, int Y) from, (int X, int Y) to) =>
        to.X > from.X ? Direction.Right :
        to.X < from.X ? Direction.Left :
        to.Y > from.Y ? Direction.Down :
        Direction.Up;

    private static void Patrol(NPC npc, HalloweenConfig cfg, GameWorld world)
    {
        var loop = Loop(npc, cfg);
        if (loop is null) return;
        var (name, waypoints) = loop.Value;
        int index = npc.Properties.TryGetProperty<int>("weeperWaypoint", out var i) ? i : 0;
        if (index < 0 || index >= waypoints.Count) index = 0;
        var (wx, wy) = waypoints[index];
        bool atWaypoint = npc.MapX == wx && npc.MapY == wy;
        bool besideTakenWaypoint = Math.Max(Math.Abs(npc.MapX - wx), Math.Abs(npc.MapY - wy)) <= 1 &&
            npc.Map.GetCharacterAt(wx, wy) is not null;
        if (atWaypoint || besideTakenWaypoint)
        {
            NextWaypoint(npc, cfg, world, name, index, waypoints.Count);
            return;
        }
        var dir = npc.PathStepTo(wx, wy, world, (x, y) => npc.Map.GetCharacterAt(x, y) is not null);
        if (dir is not null && npc.StepTo(dir.Value, world))
        {
            npc.Properties["weeperStuck"] = 0;
            return;
        }
        int stuck = (npc.Properties.TryGetProperty<int>("weeperStuck", out var s) ? s : 0) + 1;
        npc.Properties["weeperStuck"] = stuck;
        if (stuck >= StuckStepsBeforeSkip)
            NextWaypoint(npc, cfg, world, name, index, waypoints.Count);
    }

    private static void NextWaypoint(NPC npc, HalloweenConfig cfg, GameWorld world, string loop, int index, int count)
    {
        npc.Properties["weeperStuck"] = 0;
        if (index >= count - 1)
        {
            npc.Properties["weeperLastLoop"] = loop;
            npc.Properties["weeperLoop"] = PickLoop(cfg, world, loop);
            npc.Properties["weeperWaypoint"] = 0;
        }
        else
        {
            npc.Properties["weeperWaypoint"] = index + 1;
        }
    }

    private static bool Lunge(NPC npc, WeeperState state, HalloweenConfig cfg, long now, GameWorld world)
    {
        var candidates = HalloweenBoss.PlayersNear(npc, cfg.Weeper.LungeRadius)
            .Where(p => p != npc.AggroTarget).ToList();
        if (candidates.Count == 0) return false;
        var target = candidates[world.Random.Next(candidates.Count)];
        (int X, int Y)? tile = TileBehind(npc, target);
        if (tile is null) return false;
        npc.MoveTo(world, tile.Value.X, tile.Value.Y);
        npc.AddAggro(target, npc.AggroValue.Damage + npc.AggroValue.Taunt + 1, wastaunt: true, world);
        Halloween.Feed(world, target, "The Weeper lunges at " + target.Name + "!");
        state.NextLungeAt = now + Halloween.Ticks(world, cfg.Weeper.LungeSeconds);
        return true;
    }

    private static bool Hide(NPC npc, WeeperState state, HalloweenConfig cfg, long now, GameWorld world)
    {
        if (cfg.Weeper.HideSeconds <= 0) return false;
        var watchers = HalloweenBoss.PlayersNear(npc, cfg.Weeper.LeashRadius);
        if (watchers.Count == 0) return false;
        npc.Hide(world);
        foreach (var p in watchers)
            Halloween.Feed(world, p, "The Weeper hides in the shadows…");
        state.HiddenUntil = now + Halloween.Ticks(world, cfg.Weeper.HideSeconds);
        state.NextLungeAt = state.HiddenUntil + Halloween.Ticks(world, cfg.Weeper.LungeSeconds);
        // Same reload-safe pattern as the tick timer: re-resolve and invoke Emerge reflectively.
        ScriptTimerEvent.Create(() =>
        {
            var current = npc.Script?.Object;
            if (current is not null)
                current.GetType().GetMethod("Emerge")!.Invoke(current, new object?[] { npc, world });
        }, TimeSpan.FromSeconds(cfg.Weeper.HideSeconds), world);
        return true;
    }

    public void Emerge(NPC npc, GameWorld world)
    {
        if (!npc.Hidden || npc.State != NPC.States.Alive) return;
        var cfg = HalloweenConfig.Get(world);
        var near = HalloweenBoss.PlayersNear(npc, cfg.Weeper.LeashRadius);
        var others = near.Where(p => p != npc.AggroTarget).ToList();
        var pool = others.Count > 0 ? others : near;
        var target = pool.Count > 0 ? pool[world.Random.Next(pool.Count)] : null;
        var tile = target is null ? null : TileBehind(npc, target);
        var (x, y) = tile ?? (npc.MapX, npc.MapY);
        if (target is not null) npc.Facing = DirectionTo((x, y), (target.MapX, target.MapY));
        npc.Reveal(world, x, y);
        if (target is null) return;
        npc.AddAggro(target, npc.AggroValue.Damage + npc.AggroValue.Taunt + 1, wastaunt: true, world);
        foreach (var p in near)
            Halloween.Feed(world, p, "The Weeper steps out of the shadows behind " + target.Name + "!");
    }

    private static (int X, int Y)? TileBehind(NPC npc, Player target)
    {
        var behind = target.Facing switch
        {
            Direction.Up => (0, 1),
            Direction.Down => (0, -1),
            Direction.Right => (-1, 0),
            _ => (1, 0),
        };
        var sides = behind.Item1 == 0 ? new[] { (1, 0), (-1, 0) } : new[] { (0, 1), (0, -1) };
        var front = (-behind.Item1, -behind.Item2);
        foreach (var (dx, dy) in new[] { behind, sides[0], sides[1], front })
        {
            int x = target.MapX + dx;
            int y = target.MapY + dy;
            if (!npc.Map.IsTileBlocked(null, x, y)) return (x, y);
        }
        return null;
    }

    private static void Swing(NPC npc, WeeperState state, HalloweenConfig cfg, long now, GameWorld world)
    {
        if (npc.AggroTarget is not Player target || npc.Map != target.Map) return;
        if (Math.Abs(npc.MapX - target.MapX) > npc.AttackRange || Math.Abs(npc.MapY - target.MapY) > npc.AttackRange) return;
        double percent = cfg.Weeper.HitPercent * (HalloweenBoss.IsStraggler(target, cfg.Weeper.StragglerRadius)
            ? cfg.Weeper.StragglerMultiplier : 1);
        HalloweenBoss.PercentHit(npc, target, percent, world);
        HalloweenBoss.AddBleed(npc, target, now, world);
        state.LastCombatAt = now;
    }

    private static (string Name, List<(int X, int Y)> Points)? Loop(NPC npc, HalloweenConfig cfg)
    {
        if (cfg.Weeper.Loops.Count == 0) return null;
        string name = npc.Properties.TryGetProperty<string>("weeperLoop", out var n) ? n : "";
        if (!cfg.Weeper.Loops.TryGetValue(name, out var raw) || raw is null || raw.Length == 0)
        {
            name = cfg.Weeper.Loops.Keys.First();
            raw = cfg.Weeper.Loops[name];
            npc.Properties["weeperLoop"] = name;
            npc.Properties["weeperWaypoint"] = 0;
        }
        return (name, raw.Select(p => (p[0], p[1])).ToList());
    }

    private static string PickLoop(HalloweenConfig cfg, GameWorld world, string? exclude)
    {
        var candidates = cfg.Weeper.Loops.Keys.Where(k => k != exclude).ToList();
        if (candidates.Count == 0) candidates = cfg.Weeper.Loops.Keys.ToList();
        if (candidates.Count == 0) return "";
        return candidates[world.Random.Next(candidates.Count)];
    }

    private static void NearestWaypoint(NPC npc, HalloweenConfig cfg)
    {
        var loop = Loop(npc, cfg);
        if (loop is null) return;
        var waypoints = loop.Value.Points;
        int best = 0;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < waypoints.Count; i++)
        {
            int d = Math.Abs(npc.MapX - waypoints[i].X) + Math.Abs(npc.MapY - waypoints[i].Y);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = i;
            }
        }
        npc.Properties["weeperWaypoint"] = best;
    }

    private static WeeperState State(NPC npc)
    {
        if (npc.ScriptStore is WeeperState state) return state;
        state = new WeeperState();
        npc.ScriptStore = state;
        return state;
    }
}

return typeof(Weeper);
