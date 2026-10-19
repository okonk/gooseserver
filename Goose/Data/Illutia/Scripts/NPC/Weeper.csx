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
}

public class Weeper : BaseNPCScript
{
    private const int StuckStepsBeforeSkip = 5;

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
        if (npc.AggroTarget is null) return;
        var cfg = HalloweenConfig.Get(world);
        var state = State(npc);
        if (now >= state.NextLungeAt && Lunge(npc, state, cfg, now, world)) return;
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
        if (Math.Abs(npc.MapX - target.MapX) <= 1 && Math.Abs(npc.MapY - target.MapY) <= 1) return;
        var dir = npc.PathStepTo(target.MapX, target.MapY, world,
            (x, y) => npc.Map.GetCharacterAt(x, y) is not null && npc.Map.GetCharacterAt(x, y) != target);
        if (dir is not null) npc.StepTo(dir.Value, world);
    }

    private static void Patrol(NPC npc, HalloweenConfig cfg, GameWorld world)
    {
        var loop = Loop(npc, cfg);
        if (loop is null) return;
        var (name, waypoints) = loop.Value;
        int index = npc.Properties.TryGetProperty<int>("weeperWaypoint", out var i) ? i : 0;
        if (index < 0 || index >= waypoints.Count) index = 0;
        var (wx, wy) = waypoints[index];
        if (npc.MapX == wx && npc.MapY == wy)
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
