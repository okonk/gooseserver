#load "HalloweenConfig.csx"
#load "HalloweenCommon.csx"
using System;
using Goose;

public static class HalloweenAmbush
{
    public static bool OnStep(GameWorld world, Map map, Player player, long now, Random rng)
    {
        var cfg = HalloweenConfig.Get(world);
        if (!Halloween.Active(world)) return false;
        if (!Halloween.IsEventMap(map, cfg)) return false;
        if (player.State != Player.States.Ready) return false;
        if (player.IsGMInvisible) return false;

        bool basement = Halloween.IsBasement(map, cfg);
        double weight = basement
            ? cfg.Ambush.BasementWeight
            : Halloween.Weight(cfg, player.MapX, player.MapY);
        if (weight <= 0) return false;
        if (basement && GroundskeeperAlive(map, cfg)) return false;
        if (now < Halloween.GetState(player, "hw.ambushReadyAt")) return false;
        if (rng.NextDouble() >= cfg.Ambush.BaseChance * weight) return false;

        string table = basement ? "basement" : Halloween.Table(cfg, player.MapX, player.MapY);
        Trigger(world, map, player, PickOutcome(cfg, table, rng), now, rng);
        return true;
    }

    public static void Trigger(GameWorld world, Map map, Player player, string outcome, long now, Random rng)
    {
        var cfg = HalloweenConfig.Get(world);
        bool basement = Halloween.IsBasement(map, cfg);

        if (outcome is "fake")
        {
            Fake(world, player, basement, now, rng);
            return;
        }
        if (outcome is "stalker")
        {
            if (!Halloween.SpawnStalker(world, player, "chase", now))
            {
                Fake(world, player, basement, now, rng);
                return;
            }
            GroupCooldown(world, map, player, now, basement);
            return;
        }
        if (outcome is not ("ghosts" or "bones" or "bloody" or "phantom")) return;
        if (outcome is "phantom" &&
            LivePhantoms(map) >= (basement ? cfg.Ambush.MaxPhantomsBasement : cfg.Ambush.MaxPhantomsHouse))
            outcome = basement ? "bones" : "ghosts";

        int templateId;
        string packName, candyKey, feed;
        switch (outcome)
        {
            case "bones":
                templateId = cfg.Npcs.RestlessBones;
                packName = "bones";
                candyKey = "bones";
                feed = "Bones rise from the piles!";
                break;
            case "bloody":
                templateId = cfg.Npcs.BloodyGhost;
                packName = "bloody";
                candyKey = "bloody";
                feed = "Ghosts pour out of the walls!";
                break;
            case "phantom":
                templateId = cfg.Npcs.PinkPhantom;
                packName = "";
                candyKey = "phantom";
                feed = "A pink glow fills the room…";
                break;
            default:
                templateId = rng.NextDouble() < 2.0 / 3.0 ? cfg.Npcs.WailingGhost : cfg.Npcs.FlickeringGhost;
                packName = templateId == cfg.Npcs.WailingGhost ? "wailing" : "flickering";
                candyKey = "ghost";
                feed = "Ghosts pour out of the walls!";
                break;
        }

        int size = outcome is "phantom"
            ? 1
            : PackSize(cfg, packName, rng) + NearbyReady(map, player, cfg.Ambush.PackRadius);
        size = Math.Min(size, cfg.Ambush.PackMax);
        int live = LiveAmbushMobs(map);
        if (live + size > cfg.Ambush.MaxLivePerMap) size = cfg.Ambush.MaxLivePerMap - live;
        if (size <= 0)
        {
            Fake(world, player, basement, now, rng);
            return;
        }

        var template = world.NPCHandler.GetNPCTemplate(templateId);
        if (template is null) return;
        int[] candy = cfg.Ambush.Candy.GetValueOrDefault(candyKey) ?? [0, 0];

        var room = Halloween.RoomAt(cfg, player.MapX, player.MapY);
        Direction dir = player.Facing;
        long hint = Halloween.GetState(player, "hw.hintDir");
        if (hint is >= 1 and <= 4)
        {
            dir = Rotate(player.Facing, (int)hint);
            Halloween.SetState(player, "hw.hintDir", 0);
        }

        int spawned = 0;
        for (int i = 0; i < size; i++)
        {
            var tiles = CandidateTiles(map, player, room, dir);
            if (tiles.Count == 0) break;
            var (x, y) = tiles[rng.Next(tiles.Count)];

            var properties = new PropertiesDictionary
            {
                ["ambush"] = 1,
                ["candyMin"] = candy[0],
                ["candyMax"] = candy[1],
                ["spawnedAt"] = now,
            };
            if (outcome is "phantom") properties["phantom"] = 1;

            var npc = world.NPCHandler.SpawnNPC(world, map.ID, x, y, template, shouldRespawn: false, properties);
            if (npc is null) continue;

            npc.AddAggro(player, 1, world);
            npc.AddAttackEvent(world);
            npc.AddMoveEvent(world);
            DespawnTimer(world, npc, cfg.Ambush.DespawnSeconds);
            spawned++;
        }

        if (spawned == 0)
        {
            Fake(world, player, basement, now, rng);
            return;
        }
        Halloween.Feed(world, player, feed);
        GroupCooldown(world, map, player, now, basement);
    }

