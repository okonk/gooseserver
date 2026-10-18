using System.Text.Json.Nodes;
using Goose;
using Goose.Testing;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenCommonTests
{
    [Fact]
    public void Missing_config_disables_everything()
    {
        using var kit = new HalloweenTestKit();
        File.Delete(kit.ConfigPath);

        Assert.False(B(kit, "Active", kit.World));
        Assert.Equal(0, ConfigValue(kit, "Maps", "House"));
        Assert.Equal(0, ConfigValue(kit, "Maps", "Basement"));
        Assert.Equal(0, ConfigValue(kit, "Maps", "Grove"));
        Assert.Equal(0, ConfigValue(kit, "Npcs", "Stalker"));
    }

    [Fact]
    public void Invalid_json_disables_and_logs_once()
    {
        using var kit = new HalloweenTestKit();
        File.WriteAllText(kit.ConfigPath, "{ not json");
        using var log = new CapturingLog();

        Assert.Equal(0, ConfigValue(kit, "Maps", "House"));
        Assert.Equal(0, ConfigValue(kit, "Npcs", "Stalker"));
        Assert.Equal(0, ConfigValue(kit, "Maps", "House"));
        Assert.Equal(0, ConfigValue(kit, "Npcs", "Stalker"));

        Assert.Equal(1, log.Messages.Count(m => m.Contains("halloween.json")));
    }

    [Fact]
    public void Edited_config_is_picked_up_without_reload()
    {
        using var kit = new HalloweenTestKit();
        Assert.Equal(48, ConfigValue(kit, "Maps", "House"));

        kit.SetConfig(cfg => cfg["maps"]!["house"] = 49);

        Assert.Equal(49, ConfigValue(kit, "Maps", "House"));
    }

    [Fact]
    public void IsInCostume_true_for_costume_or_any_mask_false_for_other_helm()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(48, "Haunted House", 40, 40);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        Assert.False(B(kit, "IsInCostume", kit.World, player));

        Equip(kit, player, kit.PartyCostumeId);
        Assert.True(B(kit, "IsInCostume", kit.World, player));
        Assert.True(player.Inventory.Unequip(Inventory.EquipSlots.Head, kit.World));

        foreach (var maskId in kit.MaskIds)
        {
            Equip(kit, player, maskId);
            Assert.True(B(kit, "IsInCostume", kit.World, player));
            Assert.True(player.Inventory.Unequip(Inventory.EquipSlots.Head, kit.World));
        }

        kit.Fixture.AddBaseItemTemplate(90099, "Other Helm",
            ItemTemplate.UseTypes.Armor, t => t.Slot = ItemTemplate.ItemSlots.Helmet);
        Equip(kit, player, 90099);
        Assert.False(B(kit, "IsInCostume", kit.World, player));
    }

    [Fact]
    public void RoomAt_returns_named_room_and_null_outside()
    {
        using var kit = new HalloweenTestKit();
        var host = kit.Host;

        Assert.Equal("Foyer", HalloweenTestKit.Call(host, "RoomAt", kit.World, 2, 2));
        Assert.Equal("Master Bedroom", HalloweenTestKit.Call(host, "RoomAt", kit.World, 12, 12));
        Assert.Null(HalloweenTestKit.Call(host, "RoomAt", kit.World, 7, 7));

        Assert.Equal(0.0, (double)HalloweenTestKit.Call(host, "Weight", kit.World, 2, 2)!);
        Assert.Equal(3.0, (double)HalloweenTestKit.Call(host, "Weight", kit.World, 12, 12)!);
        Assert.Equal(1.0, (double)HalloweenTestKit.Call(host, "Weight", kit.World, 7, 7)!);
        Assert.Equal("house", HalloweenTestKit.Call(host, "Table", kit.World, 2, 2));
        Assert.Equal("master", HalloweenTestKit.Call(host, "Table", kit.World, 12, 12));
        Assert.Equal("house", HalloweenTestKit.Call(host, "Table", kit.World, 7, 7));
    }

    [Fact]
    public void Clone_map_is_not_an_event_map()
    {
        using var kit = new HalloweenTestKit();
        var house = kit.Fixture.AddBaseMap(48, "Haunted House", 40, 40);
        var clone = kit.Fixture.AddBaseMap(100048, "Haunted House (dimension)", 40, 40);

        Assert.True(B(kit, "IsHouse", kit.World, house));
        Assert.True(B(kit, "IsEventMap", kit.World, house));
        Assert.False(B(kit, "IsHouse", kit.World, clone));
        Assert.False(B(kit, "IsEventMap", kit.World, clone));
    }

    [Fact]
    public void ScriptState_round_trips_through_GetState_SetState()
    {
        using var kit = new HalloweenTestKit();
        var map = kit.Fixture.AddBaseMap(48, "Haunted House", 40, 40);
        var player = kit.Fixture.CommandPlayerOn(map, 1, 1);

        Assert.Equal(0L, (long)HalloweenTestKit.Call(kit.Host, "GetState", player, "hw.kills")!);
        HalloweenTestKit.Call(kit.Host, "SetState", player, "hw.kills", 7L);
        Assert.Equal(7L, (long)HalloweenTestKit.Call(kit.Host, "GetState", player, "hw.kills")!);
        HalloweenTestKit.Call(kit.Host, "SetState", player, "hw.kills", 9L);
        Assert.Equal(9L, (long)HalloweenTestKit.Call(kit.Host, "GetState", player, "hw.kills")!);
    }

    private static bool B(HalloweenTestKit kit, string method, params object?[] args)
        => (bool)HalloweenTestKit.Call(kit.Host, method, args)!;

    [Fact]
    public void Pack_values_parse_from_number_and_range_shapes()
    {
        using var kit = new HalloweenTestKit();

        var packs = PackDictionary(kit);
        Assert.Equal((2, 2), PackRange(packs["wailing"]!));
        Assert.Equal((3, 4), PackRange(packs["bones"]!));
    }

    [Fact]
    public void Malformed_pack_shape_disables_and_logs()
    {
        using var kit = new HalloweenTestKit();
        using var log = new CapturingLog();

        kit.SetConfig(cfg => cfg["ambush"]!["packs"]!["wailing"] = new JsonArray(3));
        Assert.Equal(0, ConfigValue(kit, "Maps", "House"));
        Assert.Equal(1, log.Messages.Count(m => m.Contains("halloween.json")));

        kit.SetConfig(cfg => cfg["ambush"]!["packs"]!["wailing"] = new JsonArray(4, 3));
        Assert.Equal(0, ConfigValue(kit, "Maps", "House"));
        Assert.Equal(2, log.Messages.Count(m => m.Contains("halloween.json")));
    }

    private static int ConfigValue(HalloweenTestKit kit, string section, string name)
    {
        var config = (object)HalloweenTestKit.Call(kit.Host, "Config", kit.World)!;
        var group = (object)config.GetType().GetProperty(section)!.GetValue(config)!;
        return (int)group.GetType().GetProperty(name)!.GetValue(group)!;
    }

    private static System.Collections.IDictionary PackDictionary(HalloweenTestKit kit)
    {
        var config = (object)HalloweenTestKit.Call(kit.Host, "Config", kit.World)!;
        var ambush = (object)config.GetType().GetProperty("Ambush")!.GetValue(config)!;
        return (System.Collections.IDictionary)ambush.GetType().GetProperty("Packs")!.GetValue(ambush)!;
    }

    private static (int, int) PackRange(object pack)
    {
        var type = pack.GetType();
        return ((int)type.GetProperty("Min")!.GetValue(pack)!, (int)type.GetProperty("Max")!.GetValue(pack)!);
    }

    private static void Equip(HalloweenTestKit kit, Player player, int templateId)
    {
        var item = new Item();
        item.LoadFromTemplate(kit.World.ItemHandler.GetTemplate(templateId)!);
        kit.World.ItemHandler.AddAndAssignId(item, kit.World);
        Assert.True(player.Inventory.AddItem(item, 1, kit.World));
        Assert.True(player.Inventory.Equip(item, kit.World));
    }
}
