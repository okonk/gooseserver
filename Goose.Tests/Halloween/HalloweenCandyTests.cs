using System.Text.Json.Nodes;
using Goose;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenCandyTests
{
    [Fact]
    public void Level50_respawning_mob_awards_base_candy_with_chance_per_player()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var npc = kit.Fixture.SpawnNpc(map, 5, 5,
            kit.Fixture.AddNpcTemplate(92001, "Town Mob"), shouldRespawn: true);
        var a = kit.Fixture.CommandPlayerOn(map, 1, 1, "A");
        var b = kit.Fixture.CommandPlayerOn(map, 2, 2, "B");

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        HalloweenTestKit.Call(candy.Object, "AwardKill", npc, new List<Player> { a, b }, kit.World,
            kit.World.TimeNow, new HalloweenTestKit.FixedRandom([0.1, 0.0, 0.9]));

        Assert.True(a.Inventory.HasItem(kit.CandyId, 1));
        Assert.Contains(a.Sent, s => s.Contains("[Event]") && s.Contains("+1 Candy"));
        Assert.False(b.Inventory.HasItem(kit.CandyId, 1));
        Assert.DoesNotContain(b.Sent, s => s.Contains("Candy"));
    }

    [Fact]
    public void Non_level50_mob_awards_nothing()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var npc = kit.Fixture.SpawnNpc(map, 5, 5,
            kit.Fixture.AddNpcTemplate(92002, "Low Mob", t => t.Level = 10), shouldRespawn: true);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        HalloweenTestKit.Call(candy.Object, "AwardKill", npc, new List<Player> { player }, kit.World,
            kit.World.TimeNow, new HalloweenTestKit.FixedRandom([]));

        Assert.False(player.Inventory.HasItem(kit.CandyId));
        Assert.DoesNotContain(player.Sent, s => s.Contains("Candy"));
    }

    [Fact]
    public void Template_table_overrides_level_rule()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var npc = kit.Fixture.SpawnNpc(map, 5, 5,
            kit.Fixture.AddNpcTemplate(92003, "Weeper"), shouldRespawn: true);
        kit.SetConfig(cfg => cfg["candy"]!["byTemplate"]!["92003"] = new JsonArray(3, 3));
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        HalloweenTestKit.Call(candy.Object, "AwardKill", npc, new List<Player> { player }, kit.World,
            kit.World.TimeNow, new HalloweenTestKit.FixedRandom([0.0]));

        Assert.True(player.Inventory.HasItem(kit.CandyId, 3));
        Assert.Contains(player.Sent, s => s.Contains("+3 Candy"));
    }

    [Fact]
    public void Tagged_temporary_npc_awards_its_range()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var npc = kit.Fixture.SpawnNpc(map, 5, 5,
            kit.Fixture.AddNpcTemplate(92004, "Ambush Mob", t => t.Level = 10), shouldRespawn: false);
        npc.Properties["candyMin"] = 3;
        npc.Properties["candyMax"] = 5;
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        HalloweenTestKit.Call(candy.Object, "AwardKill", npc, new List<Player> { player }, kit.World,
            kit.World.TimeNow, new HalloweenTestKit.FixedRandom([0.5]));

        Assert.True(player.Inventory.HasItem(kit.CandyId, 4));
        Assert.Contains(player.Sent, s => s.Contains("+4 Candy"));
    }

    [Fact]
    public void Untagged_temporary_npc_awards_nothing()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var npc = kit.Fixture.SpawnNpc(map, 5, 5,
            kit.Fixture.AddNpcTemplate(92005, "Add Mob", t => t.Level = 10), shouldRespawn: false);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        HalloweenTestKit.Call(candy.Object, "AwardKill", npc, new List<Player> { player }, kit.World,
            kit.World.TimeNow, new HalloweenTestKit.FixedRandom([]));

        Assert.False(player.Inventory.HasItem(kit.CandyId));
        Assert.DoesNotContain(player.Sent, s => s.Contains("Candy"));
    }

    [Fact]
    public void Dimension_map_kill_awards_nothing()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(100038, "Dimension Town", 40, 40);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var npc = kit.Fixture.SpawnNpc(map, 5, 5,
            kit.Fixture.AddNpcTemplate(92006, "Clone Mob"), shouldRespawn: true);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        HalloweenTestKit.Call(candy.Object, "AwardKill", npc, new List<Player> { player }, kit.World,
            kit.World.TimeNow, new HalloweenTestKit.FixedRandom([]));

        Assert.False(player.Inventory.HasItem(kit.CandyId));
        Assert.DoesNotContain(player.Sent, s => s.Contains("Candy"));
    }

    [Fact]
    public void Inactive_event_awards_nothing()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        var npc = kit.Fixture.SpawnNpc(map, 5, 5,
            kit.Fixture.AddNpcTemplate(92007, "Town Mob"), shouldRespawn: true);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        HalloweenTestKit.Call(candy.Object, "AwardKill", npc, new List<Player> { player }, kit.World,
            kit.World.TimeNow, new HalloweenTestKit.FixedRandom([]));

        Assert.False(player.Inventory.HasItem(kit.CandyId));
        Assert.DoesNotContain(player.Sent, s => s.Contains("Candy"));
    }

    [Fact]
    public void Candy_stacks_without_registering_a_new_item()
    {
        using var kit = new HalloweenTestKit();
        kit.Fixture.AddBaseItemTemplate(kit.CandyId, "Candy", ItemTemplate.UseTypes.OneTime,
            t => { t.Slot = ItemTemplate.ItemSlots.Misc; t.StackSize = 9999; });
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        int before = kit.World.ItemHandler.GetItems().Count();

        HalloweenTestKit.Call(candy.Object, "GiveCandy", kit.World, player, 1);
        Assert.Equal(before + 1, kit.World.ItemHandler.GetItems().Count());
        Assert.True(player.Inventory.HasItem(kit.CandyId, 1));

        HalloweenTestKit.Call(candy.Object, "GiveCandy", kit.World, player, 5);
        Assert.Equal(before + 1, kit.World.ItemHandler.GetItems().Count());
        Assert.True(player.Inventory.HasItem(kit.CandyId, 6));
        Assert.Contains(player.Sent, s => s.Contains("+5 Candy"));
    }

    [Fact]
    public void Full_bag_drops_candy_at_feet()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(30, "Town", 40, 40);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);
        var candle = kit.World.ItemHandler.GetTemplate(kit.RitualCandleId)!;
        for (int i = 0; i < kit.Fixture.Settings.InventorySize; i++)
        {
            var item = new Item();
            item.LoadFromTemplate(candle);
            kit.World.ItemHandler.AddAndAssignId(item, kit.World);
            Assert.True(player.Inventory.AddItem(item, 1, kit.World));
        }

        var candy = kit.Compile<IGlobalScript>("Global", "HalloweenCandy.csx");
        int before = kit.World.ItemHandler.GetItems().Count();
        HalloweenTestKit.Call(candy.Object, "GiveCandy", kit.World, player, 3);

        Assert.Equal(before + 1, kit.World.ItemHandler.GetItems().Count());
        var tile = Assert.IsType<ItemTile>(map.GetTile(1, 1));
        Assert.Equal(kit.CandyId, tile.ItemSlot.Item.TemplateID);
        Assert.Equal(3, tile.ItemSlot.Stack);
        Assert.Same(player, tile.Owner);
        Assert.Contains(player.Sent, s => s.Contains("+3 Candy"));
    }
}
