#load "ArenaConfig.csx"
#load "ArenaFloor.csx"
#load "ArenaMath.csx"
using System;
using System.Collections.Generic;
using System.Linq;
using Goose;
using Goose.Events;

public sealed class ArenaTelegraph
{
    public List<(int X, int Y)> Tiles { get; set; } = new();
    public Func<List<(int X, int Y)>>? LiveTiles { get; set; }
    public double Windup { get; set; }
    public bool Busy { get; set; }
    public Action Resolve { get; set; } = () => { };
}

public static class ArenaBoss
{
    public const string TokenKey = "arena.boss.token";
    public const string NextKey = "arena.boss.next";
    public const string LastKey = "arena.boss.last";
    public const string SwapAtKey = "arena.boss.swapAt";
    public const string BusyUntilKey = "arena.boss.busyUntil";
    public const string LockIdKey = "arena.boss.lockId";
    public const string LockUntilKey = "arena.boss.lockUntil";
    public const string SummonsKey = "arena.boss.summons";
    public const string GuardOfKey = "arena.guardOf";

    public static bool IsBoss(NPC npc)
        => npc.Properties.TryGetProperty<string>("arena.run", out _)
           && (MobTier)npc.Properties.GetProperty<int>("arena.tier", 0) == MobTier.Boss;

    private static int Round(NPC npc) => npc.Properties.GetProperty<int>("arena.round", 1);

    private static long Ticks(GameWorld world, double seconds) => (long)(seconds * world.TimerFrequency);

    private static double Between(GameWorld world, double[] range) => range[0] + world.Random.NextDouble() * (range[1] - range[0]);

    public static void OnSpawn(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        var m = cfg.BossMechanics;
        npc.Properties[TokenKey] = Guid.NewGuid().ToString("N");
        npc.Properties[NextKey] = world.TimeNow + Ticks(world, Between(world, m.GapSeconds));
        if (m.Swap is not null)
            npc.Properties[SwapAtKey] = world.TimeNow + Ticks(world, Between(world, m.Swap.EverySeconds));
    }

    public static bool IsBusy(NPC npc, GameWorld world)
        => world.TimeNow < npc.Properties.GetProperty<long>(BusyUntilKey, 0);

    public static void HoldTarget(NPC npc, GameWorld world)
    {
        if (world.TimeNow >= npc.Properties.GetProperty<long>(LockUntilKey, 0)) return;
        int id = npc.Properties.GetProperty<int>(LockIdKey, 0);
        var target = npc.Map.Players.FirstOrDefault(p => p.PlayerID == id);
        if (target is null) return;
        if (!npc.AggroTargetToValue.ContainsKey(target)) npc.AddAggro(target, 1, world);
        npc.AggroTarget = target;
        npc.AggroValue = npc.AggroTargetToValue[target];
    }

    public static bool Think(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        if (IsBusy(npc, world)) return false;
        var m = cfg.BossMechanics;
        int round = Round(npc);

        if (m.Swap is not null && round >= m.Swap.FromRound
            && world.TimeNow >= npc.Properties.GetProperty<long>(SwapAtKey, long.MaxValue))
        {
            npc.Properties[SwapAtKey] = world.TimeNow + Ticks(world, Between(world, m.Swap.EverySeconds));
            var swap = Swap(npc, world, cfg);
            if (swap is not null)
            {
                Run(npc, world, cfg, swap);
                return true;
            }
        }

        if (world.TimeNow < npc.Properties.GetProperty<long>(NextKey, 0)) return false;

        string last = npc.Properties.GetProperty<string>(LastKey, "");
        var options = Available(cfg, round).Where(k => k != last).OrderBy(_ => world.Random.Next()).ToList();
        foreach (var kind in options)
        {
            var telegraph = Start(kind, npc, world, cfg);
            if (telegraph is null) continue;
            npc.Properties[LastKey] = kind;
            npc.Properties[NextKey] = world.TimeNow + Ticks(world, telegraph.Windup + Between(world, m.GapSeconds));
            Run(npc, world, cfg, telegraph);
            return true;
        }
        return false;
    }

