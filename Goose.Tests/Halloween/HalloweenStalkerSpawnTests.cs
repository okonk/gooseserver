using System.Reflection;
using System.Text.Json.Nodes;
using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenStalkerSpawnTests
{
    private static readonly long Now = 1000;

    [Fact]
    public void Spawns_behind_target_with_properties()
    {
        var (kit, halloween, map, player) = Setup();
        player.Facing = Direction.Up;
        kit.Fixture.SpawnNpc(map, 12, 16, kit.World.NPCHandler.GetNPCTemplate(kit.StalkerId)!);

        Assert.True(Spawn(halloween, kit.World, player, "chase", Now));

        var npc = map.NPCs.Single(n => n.Properties.TryGetProperty<int>("stalkerTarget", out _));
        Assert.Equal(kit.StalkerId, npc.NPCTemplateID);
        Assert.Equal((12, 15), (npc.MapX, npc.MapY));
        Assert.True(npc.Properties.TryGetProperty<int>("stalkerTarget", out int targetId));
        Assert.Equal(player.PlayerID, targetId);
        Assert.True(npc.Properties.TryGetProperty<string>("stalkerMode", out string mode));
        Assert.Equal("chase", mode);
        Assert.True(npc.Properties.TryGetProperty<long>("spawnedAt", out long spawnedAt));
        Assert.Equal(Now, spawnedAt);
        Assert.Equal(npc.LoginID, player.ScriptState["hw.stalkerNpc"]);
        Assert.Contains(player.Sent, s => s.Contains("A chainsaw revs behind you!"));
    }

    [Fact]
    public void Refuses_during_grace_and_over_cap_for_chase()
    {
        var (kit, halloween, map, player) = Setup();
        player.Facing = Direction.Up;

        player.ScriptState["hw.stalkerGraceUntil"] = Now + 100;
        Assert.False(Spawn(halloween, kit.World, player, "chase", Now));
        Assert.Empty(map.NPCs);
        Assert.Equal(0, player.ScriptState.GetValueOrDefault("hw.stalkerNpc"));

        player.ScriptState["hw.stalkerGraceUntil"] = 0;
        var a = KitPlayer(kit, map, 14, 12);
        var b = KitPlayer(kit, map, 16, 12);
        var c = KitPlayer(kit, map, 18, 12);
        Assert.True(Spawn(halloween, kit.World, player, "chase", Now));
        Assert.True(Spawn(halloween, kit.World, a, "chase", Now + 1));
        Assert.True(Spawn(halloween, kit.World, b, "chase", Now + 2));
        Assert.Equal(3, LiveStalkers(halloween, kit.World));
        Assert.False(Spawn(halloween, kit.World, c, "chase", Now + 3));
        Assert.Equal(3, map.NPCs.Count);
    }

    [Fact]
    public void Hunt_ignores_grace_and_cap()
    {
        var (kit, halloween, map, player) = Setup(config: cfg => cfg["stalker"]!["maxLive"] = 1);
        player.Facing = Direction.Up;
        player.ScriptState["hw.stalkerGraceUntil"] = Now + 100;
        var a = KitPlayer(kit, map, 14, 12);

        Assert.True(Spawn(halloween, kit.World, a, "chase", Now));
        Assert.True(Spawn(halloween, kit.World, player, "hunt", Now + 1));

        var hunt = map.NPCs.Single(n => n.LoginID == (int)player.ScriptState["hw.stalkerNpc"]);
        Assert.Equal((12, 16), (hunt.MapX, hunt.MapY));
        Assert.True(hunt.Properties.TryGetProperty<string>("stalkerMode", out string mode));
        Assert.Equal("hunt", mode);
        Assert.Equal(2, map.NPCs.Count);
    }

    [Fact]
    public void One_stalker_per_target()
    {
        var (kit, halloween, map, player) = Setup();
        player.Facing = Direction.Up;

        Assert.True(Spawn(halloween, kit.World, player, "chase", Now));
        var first = Assert.Single(map.NPCs);
        Assert.False(Spawn(halloween, kit.World, player, "chase", Now + 1));
        Assert.Equal(first.LoginID, player.ScriptState["hw.stalkerNpc"]);
        Assert.Single(map.NPCs);
    }

    [Fact]
    public void Chainsaw_needs_three_players_and_skips_foyer_and_uncostumed()
    {
        var kit = new HalloweenTestKit();
        kit.SetConfig(cfg => Rooms(cfg));
        var controller = kit.Compile<IGlobalScript>("Global", "Halloween.csx").Object;
        var house = kit.Fixture.AddBaseMap(48, "House", 48, 48);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([0.5, 0.5, 0.5], [2]));

        var a = KitPlayer(kit, house, 12, 12);
        Equip(kit, a, kit.PartyCostumeId);
        a.Facing = Direction.Up;
        var b = KitPlayer(kit, house, 14, 12);
        Equip(kit, b, kit.PartyCostumeId);

        long freq = kit.World.TimerFrequency;
        long t0 = Now;
        Chainsaw(controller, kit.World, t0, kit.World.Random);
        long t1 = t0 + freq * 30 * 60;
        Chainsaw(controller, kit.World, t1, kit.World.Random);
        Assert.DoesNotContain(a.Sent, s => s.Contains("A chainsaw revs somewhere in the house"));
        Assert.Empty(PendingScriptTimers(kit.World));

        var gm = KitPlayer(kit, house, 16, 12, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        Equip(kit, gm, kit.PartyCostumeId);
        var foyer = KitPlayer(kit, house, 2, 2);
        Equip(kit, foyer, kit.PartyCostumeId);
        var uncostumed = KitPlayer(kit, house, 12, 14);
        long t2 = t1 + freq * 30 * 60;
        Chainsaw(controller, kit.World, t2, kit.World.Random);
        foreach (var p in house.Players.OfType<TestWorldFixture.CapturingPlayer>())
            Assert.Contains(p.Sent, s => s.Contains("A chainsaw revs somewhere in the house"));

        var timers = PendingScriptTimers(kit.World);
        Assert.Single(timers);
        timers[0].Ready(kit.World);

        var npc = Assert.Single(house.NPCs);
        Assert.Equal(kit.StalkerId, npc.NPCTemplateID);
        Assert.Equal((12, 16), (npc.MapX, npc.MapY));
        Assert.True(npc.Properties.TryGetProperty<int>("stalkerTarget", out int targetId));
        Assert.Equal(a.PlayerID, targetId);
        Assert.Equal(npc.LoginID, a.ScriptState["hw.stalkerNpc"]);
        Assert.Contains(a.Sent, s => s.Contains("A chainsaw revs behind you!"));
    }

    [Fact]
    public void Refuses_outside_event_maps_and_in_basement_with_live_groundskeeper()
    {
        var (kit, halloween, map, player) = Setup();
        var grove = kit.Fixture.AddBaseMap(38, "Grove", 48, 48);
        var grovePlayer = kit.Fixture.CommandPlayerOn(grove, 5, 5, "G");
        grove.AddPlayer(grovePlayer, kit.World);

        Assert.False(Spawn(halloween, kit.World, grovePlayer, "chase", Now));

        var basement = kit.Fixture.AddBaseMap(39, "Basement", 48, 48);
        var first = KitPlayer(kit, basement, 5, 5, "C1");
        Assert.True(Spawn(halloween, kit.World, first, "chase", Now));

        var groundskeeper = kit.Fixture.SpawnNpc(basement, 8, 8,
            kit.World.NPCHandler.GetNPCTemplate(kit.GroundskeeperId)!);
        var second = KitPlayer(kit, basement, 20, 20, "C2");
        Assert.False(Spawn(halloween, kit.World, second, "chase", Now + 1));
        Assert.Equal(1, basement.NPCs.Count(n => n.NPCTemplateID == kit.StalkerId));

        groundskeeper.Despawn(kit.World);
        Assert.True(Spawn(halloween, kit.World, second, "hunt", Now + 2));

        kit.SetConfig(cfg => cfg["npcs"]!["stalker"] = 0);
        var third = KitPlayer(kit, basement, 30, 30, "C3");
        Assert.False(Spawn(halloween, kit.World, third, "hunt", Now + 3));
    }

    [Fact]
    public void Stalker_refusal_and_despawn_span_house_and_basement()
    {
        var (kit, halloween, house, player) = Setup();
        player.Facing = Direction.Up;
        Assert.True(Spawn(halloween, kit.World, player, "chase", Now));
        var stalker = Assert.Single(house.NPCs);

        var basement = kit.Fixture.AddBaseMap(39, "Basement", 48, 48);
        player.WarpTo(kit.World, basement, 5, 5);

        Assert.False(Spawn(halloween, kit.World, player, "chase", Now + 1));
        Assert.Single(house.NPCs);
        Assert.Equal(stalker.LoginID, player.ScriptState["hw.stalkerNpc"]);

        Assert.True(Despawn(halloween, kit.World, player));
        Assert.Empty(house.NPCs);
        Assert.Equal(NPC.States.Dead, stalker.State);
        Assert.Equal(0, player.ScriptState["hw.stalkerNpc"]);
    }

    private static (HalloweenTestKit Kit, Type Halloween, Map Map, TestWorldFixture.CapturingPlayer Player) Setup(
        int x = 12, int y = 12, Action<JsonObject>? config = null)
    {
        var kit = new HalloweenTestKit();
        kit.SetConfig(cfg =>
        {
            Rooms(cfg);
            config?.Invoke(cfg);
        });
        var asm = kit.Compile<IGlobalScript>("Global", "Halloween.csx").Object.GetType().Assembly;
        var type = asm.GetTypes().FirstOrDefault(t => t.Name == "Halloween")
            ?? throw new InvalidOperationException("Halloween type not found");
        var map = kit.Fixture.AddBaseMap(48, "House", 48, 48);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var player = kit.Fixture.CommandPlayerOn(map, x, y, "A");
        player.PlayerID = x * 100 + y;
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

    private static TestWorldFixture.CapturingPlayer KitPlayer(HalloweenTestKit kit, Map map, int x, int y, string name = "P")
    {
        var player = kit.Fixture.CommandPlayerOn(map, x, y, name + x + y);
        player.PlayerID = x * 100 + y;
        map.AddPlayer(player, kit.World);
        return player;
    }

    private static void Equip(HalloweenTestKit kit, Player player, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(kit.World.ItemHandler.GetTemplate(templateId)!);
        kit.World.ItemHandler.AddAndAssignId(item, kit.World);
        Assert.True(player.Inventory.AddItem(item, 1, kit.World));
        Assert.True(player.Inventory.Equip(item, kit.World));
    }

    private static bool Spawn(Type halloween, GameWorld world, Player player, string mode, long now)
        => (bool)halloween.GetMethod("SpawnStalker")!.Invoke(null, new object?[] { world, player, mode, now })!;

    private static bool Despawn(Type halloween, GameWorld world, Player player)
        => (bool)halloween.GetMethod("DespawnStalkerFor")!.Invoke(null, new object?[] { world, player })!;

    private static int LiveStalkers(Type halloween, GameWorld world)
        => (int)halloween.GetMethod("LiveStalkers")!.Invoke(null, new object?[] { world })!;

    private static void Chainsaw(object controller, GameWorld world, long now, Random rng)
        => HalloweenTestKit.Call(controller, "Chainsaw", world, now, rng);

    private static void SetWorldRandom(GameWorld world, Random rng)
        => typeof(GameWorld).GetField("rng", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(world, rng);

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
