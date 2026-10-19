using System.Reflection;
using System.Text.Json.Nodes;
using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenAmbushTests
{
    private static readonly long Now = 1000;

    [Fact]
    public void Foyer_never_rolls()
    {
        var (kit, ambush, map, player) = Setup(x: 2, y: 2);

        Assert.False(OnStep(ambush, kit.World, map, player, Now, new HalloweenTestKit.FixedRandom([0.0])));

        Assert.Empty(player.Sent);
        Assert.Empty(map.NPCs);
        Assert.Equal(0, player.ScriptState.GetValueOrDefault("hw.ambushReadyAt"));
    }

    [Fact]
    public void Gm_invisible_never_rolls()
    {
        var (kit, ambush, map, player) = Setup();
        player.Access = Player.AccessStatus.GameMaster;
        Assert.True(player.IsGMInvisible);

        Assert.False(OnStep(ambush, kit.World, map, player, Now, new HalloweenTestKit.FixedRandom([0.0])));

        Assert.Empty(player.Sent);
        Assert.Empty(map.NPCs);
        Assert.Equal(0, player.ScriptState.GetValueOrDefault("hw.ambushReadyAt"));
    }

    [Fact]
    public void Roll_below_chance_triggers_and_sets_group_cooldown()
    {
        var (kit, ambush, map, player) = Setup();
        var near = KitPlayer(kit, map, 12, 13);
        var far = KitPlayer(kit, map, 12, 24);

        Assert.True(OnStep(ambush, kit.World, map, player, Now,
            new HalloweenTestKit.FixedRandom([0.0], [50, 0, 0, 0])));

        long readyAt = Now + kit.World.TimerFrequency * 40;
        Assert.Equal(readyAt, player.ScriptState["hw.ambushReadyAt"]);
        Assert.Equal(readyAt, near.ScriptState["hw.ambushReadyAt"]);
        Assert.Equal(0, far.ScriptState.GetValueOrDefault("hw.ambushReadyAt"));
        Assert.Equal(3, map.NPCs.Count);
        Assert.All(map.NPCs, n => Assert.Equal(kit.WailingGhostId, n.NPCTemplateID));
        Assert.Contains(player.Sent, s => s.Contains("Ghosts pour out of the walls!"));
    }

    [Fact]
    public void Fake_scare_sets_only_own_short_cooldown()
    {
        var (kit, ambush, map, player) = Setup();
        var near = KitPlayer(kit, map, 12, 13);

        Trigger(ambush, kit.World, map, player, "fake", Now,
            new HalloweenTestKit.FixedRandom([1.0], [0]));

        Assert.Equal(Now + kit.World.TimerFrequency * 15, player.ScriptState["hw.ambushReadyAt"]);
        Assert.Equal(0, near.ScriptState.GetValueOrDefault("hw.ambushReadyAt"));
        Assert.Empty(map.NPCs);
        Assert.Contains(player.Sent, s => s.Contains("The portrait's eyes follow you…"));
    }

    [Fact]
    public void Pack_grows_with_nearby_players_and_caps_at_five()
    {
        var (kit, ambush, map, player) = Setup();
        Nearby(kit, map, [(12, 13), (13, 12)]);

        Trigger(ambush, kit.World, map, player, "ghosts", Now,
            new HalloweenTestKit.FixedRandom([0.0], [0, 0, 0, 0]));
        Assert.Equal(4, map.NPCs.Count);
        foreach (var npc in map.NPCs.ToList()) npc.Despawn(kit.World);

        Nearby(kit, map, [(11, 12), (12, 11), (13, 13), (14, 12)]);
        Trigger(ambush, kit.World, map, player, "ghosts", Now,
            new HalloweenTestKit.FixedRandom([0.0], [0, 0, 0, 0, 0]));
        Assert.Equal(5, map.NPCs.Count);
    }

    [Fact]
    public void Mobs_spawn_ahead_of_the_player_in_the_same_room()
    {
        var (kit, ambush, map, player) = Setup();

        Trigger(ambush, kit.World, map, player, "ghosts", Now,
            new HalloweenTestKit.FixedRandom([0.0], [0, 0]));

        Assert.Equal(2, map.NPCs.Count);
        foreach (var n in map.NPCs)
        {
            Assert.True(n.MapY < player.MapY,
                $"mob at {n.MapX},{n.MapY} is not ahead of player at {player.MapX},{player.MapY}");
            Assert.InRange(n.MapX, 10, 19);
            Assert.InRange(n.MapY, 10, 19);
        }
    }

    [Fact]
    public void Live_cap_shrinks_pack_and_turns_full_cap_into_fake()
    {
        var (kit, ambush, map, player) = Setup(config: cfg => cfg["ambush"]!["maxLivePerMap"] = 3);
        var near = KitPlayer(kit, map, 12, 13);
        KitPlayer(kit, map, 13, 12);

        Trigger(ambush, kit.World, map, player, "ghosts", Now,
            new HalloweenTestKit.FixedRandom([0.0], [0, 0, 0]));
        Assert.Equal(3, map.NPCs.Count);

        long second = Now + 1;
        Trigger(ambush, kit.World, map, player, "ghosts", second,
            new HalloweenTestKit.FixedRandom([0.0, 0.5], [0]));

        Assert.Equal(3, map.NPCs.Count);
        Assert.Equal(second + kit.World.TimerFrequency * 15, player.ScriptState["hw.ambushReadyAt"]);
        Assert.Equal(Now + kit.World.TimerFrequency * 40, near.ScriptState["hw.ambushReadyAt"]);
        Assert.Contains(player.Sent, s => s.Contains("The portrait's eyes follow you…"));
    }

    [Fact]
    public void Second_phantom_becomes_ghosts_in_house()
    {
        var (kit, ambush, map, player) = Setup();

        Trigger(ambush, kit.World, map, player, "phantom", Now,
            new HalloweenTestKit.FixedRandom([], [0]));
        Assert.Equal(1, LivePhantoms(ambush, map));
        Assert.Equal(1, map.NPCs.Count);
        Assert.True(map.NPCs[0].Properties.TryGetProperty<int>("phantom", out int tag));
        Assert.Equal(1, tag);

        Trigger(ambush, kit.World, map, player, "phantom", Now + 1,
            new HalloweenTestKit.FixedRandom([0.0], [0, 0]));
        Assert.Equal(1, LivePhantoms(ambush, map));
        Assert.Equal(3, LiveAmbushMobs(ambush, map));
        Assert.Equal(1, map.NPCs.Count(n => n.NPCTemplateID == kit.PinkPhantomId));
        Assert.Equal(2, map.NPCs.Count(n => n.NPCTemplateID == kit.WailingGhostId));
    }

    [Fact]
    public void Basement_rolls_basement_table_even_inside_house_room_rect()
    {
        var (kit, ambush, map, player) = Setup(mapId: 39, x: 31, y: 31);

        Assert.True(OnStep(ambush, kit.World, map, player, Now,
            new HalloweenTestKit.FixedRandom([0.0, 0.0], [20, 0, 0, 0])));

        Assert.Equal(3, map.NPCs.Count);
        Assert.All(map.NPCs, n => Assert.Equal(kit.RestlessBonesId, n.NPCTemplateID));
        Assert.Contains(player.Sent, s => s.Contains("Bones rise from the piles!"));
    }

    [Fact]
    public void Basement_fake_scare_uses_basement_fake_cooldown()
    {
        var (kit, ambush, map, player) = Setup(mapId: 39, x: 5, y: 5);
        kit.SetConfig(cfg => cfg["ambush"]!["basementFakeCooldownSeconds"] = 2);

        Trigger(ambush, kit.World, map, player, "fake", Now,
            new HalloweenTestKit.FixedRandom([1.0], [0]));

        Assert.Equal(Now + kit.World.TimerFrequency * 2, player.ScriptState["hw.ambushReadyAt"]);
    }

    [Fact]
    public void Basement_never_rolls_while_groundskeeper_alive()
    {
        var (kit, ambush, map, player) = Setup(mapId: 39, x: 5, y: 5);
        var gk = kit.Fixture.SpawnNpc(map, 8, 8, kit.World.NPCHandler.GetNPCTemplate(kit.GroundskeeperId)!);

        Assert.False(OnStep(ambush, kit.World, map, player, Now, new HalloweenTestKit.FixedRandom([0.0])));
        Assert.DoesNotContain(player.Sent, s => s.Contains("[Event]"));

        gk.Despawn(kit.World);
        Assert.True(OnStep(ambush, kit.World, map, player, Now, new HalloweenTestKit.FixedRandom([0.0, 0.5], [0])));
        Assert.Contains(player.Sent, s => s.Contains("Something drips in the dark."));
    }

    [Fact]
    public void Unfought_ambush_mob_despawns_after_timeout()
    {
        var (kit, ambush, map, player) = Setup();

        Trigger(ambush, kit.World, map, player, "ghosts", Now,
            new HalloweenTestKit.FixedRandom([0.0], [0, 0]));
        var fought = map.NPCs[0];
        var unfought = map.NPCs[1];
        unfought.AggroTarget = null;

        var timers = PendingScriptTimers(kit.World);
        Assert.Equal(2, timers.Count);
        foreach (var timer in timers) timer.Ready(kit.World);

        Assert.Contains(map.NPCs, n => ReferenceEquals(n, fought));
        Assert.DoesNotContain(map.NPCs, n => ReferenceEquals(n, unfought));
        Assert.Equal(NPC.States.Dead, unfought.State);
        Assert.Single(PendingScriptTimers(kit.World));
    }

    [Fact]
    public void Ambush_mob_is_tagged_for_candy()
    {
        var (kit, ambush, map, player) = Setup();

        Trigger(ambush, kit.World, map, player, "ghosts", Now,
            new HalloweenTestKit.FixedRandom([0.0], [0, 0]));

        var mob = map.NPCs[0];
        Assert.True(mob.Properties.TryGetProperty<int>("ambush", out int ambushTag));
        Assert.Equal(1, ambushTag);
        Assert.True(mob.Properties.TryGetProperty<int>("candyMin", out int min));
        Assert.True(mob.Properties.TryGetProperty<int>("candyMax", out int max));
        Assert.Equal(3, min);
        Assert.Equal(5, max);
        Assert.True(mob.Properties.TryGetProperty<long>("spawnedAt", out long spawnedAt));
        Assert.Equal(Now, spawnedAt);
    }

    [Fact]
    public void Directional_fake_hints_next_spawn_behind()
    {
        var (kit, ambush, map, player) = Setup();

        Trigger(ambush, kit.World, map, player, "fake", Now,
            new HalloweenTestKit.FixedRandom([0.1], [0]));
        Assert.Contains(player.Sent, s => s.Contains("Footsteps behind you."));
        Assert.Equal(1, player.ScriptState["hw.hintDir"]);

        Trigger(ambush, kit.World, map, player, "ghosts", Now + 1,
            new HalloweenTestKit.FixedRandom([0.0], [0, 0]));
        Assert.Equal(0, player.ScriptState["hw.hintDir"]);
        Assert.Equal(2, map.NPCs.Count);
        foreach (var n in map.NPCs)
            Assert.True(n.MapY > player.MapY,
                $"mob at {n.MapX},{n.MapY} is not behind player at {player.MapX},{player.MapY}");
    }

    private static (HalloweenTestKit Kit, Type Ambush, Map Map, TestWorldFixture.CapturingPlayer Player) Setup(
        int mapId = 48, int x = 12, int y = 12, Action<JsonObject>? config = null)
    {
        var kit = new HalloweenTestKit();
        kit.SetConfig(cfg =>
        {
            Rooms(cfg);
            config?.Invoke(cfg);
        });
        var asm = kit.Compile<IGlobalScript>("Global", "Halloween.csx").Object.GetType().Assembly;
        var type = asm.GetTypes().FirstOrDefault(t => t.Name == "HalloweenAmbush")
            ?? throw new InvalidOperationException("HalloweenAmbush type not found");
        var map = kit.Fixture.AddBaseMap(mapId, "Map", 48, 48);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var player = kit.Fixture.CommandPlayerOn(map, x, y, "A");
        player.Facing = Direction.Up;
        map.AddPlayer(player, kit.World);
        return (kit, type, map, player);
    }

    private static void Rooms(JsonObject cfg)
    {
        cfg["rooms"] = new JsonArray(
            Room("Foyer", 0, 0, 5, 5, 0, "house"),
            Room("Library", 10, 10, 10, 10, 1, "house"),
            Room("Master Bedroom", 30, 30, 6, 6, 3, "master"));
    }

    private static JsonObject Room(string name, int x, int y, int w, int h, double weight, string table) => new()
    {
        ["name"] = name,
        ["rect"] = new JsonArray(x, y, w, h),
        ["weight"] = weight,
        ["table"] = table,
    };

    private static Player KitPlayer(HalloweenTestKit kit, Map map, int x, int y)
    {
        var player = kit.Fixture.CommandPlayerOn(map, x, y, "P" + x + y);
        map.AddPlayer(player, kit.World);
        return player;
    }

    private static void Nearby(HalloweenTestKit kit, Map map, (int X, int Y)[] spots)
    {
        foreach (var (x, y) in spots) KitPlayer(kit, map, x, y);
    }

    private static bool OnStep(Type t, GameWorld world, Map map, Player p, long now, Random rng)
        => (bool)t.GetMethod("OnStep")!.Invoke(null, new object?[] { world, map, p, now, rng })!;

    private static void Trigger(Type t, GameWorld world, Map map, Player p, string outcome, long now, Random rng)
        => t.GetMethod("Trigger")!.Invoke(null, new object?[] { world, map, p, outcome, now, rng });

    private static int LiveAmbushMobs(Type t, Map map)
        => (int)t.GetMethod("LiveAmbushMobs")!.Invoke(null, new object?[] { map })!;

    private static int LivePhantoms(Type t, Map map)
        => (int)t.GetMethod("LivePhantoms")!.Invoke(null, new object?[] { map })!;

    private static List<ScriptTimerEvent> PendingScriptTimers(GameWorld world)
    {
        var queue = (PriorityQueue<Event, long>)typeof(Goose.EventHandler)
            .GetField("events", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(world.EventHandler)!;
        var pending = new List<Event>();
        while (queue.TryDequeue(out Event? ev, out _)) pending.Add(ev!);
        return pending.OfType<ScriptTimerEvent>().ToList();
    }
}

