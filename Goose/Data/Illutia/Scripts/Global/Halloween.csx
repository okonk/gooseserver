#load "Halloween/HalloweenConfig.csx"
#load "Halloween/HalloweenCommon.csx"
#load "Halloween/HalloweenAmbush.csx"
using System;
using System.IO;
using Goose;
using Goose.Commands;
using Goose.Scripting;

public class HalloweenController : BaseGlobalScript
{
    private const string FileName = "Halloween.csx";
    private static readonly TimeSpan TickPeriod = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(1);
    private static readonly string[] AmbushKinds = ["fake", "ghosts", "phantom", "bones", "bloody", "stalker"];

    private long nextChainsawAt;
    private long lastTickAt;

    public override void OnLoaded(GameWorld world)
    {
        world.Commands.Register("/halloween ", AccessPrivilege.SpawnNPC, "Halloween",
            "Halloween event: on, off, tier <1-3> <0|1>, status, resetcd, ambush <kind>, stalker, weeper, groundskeeper",
            new Action<CommandContext, string, string[]>(Command));

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
        }, StartupDelay, world);

        ScriptTimerEvent.Create(() =>
        {
            if (Halloween.Active(world) && Halloween.Tier(world, 1))
                Halloween.ApplyXpBonus(world);
        }, StartupDelay, world);
    }

    public void Tick(GameWorld world, long now, Random rng)
    {
        lastTickAt = now;
        Evict(world, now);
    }

    public long LastTickAt() => lastTickAt;

    public void Evict(GameWorld world, long now)
    {
        if (Halloween.Active(world)) return;
        var cfg = HalloweenConfig.Get(world);
        foreach (var mapId in new[] { cfg.Maps.House, cfg.Maps.Basement })
        {
            if (!world.MapHandler.Maps.TryGetValue(mapId, out var map) || map is null) continue;
            foreach (var player in map.Players.ToList())
                Halloween.EvictPlayer(world, player);
        }
    }

    public void Command(CommandContext ctx, string sub, string[] rest)
    {
        var world = ctx.World;
        var cfg = HalloweenConfig.Get(world);
        switch (sub.ToLowerInvariant())
        {
            case "on":
                Halloween.SetActive(world, true);
                ctx.Send("Halloween event is on.");
                break;
            case "off":
                Halloween.SetActive(world, false);
                Evict(world, world.TimeNow);
                ctx.Send("Halloween event is off.");
                break;
            case "tier":
                if (rest.Length < 2 || !int.TryParse(rest[0], out var tier) || tier is < 1 or > 3 ||
                    !int.TryParse(rest[1], out var on) || on is not (0 or 1))
                {
                    ctx.Send("Usage: /halloween tier <1-3> <0|1>");
                    return;
                }
                Halloween.SetTier(world, tier, on == 1);
                ctx.Send($"Tier {tier} is {(on == 1 ? "on" : "off")}.");
                break;
            case "status":
                Status(ctx, cfg);
                break;
            case "resetcd":
                var caller = ctx.Player;
                foreach (var key in caller.ScriptState.Keys.ToList())
                    if (key.StartsWith("hw.")) caller.ScriptState.Remove(key);
                ctx.Send("Halloween cooldowns reset.");
                break;
            case "ambush":
                if (rest.Length < 1 || !AmbushKinds.Contains(rest[0].ToLowerInvariant()))
                {
                    ctx.Send("Usage: /halloween ambush <fake|ghosts|phantom|bones|bloody|stalker>");
                    return;
                }
                HalloweenAmbush.Trigger(world, ctx.Player.Map, ctx.Player, rest[0].ToLowerInvariant(), world.TimeNow, world.Random);
                break;
            case "stalker":
                if (!Halloween.SpawnStalker(world, ctx.Player, "chase", world.TimeNow))
                    ctx.Send("The stalker could not be spawned.");
                break;
            case "weeper":
                SpawnBoss(ctx, cfg.Npcs.Weeper);
                break;
            case "groundskeeper":
                SpawnBoss(ctx, cfg.Npcs.Groundskeeper);
                break;
            default:
                ctx.Send("Usage: /halloween on|off|tier <1-3> <0|1>|status|resetcd|ambush <kind>|stalker|weeper|groundskeeper");
                break;
        }
    }

    private void Status(CommandContext ctx, HalloweenConfig cfg)
    {
        var world = ctx.World;
        ctx.Send($"Halloween: {(Halloween.Active(world) ? "active" : "inactive")}");
        ctx.Send($"Tiers: 1={TierState(world, 1)} 2={TierState(world, 2)} 3={TierState(world, 3)}");
        foreach (var (mapId, name) in new[]
        {
            (cfg.Maps.House, "house"), (cfg.Maps.Basement, "basement"), (cfg.Maps.Grove, "grove"),
        })
        {
            if (!world.MapHandler.Maps.TryGetValue(mapId, out var map) || map is null) continue;
            int stalkers = 0;
            foreach (var npc in map.NPCs)
                if (npc.NPCTemplateID == cfg.Npcs.Stalker && npc.State == NPC.States.Alive) stalkers++;
            ctx.Send($"{name} ({mapId}): {HalloweenAmbush.LiveAmbushMobs(map)} ambush, {HalloweenAmbush.LivePhantoms(map)} phantom, {stalkers} stalker");
        }
        long now = world.TimeNow;
        if (nextChainsawAt > now)
            ctx.Send("Next chainsaw: in " + (nextChainsawAt - now) / world.TimerFrequency + "s");
        else
            ctx.Send("Next chainsaw: not scheduled");
    }

    private static string TierState(GameWorld world, int tier) => Halloween.Tier(world, tier) ? "on" : "off";

    private void SpawnBoss(CommandContext ctx, int templateId)
    {
        var world = ctx.World;
        if (templateId == 0)
        {
            ctx.Send("The boss is not configured.");
            return;
        }
        var template = world.NPCHandler.GetNPCTemplate(templateId);
        if (template is null)
        {
            ctx.Send("The boss template is missing.");
            return;
        }
        var npc = world.NPCHandler.SpawnNPC(world, ctx.Player.MapID, ctx.Player.MapX + 1, ctx.Player.MapY, template, shouldRespawn: false);
        ctx.Send(npc is null
            ? "The boss could not be spawned."
            : $"Spawned {template.Name} at {npc.MapX},{npc.MapY}.");
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

return typeof(HalloweenController);
