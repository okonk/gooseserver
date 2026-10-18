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

    // Placeholder until the stalker implementation lands.
    public static bool DespawnStalkerFor(GameWorld world, Player player) => false;

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

    public static void GiveCandy(GameWorld world, Player player, int amount)
    {
        var cfg = HalloweenConfig.Get(world);
        var template = world.ItemHandler.GetTemplate(cfg.Items.Candy);
        if (template is null || amount <= 0) return;

        var candy = new Item();
        if (!candy.LoadFromTemplate(template)) return;

        bool stackable = false;
        foreach (var slot in player.Inventory.GetInventorySlots())
        {
            if (slot is not null && slot.CanStack(new ItemSlot { Item = candy, Stack = amount }, amount))
            {
                stackable = true;
                break;
            }
        }

        if (stackable)
        {
            // The stack path discards this unregistered item, so repeated grants never
            // grow ItemHandler.items (ItemHandler.cs:279).
            player.Inventory.AddItem(candy, amount, world);
        }
        else
        {
            world.ItemHandler.AddAndAssignId(candy, world);
            if (!player.Inventory.AddItem(candy, amount, world))
            {
                var tile = new ItemTile
                {
                    ItemSlot = new ItemSlot { Item = candy, Stack = amount },
                    X = player.MapX,
                    Y = player.MapY,
                    Owner = player,
                };
                player.Map.PlaceItem(tile);

                var maptile = player.Map.GetTile(tile.X, tile.Y) as ItemTile;
                if (maptile is not null)
                {
                    maptile.ItemSlot.Stack += tile.ItemSlot.Stack;
                    world.SendToMap(player.Map, P.MakeObject(maptile));
                }
                else
                {
                    player.Map.AddItem(tile, world);
                }
            }
        }

        Feed(world, player, "+" + amount + " Candy");
    }

    public static void Feed(GameWorld world, Player player, string text) =>
        world.Send(player, P.Notice(NoticeType.Info, "[Event] " + text));

    public static long Ticks(GameWorld world, double seconds) => (long)(world.TimerFrequency * seconds);

    public static void EvictPlayer(GameWorld world, Player player)
    {
        if (player.IsGM) return;
        var cfg = HalloweenConfig.Get(world);
        if (cfg.Maps.GroveExit.Length < 2) return;
        if (!world.MapHandler.Maps.TryGetValue(cfg.Maps.Grove, out var grove) || grove is null) return;
        player.WarpTo(world, grove, cfg.Maps.GroveExit[0], cfg.Maps.GroveExit[1]);
        Feed(world, player, "The party is over. The door closes behind you.");
    }

    public static void CheckCostume(GameWorld world, Player player, long now)
    {
        if (!Active(world) || player.IsGM) return;
        var cfg = HalloweenConfig.Get(world);
        if (player.Map is null || (!IsHouse(player.Map, cfg) && !IsBasement(player.Map, cfg))) return;

        if (IsInCostume(player, cfg))
        {
            SetState(player, "hw.costumeDeadline", 0);
            CancelHunt(world, player);
            return;
        }

        long deadline = GetState(player, "hw.costumeDeadline");
        if (deadline != 0) return;

        Feed(world, player, "You feel exposed. Put your costume back on.");
        deadline = now + Ticks(world, cfg.Stalker.CostumeGraceSeconds);
        SetState(player, "hw.costumeDeadline", deadline);

        ScriptTimerEvent.Create(() =>
        {
            if (GetState(player, "hw.costumeDeadline") != deadline) return;
            var graceCfg = HalloweenConfig.Get(world);
            if (player.Map is null || (!IsHouse(player.Map, graceCfg) && !IsBasement(player.Map, graceCfg))) return;
            if (IsInCostume(player, graceCfg)) return;
            SetState(player, "hw.costumeDeadline", 0);
            SpawnStalker(world, player, "hunt", world.TimeNow);
        }, TimeSpan.FromSeconds(cfg.Stalker.CostumeGraceSeconds), world);
    }

    private static void CancelHunt(GameWorld world, Player player)
    {
        long npcId = GetState(player, "hw.stalkerNpc");
        if (npcId == 0 || player.Map is null) return;
        var npc = player.Map.NPCs.FirstOrDefault(n => n.LoginID == npcId);
        if (npc is null || npc.State != NPC.States.Alive) return;
        if (!npc.Properties.TryGetProperty<string>("stalkerMode", out var mode) || mode is not "hunt") return;
        SetState(player, "hw.huntCancelled", 1);
        DespawnStalkerFor(world, player);
    }
}