    public static int LiveAmbushMobs(Map map) =>
        map.NPCs.Count(n => n.State == NPC.States.Alive && n.Properties.TryGetProperty<int>("ambush", out _));

    public static int LivePhantoms(Map map) =>
        map.NPCs.Count(n => n.State == NPC.States.Alive &&
                            n.Properties.TryGetProperty<int>("ambush", out _) &&
                            n.Properties.TryGetProperty<int>("phantom", out _));

    private static bool GroundskeeperAlive(Map map, HalloweenConfig cfg) =>
        map.NPCs.Any(n => n.NPCTemplateID == cfg.Npcs.Groundskeeper && n.State == NPC.States.Alive);

    private static int NearbyReady(Map map, Player player, int radius) =>
        map.Players.Count(p => p != player && p.State == Player.States.Ready &&
                               Math.Abs(p.MapX - player.MapX) <= radius &&
                               Math.Abs(p.MapY - player.MapY) <= radius);

    private static string PickOutcome(HalloweenConfig cfg, string table, Random rng)
    {
        if (!cfg.Ambush.Tables.TryGetValue(table, out HalloweenAmbushTable? t) || t is null)
            table = "house";
        if (!cfg.Ambush.Tables.TryGetValue(table, out t) || t is null) return "fake";
        var options = new (string Name, int Weight)[]
        {
            ("fake", t.Fake), ("ghosts", t.Ghosts), ("bones", t.Bones),
            ("bloody", t.Bloody), ("phantom", t.Phantom), ("stalker", t.Stalker),
        };
        int total = options.Sum(o => o.Weight);
        if (total <= 0) return "fake";
        int pick = rng.Next(total);
        foreach (var (name, weight) in options)
        {
            pick -= weight;
            if (pick < 0) return name;
        }
        return "fake";
    }

    private static int PackSize(HalloweenConfig cfg, string pack, Random rng)
    {
        if (!cfg.Ambush.Packs.TryGetValue(pack, out HalloweenPack? p) || p is null) return 1;
        return p.Min == p.Max ? p.Min : rng.Next(p.Min, p.Max + 1);
    }

    private static void Fake(GameWorld world, Player player, bool basement, long now, Random rng)
    {
        var cfg = HalloweenConfig.Get(world);
        bool directional = rng.NextDouble() < 0.25;
        var lines = cfg.Ambush.FakeScares.GetValueOrDefault(
            directional ? "directional" : basement ? "basement" : "house");
        if (lines is { Length: > 0 })
        {
            string line = lines[rng.Next(lines.Length)];
            if (directional)
            {
                int rel = RelativeDirection(line);
                if (rel != 0) Halloween.SetState(player, "hw.hintDir", rel);
            }
            Halloween.Feed(world, player, line);
        }
        int cooldown = basement
            ? cfg.Ambush.BasementFakeCooldownSeconds ?? cfg.Ambush.FakeCooldownSeconds
            : cfg.Ambush.FakeCooldownSeconds;
        Halloween.SetState(player, "hw.ambushReadyAt", now + Halloween.Ticks(world, cooldown));
    }

