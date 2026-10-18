using System;
using System.Linq;
using Goose;

public static class Halloween
{
    public static bool Active(GameWorld world) => world.CommunityGoals.GetFlag("halloween.active") != 0;
    public static bool Tier(GameWorld world, int n) => world.CommunityGoals.GetFlag("halloween.tier" + n) != 0;
    public static void SetActive(GameWorld world, bool on) => world.CommunityGoals.SetFlag("halloween.active", on ? 1 : 0, null);
    public static void SetTier(GameWorld world, int n, bool on) => world.CommunityGoals.SetFlag("halloween.tier" + n, on ? 1 : 0, null);

    // Set from the settings base, not the current value, so granting twice cannot stack.
    public static void ApplyXpBonus(GameWorld world)
    {
        world.ExperienceModifier = world.Settings.ExperienceModifier + HalloweenConfig.Get(world).XpBonus;
    }

    // Placeholder until the stalker implementation lands; the controller compiles against this.
    public static bool SpawnStalker(GameWorld world, Player target, string mode, long now) => false;

    public static bool IsHouse(Map map, HalloweenConfig cfg) => map.ID == cfg.Maps.House;
    public static bool IsBasement(Map map, HalloweenConfig cfg) => map.ID == cfg.Maps.Basement;
    public static bool IsEventMap(Map map, HalloweenConfig cfg) =>
        IsHouse(map, cfg) || IsBasement(map, cfg) || map.ID == cfg.Maps.Grove;

    public static bool IsInCostume(Player player, HalloweenConfig cfg)
    {
        foreach (var slot in player.Inventory.GetEquippedSlots())
        {
            if (slot is null) continue;
            int id = slot.Item.TemplateID;
            if (id == cfg.Items.PartyCostume || cfg.Items.Masks.Contains(id)) return true;
        }
        return false;
    }

    public static HalloweenRoom? RoomAt(HalloweenConfig cfg, int x, int y)
    {
        foreach (var room in cfg.Rooms)
        {
            if (x >= room.Rect[0] && x < room.Rect[0] + room.Rect[2] &&
                y >= room.Rect[1] && y < room.Rect[1] + room.Rect[3])
                return room;
        }
        return null;
    }

    public static double Weight(HalloweenConfig cfg, int x, int y)
    {
        var room = RoomAt(cfg, x, y);
        return room is null ? cfg.Ambush.DefaultWeight : room.Weight;
    }

    public static string Table(HalloweenConfig cfg, int x, int y)
    {
        var room = RoomAt(cfg, x, y);
        return room is null ? "house" : room.Table;
    }

    public static long GetState(Player player, string key) =>
        player.ScriptState.TryGetValue(key, out var value) ? value : 0;

    public static void SetState(Player player, string key, long value) => player.ScriptState[key] = value;

    public static void Feed(GameWorld world, Player player, string text) =>
        world.Send(player, P.Notice(NoticeType.Info, "[Event] " + text));

    public static long Ticks(GameWorld world, double seconds) => (long)(world.TimerFrequency * seconds);
}