    public static List<string> Available(ArenaConfig cfg, int round)
    {
        var m = cfg.BossMechanics;
        var kinds = new List<string>();
        if (m.Slam is not null && round >= m.Slam.FromRound) kinds.Add("slam");
        if (m.Slam is not null && m.Pull is not null && round >= m.Pull.FromRound) kinds.Add("pull");
        if (m.Charge is not null && round >= m.Charge.FromRound) kinds.Add("charge");
        if (m.Mark is not null && round >= m.Mark.FromRound) kinds.Add("mark");
        if (m.SafeZone is not null && round >= m.SafeZone.FromRound) kinds.Add("safeZone");
        return kinds;
    }

    public static ArenaTelegraph? Start(string kind, NPC npc, GameWorld world, ArenaConfig cfg) => kind switch
    {
        "slam" => Slam(npc, world, cfg, cfg.BossMechanics.Slam!.Windup),
        "pull" => Pull(npc, world, cfg),
        "charge" => Charge(npc, world, cfg),
        "mark" => Mark(npc, world, cfg),
        "safeZone" => SafeZone(npc, world, cfg),
        "swap" => Swap(npc, world, cfg),
        _ => null,
    };

    public static void Run(NPC npc, GameWorld world, ArenaConfig cfg, ArenaTelegraph t)
    {
        string token = npc.Properties.GetProperty<string>(TokenKey, "");
        if (t.Busy) npc.Properties[BusyUntilKey] = world.TimeNow + Ticks(world, t.Windup);
        world.SendToMap(npc.Map, P.Cast(npc));
        Show(npc, world, cfg, t.LiveTiles?.Invoke() ?? t.Tiles);
        for (int second = 1; second < t.Windup; second++)
            ScriptTimerEvent.Create(() =>
            {
                if (Same(npc, token)) Show(npc, world, cfg, t.LiveTiles?.Invoke() ?? t.Tiles);
            }, TimeSpan.FromSeconds(second), world);
        ScriptTimerEvent.Create(() =>
        {
            if (Same(npc, token)) t.Resolve();
        }, TimeSpan.FromSeconds(t.Windup), world);
    }

    private static bool Same(NPC npc, string token)
        => npc.State == NPC.States.Alive && npc.Properties.GetProperty<string>(TokenKey, "") == token;

    private static void Show(NPC npc, GameWorld world, ArenaConfig cfg, List<(int X, int Y)> tiles)
    {
        if (tiles.Count == 0) return;
        var m = cfg.BossMechanics;
        world.SendToMap(npc.Map, string.Join("\x1", tiles.Select(t => P.SpellTile(t.X, t.Y, m.Animation, m.AnimationFile))));
    }

    private static void Notice(NPC npc, GameWorld world, string text)
        => world.SendToMap(npc.Map, P.Notice(NoticeType.Info, text));

    private static string Name(NPC npc) => npc.Name.StartsWith("Arena ") ? npc.Name.Substring("Arena ".Length) : npc.Name;

    public static List<Player> Targets(NPC npc)
        => npc.Map.Players.Where(p => !p.IsGM && p.CurrentHP > 0).ToList();

    public static void Hit(NPC npc, GameWorld world, ArenaConfig cfg, IEnumerable<Player> players, double mult)
    {
        int round = Round(npc);
        foreach (var player in players.ToList())
        {
            if (player.Map != npc.Map) continue;
            double weapon = ArenaMath.MobWeaponDamage(round, MobTier.Boss, ArenaMath.PowerFraction(player.ExperienceSold, cfg), cfg);
            long damage = (long)npc.MitigateMelee(player, weapon * mult, world);
            if (damage > 0) player.Attacked(npc, damage, world);
        }
    }