    // hw.hintDir stores the direction a directional scare line names, relative to the
    // player: 1=behind, 2=left, 3=right, 4=front.
    private static int RelativeDirection(string line)
    {
        string l = line.ToLowerInvariant();
        if (l.Contains("behind")) return 1;
        if (l.Contains("left")) return 2;
        if (l.Contains("right")) return 3;
        if (l.Contains("front") || l.Contains("ahead")) return 4;
        return 0;
    }

    private static Direction Rotate(Direction facing, int hint)
    {
        int delta = hint switch { 1 => 2, 2 => 3, 3 => 1, _ => 0 };
        return (Direction)(((int)facing - 1 + delta) % 4 + 1);
    }

    private static List<(int X, int Y)> CandidateTiles(Map map, Player player, HalloweenRoom? room, Direction dir)
    {
        var (dx, dy) = dir switch
        {
            Direction.Up => (0, -1),
            Direction.Right => (1, 0),
            Direction.Down => (0, 1),
            _ => (-1, 0),
        };
        var tiles = new List<(int, int)>();
        for (int d = 2; d <= 4; d++)
            AddIfFree(map, room, player.MapX + dx * d, player.MapY + dy * d, tiles);
        if (tiles.Count > 0) return tiles;
        for (int ox = -3; ox <= 3; ox++)
            for (int oy = -3; oy <= 3; oy++)
            {
                int dist = Math.Max(Math.Abs(ox), Math.Abs(oy));
                if (dist is < 1 or > 3) continue;
                AddIfFree(map, room, player.MapX + ox, player.MapY + oy, tiles);
            }
        return tiles;
    }

    private static void AddIfFree(Map map, HalloweenRoom? room, int x, int y, List<(int, int)> tiles)
    {
        if (room is not null &&
            (x < room.Rect[0] || x >= room.Rect[0] + room.Rect[2] ||
             y < room.Rect[1] || y >= room.Rect[1] + room.Rect[3])) return;
        if (!map.IsTileBlocked(null, x, y)) tiles.Add((x, y));
    }

    private static void GroupCooldown(GameWorld world, Map map, Player player, long now, bool basement)
    {
        var cfg = HalloweenConfig.Get(world);
        long readyAt = now + Halloween.Ticks(world,
            basement ? cfg.Ambush.BasementCooldownSeconds : cfg.Ambush.CooldownSeconds);
        Halloween.SetState(player, "hw.ambushReadyAt", readyAt);
        foreach (var other in map.Players)
        {
            if (other == player || other.State != Player.States.Ready) continue;
            if (Math.Abs(other.MapX - player.MapX) > cfg.Ambush.GroupCooldownRadius ||
                Math.Abs(other.MapY - player.MapY) > cfg.Ambush.GroupCooldownRadius) continue;
            Halloween.SetState(other, "hw.ambushReadyAt", readyAt);
        }
    }

    // The action only touches host objects (npc, world), so it stays valid across
    // /reloadscripts; it never calls back into this compilation.
    private static void DespawnTimer(GameWorld world, NPC npc, int despawnSeconds)
    {
        ScriptTimerEvent? timer = null;
        timer = ScriptTimerEvent.Create(() =>
        {
            if (npc.State != NPC.States.Alive) return;
            var target = npc.AggroTarget;
            if (target is null || target.Map != npc.Map ||
                Math.Abs(target.MapX - npc.MapX) >= Map.RANGE_X ||
                Math.Abs(target.MapY - npc.MapY) >= Map.RANGE_Y)
            {
                npc.Despawn(world);
                return;
            }
            timer!.Reschedule(TimeSpan.FromSeconds(30), world);
        }, TimeSpan.FromSeconds(despawnSeconds), world);
    }
}
