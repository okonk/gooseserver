#load "ArenaConfig.csx"
#load "ArenaMath.csx"
#load "ArenaCore.csx"
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Goose;
using Goose.Commands;
using Goose.Scripting;

public class ArenaController : BaseGlobalScript
{
    private const string FileName = "Arena.csx";
    private const string LastTickKey = "arena.lastTickUtc";
    private static readonly TimeSpan TickPeriod = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(2);
    // First tick must not resume stale run state before the one-shot recovery runs.
    private static readonly TimeSpan FirstTickDelay = StartupDelay + TickPeriod;
    private static NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

    public override void OnLoaded(GameWorld world)
    {
        world.Commands.Register("/arena ", AccessPrivilege.SpawnNPC, "Arena",
            "Arena: start, stop, status, open, close, preview <round> [raw] [player]",
            new Action<CommandContext, string, string[]>(CommandCurrent));

        ScriptTimerEvent? timer = null;
        timer = ScriptTimerEvent.Create(() =>
        {
            try
            {
                TickCurrent(world);
            }
            finally
            {
                timer!.Reschedule(TickPeriod, world);
            }
        }, FirstTickDelay, world);

        ScriptTimerEvent.Create(() => RecoverCurrent(world), StartupDelay, world);
    }

    public override void OnNPCKilled(NPC npc, ICharacter killer, IReadOnlyList<Player> credited, GameWorld world)
        => ArenaCore.OnNpcKilled(npc, killer, credited, world);