    private static double Distance(int x1, int y1, int x2, int y2)
        => Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));

    private static List<Player> PlayersOn(NPC npc, IEnumerable<(int X, int Y)> tiles)
    {
        var set = tiles.ToHashSet();
        return Targets(npc).Where(p => set.Contains((p.MapX, p.MapY))).ToList();
    }

    public static ArenaTelegraph Slam(NPC npc, GameWorld world, ArenaConfig cfg, double windup)
    {
        var s = cfg.BossMechanics.Slam!;
        int r = (int)Math.Ceiling(s.Radius);
        var ring = new List<(int X, int Y)>();
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                double d = Distance(0, 0, dx, dy);
                if (d > s.Radius - 1 && d <= s.Radius) ring.Add((npc.MapX + dx, npc.MapY + dy));
            }
        return new ArenaTelegraph
        {
            Tiles = ring,
            Windup = windup,
            Busy = true,
            Resolve = () => Hit(npc, world, cfg,
                Targets(npc).Where(p => Distance(p.MapX, p.MapY, npc.MapX, npc.MapY) <= s.Radius), s.Damage),
        };
    }

    public static ArenaTelegraph? Pull(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        var pull = cfg.BossMechanics.Pull!;
        var pulled = Targets(npc)
            .Where(p =>
            {
                int d = Chebyshev(p.MapX, p.MapY, npc.MapX, npc.MapY);
                return d > 1 && d <= pull.Range;
            })
            .OrderBy(p => Distance(p.MapX, p.MapY, npc.MapX, npc.MapY))
            .ToList();
        if (pulled.Count == 0) return null;
        foreach (var player in pulled)
        {
            var tile = NearestFree(npc, world, cfg, npc.MapX, npc.MapY, 3);
            if (tile is null) break;
            player.WarpTo(world, npc.Map, tile.Value.X, tile.Value.Y, false);
        }
        Notice(npc, world, Name(npc) + " drags you in! Get away before it slams!");
        return Slam(npc, world, cfg, pull.Windup);
    }

    public static ArenaTelegraph? Charge(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        var c = cfg.BossMechanics.Charge!;
        if (npc.AggroTarget is not Player target || target.Map != npc.Map) return null;
        int dx = target.MapX - npc.MapX, dy = target.MapY - npc.MapY;
        if (dx == 0 && dy == 0) return null;
        var (sx, sy, facing) = Math.Abs(dx) >= Math.Abs(dy)
            ? (Math.Sign(dx), 0, dx > 0 ? Direction.Right : Direction.Left)
            : (0, Math.Sign(dy), dy > 0 ? Direction.Down : Direction.Up);
        var line = new List<(int X, int Y)>();
        for (int i = 1; i <= c.Length; i++)
        {
            int x = npc.MapX + sx * i, y = npc.MapY + sy * i;
            if (!ArenaFloor.OnFloor(npc.Map, cfg.ArenaRect, x, y)) break;
            line.Add((x, y));
        }
        if (line.Count == 0) return null;
        npc.FaceTo(facing, world);
        return new ArenaTelegraph
        {
            Tiles = line,
            Windup = c.Windup,
            Busy = true,
            Resolve = () =>
            {
                Hit(npc, world, cfg, PlayersOn(npc, line), c.Damage);
                for (int i = line.Count - 1; i >= 0; i--)
                {
                    if (npc.Map.IsTileBlocked(npc, line[i].X, line[i].Y)) continue;
                    Move(npc, world, line[i].X, line[i].Y);
                    break;
                }
            },
        };
    }

    public static ArenaTelegraph? Mark(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        var mk = cfg.BossMechanics.Mark!;
        var targets = Targets(npc);
        if (targets.Count == 0) return null;
        var marked = targets[world.Random.Next(targets.Count)];
        int half = mk.Size / 2;
        var tiles = new List<(int X, int Y)>();
        for (int dy = -half; dy <= half; dy++)
            for (int dx = -half; dx <= half; dx++)
                tiles.Add((marked.MapX + dx, marked.MapY + dy));
        world.Send(marked, P.Notice(NoticeType.Info, "You've been marked! Get out of the marked area!"));
        return new ArenaTelegraph
        {
            Tiles = tiles,
            Windup = mk.Windup,
            Busy = false,
            Resolve = () => Hit(npc, world, cfg, PlayersOn(npc, tiles), mk.Damage),
        };
    }

    public static ArenaTelegraph? SafeZone(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        var z = cfg.BossMechanics.SafeZone!;
        int[] rect = cfg.ArenaRect;
        var blocks = new List<(int X, int Y, int W, int H)>();
        for (int by = rect[1]; by < rect[1] + rect[3]; by += z.BlockHeight)
            for (int bx = rect[0]; bx < rect[0] + rect[2]; bx += z.BlockWidth)
            {
                int w = Math.Min(z.BlockWidth, rect[0] + rect[2] - bx);
                int h = Math.Min(z.BlockHeight, rect[1] + rect[3] - by);
                if (w < z.BlockWidth / 2 || h < z.BlockHeight / 2) continue;
                blocks.Add((bx, by, w, h));
            }
        var near = blocks.Where(b => Chebyshev(b.X + b.W / 2, b.Y + b.H / 2, npc.MapX, npc.MapY) <= z.MaxDistance).ToList();
        if (near.Count == 0) near = blocks;
        if (near.Count == 0) return null;
        var safe = near[world.Random.Next(near.Count)];
        bool Inside(int x, int y) => x >= safe.X && x < safe.X + safe.W && y >= safe.Y && y < safe.Y + safe.H;

        var edge = new List<(int X, int Y)>();
        for (int x = safe.X - 1; x <= safe.X + safe.W; x++)
            for (int y = safe.Y - 1; y <= safe.Y + safe.H; y++)
                if (!Inside(x, y) && ArenaFloor.OnFloor(npc.Map, rect, x, y)) edge.Add((x, y));

        Notice(npc, world, Name(npc) + " gathers its power! Get inside the marked zone!");
        return new ArenaTelegraph
        {
            Tiles = edge,
            Windup = z.Windup,
            Busy = true,
            Resolve = () => Hit(npc, world, cfg, Targets(npc).Where(p => !Inside(p.MapX, p.MapY)), z.Damage),
        };
    }

    public static ArenaTelegraph? Swap(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        var sw = cfg.BossMechanics.Swap!;
        const string DmgPrefix = "arena.dmg.";
        var ranked = Targets(npc)
            .Select(p => (Player: p, Damage: npc.Properties.GetProperty<long>(DmgPrefix + p.PlayerID, 0)))
            .OrderByDescending(e => e.Damage).ThenBy(e => e.Player.PlayerID)
            .Select(e => e.Player)
            .ToList();
        if (ranked.Count == 0) return null;
        var target = ranked.FirstOrDefault(p => p != npc.AggroTarget) ?? ranked[0];

        world.SendToMap(npc.Map, $"EMOT{npc.LoginID},{sw.Emote[0]},{sw.Emote[1]}");
        return new ArenaTelegraph
        {
            Windup = sw.Windup,
            Busy = true,
            LiveTiles = () => target.Map == npc.Map
                ? new List<(int X, int Y)> { (target.MapX, target.MapY) }
                : new List<(int X, int Y)>(),
            Resolve = () =>
            {
                if (target.Map != npc.Map || target.CurrentHP <= 0) return;
                var (bx, by) = Behind(target);
                var tile = !npc.Map.IsTileBlocked(npc, bx, by) && ArenaFloor.OnFloor(npc.Map, cfg.ArenaRect, bx, by)
                    ? (bx, by)
                    : NearestFree(npc, world, cfg, target.MapX, target.MapY, 1);
                if (tile is null) return;
                Move(npc, world, tile.Value.X, tile.Value.Y);
                npc.Properties[LockIdKey] = target.PlayerID;
                npc.Properties[LockUntilKey] = world.TimeNow + Ticks(world, sw.LockSeconds);
                HoldTarget(npc, world);
            },
        };
    }

    public static (int X, int Y) Behind(Player p) => p.Facing switch
    {
        Direction.Up => (p.MapX, p.MapY + 1),
        Direction.Down => (p.MapX, p.MapY - 1),
        Direction.Left => (p.MapX + 1, p.MapY),
        _ => (p.MapX - 1, p.MapY),
    };

    private static void Move(NPC npc, GameWorld world, int x, int y)
    {
        npc.Hide(world);
        npc.Reveal(world, x, y);
    }

    private static (int X, int Y)? NearestFree(NPC npc, GameWorld world, ArenaConfig cfg, int cx, int cy, int maxRadius)
    {
        for (int r = 1; r <= maxRadius; r++)
        {
            var ring = new List<(int X, int Y)>();
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == r) ring.Add((cx + dx, cy + dy));
            foreach (var (x, y) in ring.OrderBy(t => Distance(t.X, t.Y, cx, cy)))
                if (ArenaFloor.OnFloor(npc.Map, cfg.ArenaRect, x, y) && !npc.Map.IsTileBlocked(null, x, y))
                    return (x, y);
        }
        return null;
    }

    private static int Chebyshev(int x1, int y1, int x2, int y2)
        => Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));

    public static bool IsShielded(NPC npc)
    {
        string token = npc.Properties.GetProperty<string>(TokenKey, "");
        return token.Length > 0 && Guards(npc, token).Count > 0;
    }

    private static List<NPC> Guards(NPC npc, string token)
        => npc.Map.NPCs.Where(n => n.State == NPC.States.Alive
                                   && n.Properties.GetProperty<string>(GuardOfKey, "") == token).ToList();

    public static long CapForSummon(NPC npc, long damage, ArenaConfig cfg)
    {
        var su = cfg.BossMechanics.Summon;
        if (su is null || Round(npc) < su.FromRound || npc.MaxHP <= 0) return damage;
        int done = npc.Properties.GetProperty<int>(SummonsKey, 0);
        var thresholds = su.AtHp.OrderByDescending(f => f).ToArray();
        if (done >= thresholds.Length) return damage;
        long floor = (long)Math.Ceiling(npc.MaxHP * thresholds[done]);
        return Math.Max(0, Math.Min(damage, npc.CurrentHP - floor));
    }

    public static void CheckSummon(NPC npc, long hpAfter, GameWorld world, ArenaConfig cfg)
    {
        var su = cfg.BossMechanics.Summon;
        if (su is null || Round(npc) < su.FromRound || npc.MaxHP <= 0) return;
        int done = npc.Properties.GetProperty<int>(SummonsKey, 0);
        var thresholds = su.AtHp.OrderByDescending(f => f).ToArray();
        if (done >= thresholds.Length || hpAfter > (long)Math.Ceiling(npc.MaxHP * thresholds[done])) return;
        npc.Properties[SummonsKey] = done + 1;
        Summon(npc, world, cfg);
    }

    public static List<NPC> Summon(NPC npc, GameWorld world, ArenaConfig cfg)
    {
        var su = cfg.BossMechanics.Summon!;
        string token = npc.Properties.GetProperty<string>(TokenKey, "");
        string runId = npc.Properties.GetProperty<string>("arena.run", "");
        int round = Round(npc);
        var targets = Targets(npc);
        int count = su.Base + targets.Count / su.PerPlayers;
        var band = ArenaMath.BandFor(round, cfg);
        var guards = new List<NPC>();
        for (int i = 0; i < count; i++)
        {
            var template = world.NPCHandler.GetNPCTemplate(band.Normals[world.Random.Next(band.Normals.Length)]);
            if (template is null) continue;
            var tile = NearestFree(npc, world, cfg, npc.MapX, npc.MapY, 4);
            if (tile is null) break;
            var guard = world.NPCHandler.SpawnNPC(world, npc.Map.ID, tile.Value.X, tile.Value.Y, template, false,
                new PropertiesDictionary
                {
                    ["arena.run"] = runId,
                    ["arena.round"] = round,
                    ["arena.tier"] = (int)MobTier.Elite,
                    ["arena.alive"] = npc.Properties.GetProperty<int>("arena.alive", 1),
                    [GuardOfKey] = token,
                    ["untameable"] = true,
                });
            if (guard is null) continue;
            if (targets.Count > 0) guard.AddAggro(targets[world.Random.Next(targets.Count)], 1, world);
            guards.Add(guard);
        }
        if (guards.Count == 0) return guards;

        Notice(npc, world, Name(npc) + " calls its guards! It can't be hurt until they fall.");
        ShieldPulse(npc, world, cfg, token);
        return guards;
    }

    private static void ShieldPulse(NPC npc, GameWorld world, ArenaConfig cfg, string token)
    {
        if (!Same(npc, token)) return;
        if (Guards(npc, token).Count == 0)
        {
            Notice(npc, world, Name(npc) + "'s shield breaks!");
            return;
        }
        var m = cfg.BossMechanics;
        world.SendToMap(npc.Map, P.SpellPlayer(npc.LoginID, m.Animation, m.AnimationFile));
        ScriptTimerEvent.Create(() => ShieldPulse(npc, world, cfg, token), TimeSpan.FromSeconds(1), world);
    }
}
