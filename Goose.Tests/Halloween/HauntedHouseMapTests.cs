using System.Reflection;
using Goose;
using Goose.Events;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HauntedHouseMapTests
{
    private const long Now = 1000;

    [Fact]
    public void Clone_house_refuses_entry()
    {
        var (kit, map, player, _) = Setup(mapId: 49);

        Assert.False(map.PlayerCanJoin(player, kit.World));
        Assert.Contains(player.Sent, s => s.Contains("The house only exists in this world."));
    }

    [Fact]
    public void Inactive_house_refuses_entry()
    {
        var (kit, map, player, _) = Setup(active: false);

        Assert.False(map.PlayerCanJoin(player, kit.World));
        Assert.Contains(player.Sent, s => s.Contains("The house is dark and the door is locked."));
    }

    [Fact]
    public void Active_house_allows_entry()
    {
        var (kit, map, player, _) = Setup();

        Assert.True(map.PlayerCanJoin(player, kit.World));
        Assert.DoesNotContain(player.Sent, s => s.StartsWith("$7"));
    }

    [Fact]
    public void Entering_without_costume_starts_the_grace()
    {
        var (kit, map, player, _) = Setup();
        long before = kit.World.TimeNow;

        map.AddPlayer(player, kit.World);

        Assert.Contains(player.Sent, s => s.Contains("You feel exposed. Put your costume back on."));
        long freq = kit.World.TimerFrequency;
        Assert.InRange(player.ScriptState["hw.costumeDeadline"], before + freq * 10, kit.World.TimeNow + freq * 10);
        Assert.Single(PendingScriptTimers(kit.World));
    }

    [Fact]
    public void Entering_while_inactive_evicts_to_grove_but_not_gms()
    {
        var (kit, map, player, _) = Setup(active: false);
        kit.Fixture.AddBaseMap(38, "Grove", 48, 48);

        map.AddPlayer(player, kit.World);
        Assert.Equal(38, player.MapID);
        Assert.Equal((38, 37), (player.MapX, player.MapY));
        Assert.Contains(player.Sent, s => s.Contains("The party is over. The door closes behind you."));

        var gm = kit.Fixture.CommandPlayerOn(map, 20, 20, "GM");
        gm.Access = Player.AccessStatus.GameMaster;
        map.AddPlayer(gm, kit.World);
        Assert.Equal(48, gm.MapID);
        Assert.DoesNotContain(gm.Sent, s => s.Contains("The party is over"));
    }

    [Fact]
    public void Removing_costume_warns_then_spawns_hunt_stalker_after_grace()
    {
        var (kit, map, player, script) = Setup();
        Equip(kit, player, kit.PartyCostumeId);
        map.AddPlayer(player, kit.World);
        Assert.DoesNotContain(player.Sent, s => s.Contains("You feel exposed"));
        Assert.Equal(0, player.ScriptState.GetValueOrDefault("hw.costumeDeadline"));

        Unequip(player, kit.World, kit.PartyCostumeId);
        CheckCostume(script, kit.World, player, Now);

        Assert.Contains(player.Sent, s => s.Contains("You feel exposed. Put your costume back on."));
        Assert.True(player.ScriptState["hw.costumeDeadline"] > 0);
        var timers = PendingScriptTimers(kit.World);
        Assert.Single(timers);

        timers[0].Ready(kit.World);
        Assert.Equal(0, player.ScriptState["hw.costumeDeadline"]);
        var hunt = Assert.Single(map.NPCs);
        Assert.Equal(kit.StalkerId, hunt.NPCTemplateID);
        Assert.True(hunt.Properties.TryGetProperty<string>("stalkerMode", out string mode));
        Assert.Equal("hunt", mode);
        Assert.True(hunt.Properties.TryGetProperty<int>("stalkerTarget", out int targetId));
        Assert.Equal(player.PlayerID, targetId);
        Assert.Equal(hunt.LoginID, player.ScriptState["hw.stalkerNpc"]);
        Assert.Contains(player.Sent, s => s.Contains("A chainsaw revs behind you!"));
    }

    [Fact]
    public void Putting_costume_back_on_within_grace_cancels_the_hunt()
    {
        var (kit, map, player, script) = Setup();
        map.AddPlayer(player, kit.World);
        Assert.Contains(player.Sent, s => s.Contains("You feel exposed"));
        Assert.True(player.ScriptState["hw.costumeDeadline"] > 0);

        Equip(kit, player, kit.PartyCostumeId);
        CheckCostume(script, kit.World, player, Now + 1);
        Assert.Equal(0, player.ScriptState["hw.costumeDeadline"]);

        var timers = PendingScriptTimers(kit.World);
        Assert.Single(timers);
        timers[0].Ready(kit.World);

        Assert.Empty(map.NPCs);
        Assert.Equal(0, player.ScriptState.GetValueOrDefault("hw.stalkerNpc"));
        Assert.Equal(0, player.ScriptState["hw.costumeDeadline"]);
        Assert.Equal(1, player.Sent.Count(s => s.Contains("You feel exposed")));
    }

    [Fact]
    public void Swapping_costume_for_a_mask_does_not_warn()
    {
        var (kit, map, player, script) = Setup();
        Equip(kit, player, kit.PartyCostumeId);
        map.AddPlayer(player, kit.World);

        Equip(kit, player, kit.MaskIds[0]);
        CheckCostume(script, kit.World, player, Now);
        CheckCostume(script, kit.World, player, Now);

        Assert.DoesNotContain(player.Sent, s => s.Contains("You feel exposed"));
        Assert.Equal(0, player.ScriptState.GetValueOrDefault("hw.costumeDeadline"));
        var equipped = player.Inventory.GetEquippedSlots().First(s => s is not null)!;
        Assert.Equal(kit.MaskIds[0], equipped.Item.TemplateID);
    }

    [Fact]
    public void Gm_removing_costume_never_warns()
    {
        var (kit, map, player, script) = Setup();
        player.Access = Player.AccessStatus.GameMaster;
        Equip(kit, player, kit.PartyCostumeId);
        map.AddPlayer(player, kit.World);

        Unequip(player, kit.World, kit.PartyCostumeId);
        CheckCostume(script, kit.World, player, Now);

        Assert.DoesNotContain(player.Sent, s => s.Contains("You feel exposed"));
        Assert.Equal(0, player.ScriptState.GetValueOrDefault("hw.costumeDeadline"));
        Assert.Empty(PendingScriptTimers(kit.World));
    }

    [Fact]
    public void Costume_requip_cancels_active_hunt_but_not_chase()
    {
        var (kit, map, player, script) = Setup();
        var template = kit.World.NPCHandler.GetNPCTemplate(kit.StalkerId)!;
        var hunt = kit.Fixture.SpawnNpc(map, 22, 22, template,
            properties: new PropertiesDictionary { ["stalkerMode"] = "hunt" });
        player.ScriptState["hw.stalkerNpc"] = hunt.LoginID;
        Equip(kit, player, kit.PartyCostumeId);
        map.AddPlayer(player, kit.World);
        Assert.Equal(1, player.ScriptState["hw.huntCancelled"]);

        var chase = kit.Fixture.SpawnNpc(map, 24, 24, template,
            properties: new PropertiesDictionary { ["stalkerMode"] = "chase" });
        player.ScriptState["hw.stalkerNpc"] = chase.LoginID;
        player.ScriptState["hw.huntCancelled"] = 0;
        CheckCostume(script, kit.World, player, Now + 1);
        Assert.Equal(0, player.ScriptState["hw.huntCancelled"]);
    }

    [Fact]
    public void Player_step_rolls_ambush()
    {
        var (kit, map, player, script) = Setup();
        // One mob only: with world.Random swapped for FixedRandom, a second spawn would
        // draw the same exhausted ID and spin in NPCHandler.GetNewID (NPCHandler.cs:287).
        kit.SetConfig(cfg => cfg["ambush"]!["packs"]!["wailing"] = 1);
        map.AddPlayer(player, kit.World);
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([0.0, 0.0, 0.5], [50, 0]));

        script.Object.OnPlayerMove(map, player, kit.World);

        Assert.Equal(1, map.NPCs.Count);
        Assert.Equal(kit.WailingGhostId, map.NPCs[0].NPCTemplateID);
        Assert.Contains(player.Sent, s => s.Contains("Ghosts pour out of the walls!"));
    }

    private static (HalloweenTestKit Kit, Map Map, TestWorldFixture.CapturingPlayer Player, Script<IMapScript> Script) Setup(
        int mapId = 48, bool active = true)
    {
        var kit = new HalloweenTestKit();
        var script = kit.Compile<IMapScript>("Map", "HauntedHouse.csx");
        var map = kit.Fixture.AddBaseMap(mapId, "House", 48, 48);
        map.Script = script;
        if (active) kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var player = kit.Fixture.CommandPlayerOn(map, 20, 20, "A");
        return (kit, map, player, script);
    }

    private static void CheckCostume(Script<IMapScript> script, GameWorld world, Player player, long now)
    {
        var type = script.Object.GetType().Assembly.GetTypes().First(t => t.Name == "Halloween");
        type.GetMethod("CheckCostume")!.Invoke(null, new object?[] { world, player, now });
    }

    private static void Equip(HalloweenTestKit kit, Player player, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(kit.World.ItemHandler.GetTemplate(templateId)!);
        kit.World.ItemHandler.AddAndAssignId(item, kit.World);
        Assert.True(player.Inventory.AddItem(item, 1, kit.World));
        Assert.True(player.Inventory.Equip(item, kit.World));
    }

    private static void Unequip(Player player, GameWorld world, int templateId)
    {
        var slots = player.Inventory.GetEquippedSlots();
        int index = Array.FindIndex(slots, s => s is not null && s.Item.TemplateID == templateId);
        Assert.NotEqual(-1, index);
        Assert.True(player.Inventory.Unequip((Inventory.EquipSlots)index, world));
    }

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
