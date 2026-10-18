#load "../Global/Halloween/HalloweenCommon.csx"
#load "../Global/Halloween/HalloweenAmbush.csx"
#load "../Global/Halloween/HalloweenConfig.csx"
using System;
using Goose;
using Goose.Scripting;

public class HauntedBasement : BaseMapScript
{
    private const string WarnedHourKey = "hw.clockWarnedHour";
    private static readonly TimeSpan ClockPeriod = TimeSpan.FromSeconds(60);

    public override string? CanPlayerJoin(Map map, Player player, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (map.ID != cfg.Maps.Basement) return "The basement only exists in this world.";
        if (!Halloween.Active(world)) return "The basement is dark and the door is locked.";
        if (!Halloween.Tier(world, 2)) return "The basement door won't budge.";
        return null;
    }

    public override void OnPlayerEntered(Map map, Player player, GameWorld world)
    {
        if (!Halloween.Active(world))
        {
            Halloween.EvictPlayer(world, player);
            return;
        }
        Halloween.CheckCostume(world, player, world.TimeNow);
    }

    public override void OnPlayerMove(Map map, Player player, GameWorld world)
        => HalloweenAmbush.OnStep(world, map, player, world.TimeNow, world.Random);

    public override void OnPlayerLeft(Map map, Player player, GameWorld world)
        => Halloween.SetState(player, "hw.costumeDeadline", 0);

    public override string? CanPlayerDropItem(Map map, Player player, Item item, long stack, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (item.TemplateID != cfg.Items.RitualCandle) return null;
        if (!IsPoint(cfg, player.MapX, player.MapY)) return null;
        return CandleAt(map, cfg, player.MapX, player.MapY) ? "A candle already burns here." : null;
    }

    public override void OnPlayerDroppedItem(Map map, Player player, ItemTile tile, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (tile.ItemSlot.Item.TemplateID != cfg.Items.RitualCandle) return;
        if (!IsPoint(cfg, tile.X, tile.Y)) return;
        FeedMap(world, map, "A candle flickers to life. " + LitPoints(map, world) + " of " + cfg.Pentagram.Points.Length + ".");
    }

    public override string? CanPlayerPickupItem(Map map, Player player, ItemTile tile, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        if (tile.ItemSlot.Item.TemplateID != cfg.Items.RitualCandle) return null;
        return IsPoint(cfg, tile.X, tile.Y) ? "The candle is fixed to the floor." : null;
    }

    public override void OnLoad(Map map, GameWorld world)
    {
        ScriptTimerEvent? timer = null;
        timer = ScriptTimerEvent.Create(() =>
        {
            try
            {
                TickCurrent(world, map);
            }
            finally
            {
                timer!.Reschedule(ClockPeriod, world);
            }
        }, ClockPeriod, world);
    }

    public int LitPoints(Map map, GameWorld world)
    {
        var cfg = HalloweenConfig.Get(world);
        int lit = 0;
        foreach (var point in cfg.Pentagram.Points)
            if (point is { Length: >= 2 } && CandleAt(map, cfg, point[0], point[1])) lit++;
        return lit;
    }

    public void ClockTick(GameWorld world, Map map, DateTime local)
    {
        if (!Halloween.Active(world)) return;
        var cfg = HalloweenConfig.Get(world);

        if (local.Minute == 45)
        {
            long warned = WarnedHour(map);
            if (warned != local.Hour)
            {
                StoreWarnedHour(map, local.Hour);
                FeedMap(world, map, "Somewhere in the house, a clock begins to tick louder…");
            }
            return;
        }

        if (local.Minute != 0) return;
        if (GroundskeeperAlive(map, cfg)) return;
        int pointCount = cfg.Pentagram.Points.Length;
        if (pointCount == 0) return;
        if (LitPoints(map, world) < pointCount)
        {
            FeedMap(world, map, "Nothing answers.");
            return;
        }

        foreach (var point in cfg.Pentagram.Points)
        {
            if (point is not { Length: >= 2 }) continue;
            var tile = map.GetTile(point[0], point[1]) as ItemTile;
            if (tile is not null && tile.ItemSlot.Item.TemplateID == cfg.Items.RitualCandle)
                map.RemoveItem(tile, world);
        }
        FeedMap(world, map, "The clock strikes midnight. The candles gutter out.");
        var template = world.NPCHandler.GetNPCTemplate(cfg.Npcs.Groundskeeper);
        if (template is not null && cfg.Pentagram.Centre is { Length: >= 2 })
            world.NPCHandler.SpawnNPC(world, map.ID, cfg.Pentagram.Centre[0], cfg.Pentagram.Centre[1], template, shouldRespawn: false);
        foreach (var npc in map.NPCs.ToList())
            if (npc.State == NPC.States.Alive && npc.Properties.TryGetProperty<int>("ambush", out _))
                npc.Despawn(world);
    }

    private static bool IsPoint(HalloweenConfig cfg, int x, int y)
    {
        foreach (var point in cfg.Pentagram.Points)
            if (point is { Length: >= 2 } && point[0] == x && point[1] == y) return true;
        return false;
    }

    private static bool CandleAt(Map map, HalloweenConfig cfg, int x, int y)
    {
        var tile = map.GetTile(x, y) as ItemTile;
        return tile is not null && tile.ItemSlot.Item.TemplateID == cfg.Items.RitualCandle;
    }

    private static bool GroundskeeperAlive(Map map, HalloweenConfig cfg) =>
        map.NPCs.Any(n => n.NPCTemplateID == cfg.Npcs.Groundskeeper && n.State == NPC.States.Alive);

    private static void FeedMap(GameWorld world, Map map, string text) =>
        world.SendToMap(map, P.Notice(NoticeType.Info, "[Event] " + text));

    private static long WarnedHour(Map map) =>
        map.ScriptStore is Dictionary<string, long> store && store.TryGetValue(WarnedHourKey, out var hour) ? hour : -1;

    private static void StoreWarnedHour(Map map, long hour)
    {
        var store = map.ScriptStore as Dictionary<string, long> ?? new Dictionary<string, long>();
        store[WarnedHourKey] = hour;
        map.ScriptStore = store;
    }

    // A /reloadscripts swap replaces Object with a freshly compiled type, so the timer
    // resolves the current object through the registry instead of capturing this.
    private static void TickCurrent(GameWorld world, Map map)
    {
        var current = map.Script?.Object;
        if (current is null) return;
        current.GetType().GetMethod("ClockTick")!.Invoke(current, new object?[] { world, map, DateTime.Now });
    }
}

return typeof(HauntedBasement);