    public void Tick(GameWorld world, DateTime nowUtc)
    {
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Enabled(world))
        {
            if (ArenaState.Load(world) is not null)
                ArenaCore.EndRun(world, "arena unavailable");
            return;
        }
        var lastTick = ParseLastTick(world, nowUtc);
        Announce(world, cfg, lastTick, nowUtc);
        var next = ArenaMath.NextStartUtc(nowUtc, cfg);
        if (next - TimeSpan.FromHours(cfg.IntervalHours) >= lastTick)
        {
            if (ArenaState.Load(world) is not null)
                log.Info("Arena start skipped at {0}: a run is already active", nowUtc);
            else
                ArenaCore.StartRun(world, nowUtc);
        }
        world.WorldState.Set(LastTickKey, nowUtc.ToString("o", CultureInfo.InvariantCulture));
        ArenaCore.Tick(world, nowUtc);
    }

    public void Recover(GameWorld world)
    {
        if (ArenaState.Load(world) is not null)
            ArenaCore.EndRun(world, "server restarted");
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Enabled(world)) return;
        if (!world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var arenaMap) ||
            !world.MapHandler.Maps.TryGetValue(cfg.LobbyMap, out var lobby)) return;
        foreach (var npc in arenaMap.NPCs.ToList())
            if (npc.Properties.ContainsKey("arena.run"))
                npc.Despawn(world);
        foreach (var player in arenaMap.Players.ToList())
            if (!player.IsGM)
                player.WarpTo(world, lobby, cfg.LobbySpawn[0], cfg.LobbySpawn[1]);
    }

    public void Command(CommandContext ctx, string sub, string[] rest)
    {
        switch (sub)
        {
            case "start": Start(ctx); break;
            case "stop": Stop(ctx); break;
            case "status": Status(ctx); break;
            case "open": Open(ctx); break;
            case "close": Close(ctx); break;
            case "preview": Preview(ctx, rest); break;
            default: ctx.Send("Usage: /arena start|stop|status|open|close|preview <round> [raw] [player]"); break;
        }
    }

    private void Start(CommandContext ctx)
    {
        if (ArenaState.Load(ctx.World) is not null)
        {
            ctx.Send("A run is already active.");
            return;
        }
        ArenaCore.StartRun(ctx.World, DateTime.UtcNow);
        ctx.Send(ArenaState.Load(ctx.World) is null
            ? (ArenaConfig.IsClosed(ctx.World)
                ? "Run not started: the arena is closed (/arena open to reopen)."
                : "Run not started: no players in the lobby (or the arena is disabled).")
            : "Arena run started.");
    }

    private void Stop(CommandContext ctx)
    {
        if (ArenaState.Load(ctx.World) is null)
        {
            ctx.Send("No run is active.");
            return;
        }
        ArenaCore.EndRun(ctx.World, "stopped by a GM");
        ctx.Send("Arena run stopped.");
    }

    private void Open(CommandContext ctx)
    {
        ctx.World.WorldState.Set(ArenaConfig.OpenKey, true);
        ctx.Send("The arena is open. Runs start on the normal schedule.");
    }

    private void Close(CommandContext ctx)
    {
        ctx.World.WorldState.Remove(ArenaConfig.OpenKey);
        if (ArenaState.Load(ctx.World) is not null)
            ArenaCore.EndRun(ctx.World, "arena closed by a GM");
        ctx.Send("The arena is closed. No runs will start until /arena open.");
    }

    private void Status(CommandContext ctx)
    {
        var world = ctx.World;
        var cfg = ArenaConfig.Get(world);
        if (ArenaConfig.IsClosed(world))
        {
            ctx.Send("The arena is closed (/arena open to reopen).");
            return;
        }
        if (!cfg.Valid)
        {
            ctx.Send("The arena is disabled.");
            return;
        }
        var next = ArenaMath.NextStartUtc(DateTime.UtcNow, cfg).ToString("HH:mm", CultureInfo.InvariantCulture);
        var run = ArenaState.Load(world);
        if (run is null)
        {
            ctx.Send("[Arena] No run active. Next start: " + next + " UTC.");
            return;
        }
        var alive = run.Participants.Values.Count(p => p.Alive);
        var mobs = 0;
        if (world.MapHandler.Maps.TryGetValue(cfg.ArenaMap, out var arenaMap))
            mobs = arenaMap.NPCs.Count(n => n.State == NPC.States.Alive &&
                n.Properties.TryGetProperty<string>("arena.run", out var tag) && tag == run.RunId);
        ctx.Send("[Arena] " + run.Phase + ", round " + run.Round + ", " + alive + " alive, " + mobs + " mobs. Next start: " + next + " UTC.");
    }

    private void Preview(CommandContext ctx, string[] rest)
    {
        var world = ctx.World;
        if (rest.Length == 0 || !int.TryParse(rest[0], out var round) || round < 1)
        {
            ctx.Send("Usage: /arena preview <round> [raw] [player]");
            return;
        }
        long raw = 1_000_000;
        if (rest.Length > 1 && !long.TryParse(rest[1], out raw))
        {
            ctx.Send("Usage: /arena preview <round> [raw] [player]");
            return;
        }
        var target = ctx.Player;
        if (rest.Length > 2)
        {
            target = world.PlayerHandler.Players.FirstOrDefault(p => string.Equals(p.Name, rest[2], StringComparison.OrdinalIgnoreCase));
            if (target is null)
            {
                ctx.Send("No online player named " + rest[2] + ".");
                return;
            }
        }
        var cfg = ArenaConfig.Get(world);
        if (!cfg.Valid)
        {
            ctx.Send("The arena is disabled.");
            return;
        }
        var run = ArenaState.Load(world);
        int alive = run is null ? 1 : run.Participants.Values.Count(p => p.Alive);
        double s = ArenaMath.PowerFraction(target.ExperienceSold, cfg);
        ctx.Send("[Arena] Preview round " + round + " for " + target.Name + ":");
        ctx.Send("  Power s = " + s.ToString("0.####", CultureInfo.InvariantCulture));
        ctx.Send("  HP (alive " + alive + "): normal " + ArenaMath.MobHp(round, MobTier.Normal, alive, cfg) +
            ", elite " + ArenaMath.MobHp(round, MobTier.Elite, alive, cfg) +
            ", boss " + ArenaMath.MobHp(round, MobTier.Boss, alive, cfg));
        ctx.Send("  Your effective hit (raw " + raw + ") = " + ArenaMath.EffectiveDamage(raw, s, cfg));
        var band = ArenaMath.BandFor(round, cfg);
        var template = world.NPCHandler.GetNPCTemplate(band.Normals[0]);
        if (template is null)
        {
            ctx.Send("  No mob template available for mitigation preview.");
            return;
        }
        foreach (var tier in new[] { MobTier.Normal, MobTier.Elite, MobTier.Boss })
        {
            var npc = new NPC();
            npc.NPCTemplate = template;
            npc.Level = template.Level;
            npc.MaxStats = new AttributeSet();
            double before = ArenaMath.MobWeaponDamage(round, tier, s, cfg);
            double after = npc.MitigateMelee(target, before, world);
            ctx.Send("  " + tier + " weapon: " + F(before) + " -> " + F(after));
        }
    }

    private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static void Announce(GameWorld world, ArenaConfig cfg, DateTime lastTick, DateTime nowUtc)
    {
        var next = ArenaMath.NextStartUtc(nowUtc, cfg);
        foreach (var m in cfg.AnnounceMinutes)
        {
            var at = next - TimeSpan.FromMinutes(m);
            if (at > lastTick && at <= nowUtc)
                world.SendToAll(P.ServerMessage("[Arena] The arena opens in " + m + " minutes. Gather in the lobby!"));
        }
    }

    private static DateTime ParseLastTick(GameWorld world, DateTime nowUtc)
    {
        var stored = world.WorldState.Get<string>(LastTickKey);
        if (stored is not null &&
            DateTime.TryParse(stored, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var t))
            return t;
        return nowUtc;
    }

    // A /reloadscripts swap replaces Object with an instance of a freshly compiled type, so
    // these cannot be called statically typed; re-resolve through the registry and invoke.
    private static void TickCurrent(GameWorld world)
    {
        var current = Current(world);
        if (current is null) return;
        current.GetType().GetMethod("Tick")!.Invoke(current, new object?[] { world, DateTime.UtcNow });
    }

    private static void RecoverCurrent(GameWorld world)
    {
        var current = Current(world);
        if (current is null) return;
        current.GetType().GetMethod("Recover")!.Invoke(current, new object?[] { world });
    }

    private static void CommandCurrent(CommandContext ctx, string sub, string[] rest)
    {
        var current = Current(ctx.World);
        if (current is null) return;
        current.GetType().GetMethod("Command")!.Invoke(current, new object?[] { ctx, sub, rest });
    }

    private static IGlobalScript? Current(GameWorld world)
    {
        foreach (var script in world.ScriptHandler.GetGlobalScripts())
            if (Path.GetFileName(script.FilePath) == FileName)
                return script.Object;
        return null;
    }
}

return typeof(ArenaController);
