using System.Reflection;
using System.Text.Json.Nodes;
using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HauntedBasementMapTests
{
    [Fact]
    public void Basement_refuses_without_tier2()
    {
        var (kit, map, player, _) = Setup();
        kit.World.CommunityGoals.SetFlag("halloween.tier2", 0, null);

        Assert.False(map.PlayerCanJoin(player, kit.World));
        Assert.Contains(player.Sent, s => s.Contains("The basement door won't budge."));

        kit.World.CommunityGoals.SetFlag("halloween.tier2", 1, null);
        var allowed = kit.Fixture.CommandPlayerOn(map, 21, 21, "B");
        Assert.True(map.PlayerCanJoin(allowed, kit.World));
        Assert.DoesNotContain(allowed.Sent, s => s.StartsWith("$7"));
    }

    [Fact]
    public void Clone_basement_refuses_entry()
    {
        var (kit, map, player, _) = Setup(mapId: 40);

        Assert.False(map.PlayerCanJoin(player, kit.World));
        Assert.Contains(player.Sent, s => s.Contains("The basement only exists in this world."));
    }

    [Fact]
    public void Inactive_basement_refuses_entry()
    {
        var (kit, map, player, _) = Setup(active: false);

        Assert.False(map.PlayerCanJoin(player, kit.World));
        Assert.Contains(player.Sent, s => s.Contains("The basement is dark and the door is locked."));
    }

    [Fact]
    public void Entering_the_clone_while_inactive_is_not_evicted()
    {
        var (kit, map, player, _) = Setup(mapId: 100039, active: false);
        kit.Fixture.AddBaseMap(38, "Grove", 48, 48);

        map.AddPlayer(player, kit.World);

        Assert.Equal(100039, player.MapID);
        Assert.DoesNotContain(player.Sent, s => s.Contains("The party is over"));
    }

    [Fact]
    public void Candles_in_the_clone_are_unrestricted()
    {
        var (kit, map, player, script) = Setup(mapId: 100039);
        var points = Points(kit);
        var tile = PlaceCandle(kit, map, points[0]);
        player.MapX = points[0][0];
        player.MapY = points[0][1];

        Assert.Null(HalloweenTestKit.Call(script.Object, "CanPlayerDropItem", map, player, NewCandle(kit), 1L, kit.World));
        Assert.Null(HalloweenTestKit.Call(script.Object, "CanPlayerPickupItem", map, player, tile, kit.World));

        script.Object.OnPlayerDroppedItem(map, player, tile, kit.World);
        Assert.DoesNotContain(player.Sent, s => s.Contains("A candle flickers to life."));
    }

    [Fact]
    public void Entering_with_tier2_off_warps_non_gms_back_to_the_house()
    {
        var (kit, map, player, _) = Setup();
        kit.World.CommunityGoals.SetFlag("halloween.tier2", 0, null);
        var house = kit.Fixture.AddBaseMap(48, "House", 64, 64);

        map.AddPlayer(player, kit.World);
        FinishWarp(kit, player, house);

        Assert.Equal(48, player.MapID);
        Assert.Equal((43, 53), (player.MapX, player.MapY));
        Assert.Contains(player.Sent, s => s.Contains("The basement door won't budge."));

        var gm = kit.Fixture.CommandPlayerOn(map, 20, 21, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        map.AddPlayer(gm, kit.World);
        Assert.Equal(39, gm.MapID);
        Assert.DoesNotContain(gm.Sent, s => s.Contains("won't budge"));

        kit.World.CommunityGoals.SetFlag("halloween.tier2", 1, null);
        var allowed = kit.Fixture.CommandPlayerOn(map, 22, 22, "B");
        map.AddPlayer(allowed, kit.World);
        Assert.Equal(39, allowed.MapID);
        Assert.DoesNotContain(allowed.Sent, s => s.Contains("won't budge"));
    }

    [Fact]
    public void Candle_on_point_is_counted_and_cannot_be_picked_up()
    {
        var (kit, map, player, script) = Setup();
        var points = Points(kit);
        var tile = PlaceCandle(kit, map, points[0]);

        Assert.Equal(1, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);

        var refusal = HalloweenTestKit.Call(script.Object, "CanPlayerPickupItem", map, player, tile, kit.World);
        Assert.Equal("The candle is fixed to the floor.", refusal);

        map.AddPlayer(player, kit.World);
        script.Object.OnPlayerDroppedItem(map, player, tile, kit.World);
        Assert.Contains(player.Sent, s => s.Contains("A candle flickers to life."));
        Assert.Contains(player.Sent, s => s.Contains("1 of 5"));

        var loose = PlaceCandle(kit, map, [1, 1]);
        Assert.Null(HalloweenTestKit.Call(script.Object, "CanPlayerPickupItem", map, player, loose, kit.World));
    }

    [Fact]
    public void Second_candle_on_lit_point_is_refused()
    {
        var (kit, map, player, script) = Setup();
        var points = Points(kit);
        PlaceCandle(kit, map, points[0]);
        var candle = NewCandle(kit);

        player.MapX = points[0][0];
        player.MapY = points[0][1];
        Assert.Equal("A candle already burns here.",
            HalloweenTestKit.Call(script.Object, "CanPlayerDropItem", map, player, candle, 1L, kit.World));

        player.MapX = points[1][0];
        player.MapY = points[1][1];
        Assert.Null(HalloweenTestKit.Call(script.Object, "CanPlayerDropItem", map, player, candle, 1L, kit.World));

        player.MapX = points[0][0];
        player.MapY = points[0][1];
        var candy = NewItem(kit, kit.CandyId);
        Assert.Null(HalloweenTestKit.Call(script.Object, "CanPlayerDropItem", map, player, candy, 1L, kit.World));
    }

    [Fact]
    public void Swept_candle_is_no_longer_counted()
    {
        var (kit, map, _, script) = Setup();
        var tile = PlaceCandle(kit, map, Points(kit)[0]);

        Assert.Equal(1, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);

        map.RemoveItem(tile, kit.World);
        Assert.Equal(0, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);
    }

    [Fact]
    public void Midnight_with_five_candles_consumes_them_and_spawns_groundskeeper()
    {
        var (kit, map, player, script) = Setup();
        var points = Points(kit);
        foreach (var point in points)
            PlaceCandle(kit, map, point);
        var bones = kit.World.NPCHandler.GetNPCTemplate(kit.RestlessBonesId)!;
        var mob = kit.Fixture.SpawnNpc(map, 30, 20, bones,
            properties: new PropertiesDictionary { ["ambush"] = 1 });
        var ghost = kit.Fixture.SpawnNpc(map, 32, 20, kit.World.NPCHandler.GetNPCTemplate(kit.WailingGhostId)!);
        map.AddPlayer(player, kit.World);

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 0, 0, 0));

        Assert.Equal(0, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);
        foreach (var point in points)
            Assert.Null(map.GetTile(point[0], point[1]));
        Assert.Contains(player.Sent, s => s.Contains("The clock strikes midnight. The candles gutter out."));
        var keeper = map.NPCs.Single(n => n.NPCTemplateID == kit.GroundskeeperId);
        Assert.Equal((67, 13), (keeper.MapX, keeper.MapY));
        Assert.False(keeper.ShouldRespawn);
        Assert.Equal(NPC.States.Dead, mob.State);
        Assert.Equal(NPC.States.Alive, ghost.State);
    }

    [Fact]
    public void Midnight_with_zero_groundskeeper_keeps_the_candles()
    {
        var (kit, map, player, script) = Setup();
        kit.SetConfig(cfg => cfg["npcs"]!["groundskeeper"] = 0);
        var points = Points(kit);
        foreach (var point in points)
            PlaceCandle(kit, map, point);
        map.AddPlayer(player, kit.World);

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 0, 0, 0));

        Assert.Equal(5, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);
        Assert.DoesNotContain(player.Sent, s => s.Contains("midnight"));
        Assert.DoesNotContain(player.Sent, s => s.Contains("Nothing answers."));
        Assert.Empty(map.NPCs);
    }

    [Fact]
    public void Midnight_with_unresolvable_groundskeeper_keeps_the_candles()
    {
        var (kit, map, player, script) = Setup();
        kit.SetConfig(cfg => cfg["npcs"]!["groundskeeper"] = 99999);
        var points = Points(kit);
        foreach (var point in points)
            PlaceCandle(kit, map, point);
        map.AddPlayer(player, kit.World);

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 0, 0, 0));

        Assert.Equal(5, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);
        Assert.DoesNotContain(player.Sent, s => s.Contains("midnight"));
        Assert.DoesNotContain(player.Sent, s => s.Contains("Nothing answers."));
        Assert.Empty(map.NPCs);
    }

    [Fact]
    public void Clock_does_nothing_while_inactive()
    {
        var (kit, map, player, script) = Setup();
        var points = Points(kit);
        foreach (var point in points)
            PlaceCandle(kit, map, point);
        map.AddPlayer(player, kit.World);
        kit.World.CommunityGoals.SetFlag("halloween.active", 0, null);

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 0, 0, 0));

        Assert.Equal(5, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);
        Assert.DoesNotContain(player.Sent, s => s.Contains("midnight"));
        Assert.DoesNotContain(player.Sent, s => s.Contains("Nothing answers."));
        Assert.DoesNotContain(map.NPCs, n => n.NPCTemplateID == kit.GroundskeeperId);
    }

    [Fact]
    public void Empty_pentagram_config_never_summons()
    {
        var (kit, map, player, script) = Setup();
        kit.SetConfig(cfg => cfg["pentagram"]!["points"] = new JsonArray());
        map.AddPlayer(player, kit.World);

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 0, 0, 0));

        Assert.DoesNotContain(player.Sent, s => s.Contains("midnight"));
        Assert.DoesNotContain(player.Sent, s => s.Contains("Nothing answers."));
        Assert.Empty(map.NPCs);
    }

    [Fact]
    public void Midnight_with_four_candles_answers_nothing_and_keeps_them()
    {
        var (kit, map, player, script) = Setup();
        var points = Points(kit);
        foreach (var point in points.Take(4))
            PlaceCandle(kit, map, point);
        map.AddPlayer(player, kit.World);

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 0, 0, 0));

        Assert.Contains(player.Sent, s => s.Contains("Nothing answers."));
        Assert.Equal(4, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);
        Assert.DoesNotContain(map.NPCs, n => n.NPCTemplateID == kit.GroundskeeperId);
    }

    [Fact]
    public void No_summon_while_groundskeeper_alive()
    {
        var (kit, map, player, script) = Setup();
        var points = Points(kit);
        foreach (var point in points)
            PlaceCandle(kit, map, point);
        var keeper = kit.Fixture.SpawnNpc(map, 67, 13, kit.World.NPCHandler.GetNPCTemplate(kit.GroundskeeperId)!);
        map.AddPlayer(player, kit.World);

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 0, 0, 0));

        Assert.Equal(5, (int)HalloweenTestKit.Call(script.Object, "LitPoints", map, kit.World)!);
        Assert.Equal(NPC.States.Alive, keeper.State);
        Assert.Single(map.NPCs, n => n.NPCTemplateID == kit.GroundskeeperId);
        Assert.DoesNotContain(player.Sent, s => s.Contains("Nothing answers."));
        Assert.DoesNotContain(player.Sent, s => s.Contains("midnight"));
    }

    [Fact]
    public void Quarter_to_warning_once_per_hour()
    {
        var (kit, map, player, script) = Setup();
        map.AddPlayer(player, kit.World);

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 13, 45, 0));
        Assert.Contains(player.Sent, s => s.Contains("Somewhere in the house, a clock begins to tick louder…"));

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 13, 46, 0));
        Assert.Single(player.Sent, s => s.Contains("tick louder"));

        HalloweenTestKit.Call(script.Object, "ClockTick", kit.World, map, new DateTime(2024, 10, 31, 14, 45, 0));
        Assert.Equal(2, player.Sent.Count(s => s.Contains("tick louder")));
    }

    [Fact]
    public void OnLoad_starts_a_self_rescheduling_clock_timer()
    {
        var (kit, map, _, script) = Setup();

        script.Object.OnLoad(map, kit.World);

        var timers = PendingScriptTimers(kit.World);
        Assert.Single(timers);
        timers[0].Ready(kit.World);
        Assert.Single(PendingScriptTimers(kit.World));
    }

    private static (HalloweenTestKit Kit, Map Map, TestWorldFixture.CapturingPlayer Player, Script<IMapScript> Script) Setup(
        int mapId = 39, bool active = true)
    {
        var kit = new HalloweenTestKit();
        var script = kit.Compile<IMapScript>("Map", "HauntedBasement.csx");
        var map = kit.Fixture.AddBaseMap(mapId, "Basement", 80, 24);
        map.Script = script;
        if (active) kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        kit.World.CommunityGoals.SetFlag("halloween.tier2", 1, null);
        var player = kit.Fixture.CommandPlayerOn(map, 20, 20, "A");
        return (kit, map, player, script);
    }

    private static int[][] Points(HalloweenTestKit kit)
    {
        var json = JsonNode.Parse(File.ReadAllText(kit.ConfigPath))!.AsObject();
        return json["pentagram"]!["points"]!.AsArray()
            .Select(p => p.AsArray().Select(v => v.GetValue<int>()).ToArray())
            .ToArray();
    }

    private static Item NewItem(HalloweenTestKit kit, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(kit.World.ItemHandler.GetTemplate(templateId)!);
        kit.World.ItemHandler.AddAndAssignId(item, kit.World);
        return item;
    }

    private static Item NewCandle(HalloweenTestKit kit) => NewItem(kit, kit.RitualCandleId);

    private static ItemTile PlaceCandle(HalloweenTestKit kit, Map map, int[] point)
    {
        var tile = new ItemTile
        {
            ItemSlot = new ItemSlot { Item = NewCandle(kit), Stack = 1 },
            X = point[0],
            Y = point[1],
        };
        map.AddItem(tile, kit.World);
        return tile;
    }

    private static void FinishWarp(HalloweenTestKit kit, Player player, Map map)
    {
        var ev = new DoneLoadingMapEvent { Player = player, Ticks = kit.World.TimeNow };
        kit.World.EventHandler.AddEvent(ev);
        kit.World.EventHandler.Update(kit.World);
        Assert.Equal(map, player.Map);
    }

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
