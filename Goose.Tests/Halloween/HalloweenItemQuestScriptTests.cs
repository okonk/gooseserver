using System.Reflection;
using System.Text.Json;
using Goose;
using Goose.Events;
using Goose.Quests;
using Goose.Scripting;
using Goose.Testing;
using Xunit;

namespace Goose.Tests.Halloween;

[Collection("NLog")]
public class HalloweenItemQuestScriptTests
{
    [Fact]
    public void Unequipping_costume_in_house_starts_grace_and_reequipping_cancels_it()
    {
        using var kit = new HalloweenTestKit();
        var house = kit.Fixture.AddBaseMap(48, "House", 48, 48);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        var player = kit.Fixture.CommandPlayerOn(house, 5, 5, "A");
        player.PlayerID = 505;
        house.AddPlayer(player, kit.World);
        AttachCostume(kit, kit.PartyCostumeId);

        var first = CreateItem(kit, kit.PartyCostumeId);
        Assert.True(player.Inventory.AddItem(first, 1, kit.World));
        Assert.True(player.Inventory.Equip(first, kit.World));
        Assert.Equal(0, player.ScriptState.GetValueOrDefault("hw.costumeDeadline"));

        Assert.True(player.Inventory.Unequip(Inventory.EquipSlots.Head, kit.World));
        Assert.NotEqual(0, player.ScriptState["hw.costumeDeadline"]);
        Assert.Contains(player.Sent, s => s.Contains("You feel exposed"));

        var second = CreateItem(kit, kit.PartyCostumeId);
        Assert.True(player.Inventory.AddItem(second, 1, kit.World));
        Assert.True(player.Inventory.Equip(second, kit.World));
        Assert.Equal(0, player.ScriptState["hw.costumeDeadline"]);
    }

    [Fact]
    public void Mask_without_params_keeps_template_look()
    {
        using var kit = new HalloweenTestKit();
        var template = kit.World.ItemHandler.GetTemplate(kit.MaskIds[0])!;
        template.GraphicEquipped = 7;
        template.GraphicR = 10;
        template.GraphicG = 20;
        template.GraphicB = 30;
        template.GraphicA = 40;
        template.Script = kit.Compile<IItemScript>("Item", "HalloweenCostume.csx");

        var item = CreateItem(kit, kit.MaskIds[0]);

        Assert.Equal(7, item.GraphicEquipped);
        Assert.Equal((10, 20, 30, 40), (item.GraphicR, item.GraphicG, item.GraphicB, item.GraphicA));
    }

    [Fact]
    public void Costume_gets_one_of_the_configured_looks_and_keeps_it()
    {
        using var kit = new HalloweenTestKit();
        var template = kit.World.ItemHandler.GetTemplate(kit.PartyCostumeId)!;
        template.ScriptParams =
            """[{"graphic": 19, "r": 1, "g": 2, "b": 3, "a": 4}, {"graphic": 20, "r": 5, "g": 6, "b": 7, "a": 8}]""";
        template.Script = kit.Compile<IItemScript>("Item", "HalloweenCostume.csx");
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([], [1, 0]));

        var first = CreateItem(kit, kit.PartyCostumeId);
        Assert.Equal(20, first.GraphicEquipped);
        Assert.Equal((5, 6, 7, 8), (first.GraphicR, first.GraphicG, first.GraphicB, first.GraphicA));

        var second = CreateItem(kit, kit.PartyCostumeId);
        Assert.Equal(19, second.GraphicEquipped);
        Assert.Equal((1, 2, 3, 4), (second.GraphicR, second.GraphicG, second.GraphicB, second.GraphicA));

        var roundTripped = JsonSerializer.Deserialize<Item>(JsonSerializer.Serialize(first))!;
        Assert.Equal(20, roundTripped.GraphicEquipped);
        Assert.Equal((5, 6, 7, 8), (roundTripped.GraphicR, roundTripped.GraphicG, roundTripped.GraphicB, roundTripped.GraphicA));
    }

    [Fact]
    public void Costume_look_can_set_its_own_drop_tile()
    {
        using var kit = new HalloweenTestKit();
        var template = kit.World.ItemHandler.GetTemplate(kit.PartyCostumeId)!;
        template.GraphicTile = 11;
        template.GraphicFile = 22;
        template.ScriptParams =
            """[{"graphic": 19, "r": 0, "g": 0, "b": 0, "a": 0, "tile": 2281, "file": 332509}, {"graphic": 20, "r": 0, "g": 0, "b": 0, "a": 0}]""";
        template.Script = kit.Compile<IItemScript>("Item", "HalloweenCostume.csx");
        SetWorldRandom(kit.World, new HalloweenTestKit.FixedRandom([], [0, 1]));

        var tiled = CreateItem(kit, kit.PartyCostumeId);
        Assert.Equal((2281, 332509), (tiled.GraphicTile, tiled.GraphicFile));

        var plain = CreateItem(kit, kit.PartyCostumeId);
        Assert.Equal((11, 22), (plain.GraphicTile, plain.GraphicFile));

        var roundTripped = JsonSerializer.Deserialize<Item>(JsonSerializer.Serialize(tiled))!;
        Assert.Equal((2281, 332509), (roundTripped.GraphicTile, roundTripped.GraphicFile));
    }

    [Fact]
    public void Invalid_look_params_keep_template_look()
    {
        using var kit = new HalloweenTestKit();
        var template = kit.World.ItemHandler.GetTemplate(kit.PartyCostumeId)!;
        template.GraphicEquipped = 9;
        template.GraphicR = 1;
        template.GraphicG = 2;
        template.GraphicB = 3;
        template.GraphicA = 4;
        template.Script = kit.Compile<IItemScript>("Item", "HalloweenCostume.csx");

        foreach (var bad in new[]
                 {
                     "not json",
                     "[]",
                     """[{"graphic": 1}]""",
                     """[{"graphic": 1, "r": 2, "g": 3, "b": 4, "a": 5, "x": 6}]""",
                     """[{"graphic": 1, "r": 300, "g": 3, "b": 4, "a": 5}]""",
                     """[{"graphic": 1, "r": 2, "g": 3, "b": 4, "a": 5, "tile": -1}]""",
                 })
        {
            template.ScriptParams = bad;
            var item = CreateItem(kit, kit.PartyCostumeId);
            Assert.Equal(9, item.GraphicEquipped);
            Assert.Equal((1, 2, 3, 4), (item.GraphicR, item.GraphicG, item.GraphicB, item.GraphicA));
        }
    }

    [Fact]
    public void Wearing_requirement_met_only_when_equipped()
    {
        using var kit = new HalloweenTestKit();
        var script = kit.Compile<IQuestScript>("Quest", "HalloweenWearing.csx").Object;
        var requirement = new QuestRequirement
        {
            Id = 1,
            Type = RequirementType.Script,
            ScriptParams = """{"items": [90002, 90003]}""",
        };
        var house = kit.Fixture.AddBaseMap(48, "House", 48, 48);
        var player = kit.Fixture.CommandPlayerOn(house, 5, 5, "A");
        player.PlayerID = 505;
        house.AddPlayer(player, kit.World);

        var item = CreateItem(kit, kit.PartyCostumeId);
        Assert.True(player.Inventory.AddItem(item, 1, kit.World));

        Assert.False(script.IsMet(requirement, player, kit.World));
        Assert.Equal("Put on your costume", script.GetProgressText(requirement, player, kit.World));

        Assert.True(player.Inventory.Equip(item, kit.World));
        Assert.True(script.IsMet(requirement, player, kit.World));
        Assert.Equal("Wearing your costume", script.GetProgressText(requirement, player, kit.World));
    }

    [Fact]
    public void Doorman_reward_warps_to_entrance()
    {
        using var kit = new HalloweenTestKit();
        var script = kit.Compile<IQuestScript>("Quest", "HalloweenDoorman.csx").Object;
        var reward = new QuestReward { Id = 1, Type = RewardType.Script, ScriptParams = "" };
        var house = kit.Fixture.AddBaseMap(48, "House", 80, 80);
        var grove = kit.Fixture.AddBaseMap(38, "Grove", 48, 48);
        var player = kit.Fixture.CommandPlayerOn(grove, 5, 5, "A");
        player.PlayerID = 505;
        grove.AddPlayer(player, kit.World);
        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);

        Assert.Null(script.CanComplete(reward, player, kit.World));
        Assert.Equal(0, script.GetRequiredInventorySpace(reward, player, kit.World));

        script.GiveReward(reward, null!, player, kit.World);
        CompleteWarp(kit, player);

        Assert.Equal(house, player.Map);
        Assert.Equal((32, 64), (player.MapX, player.MapY));
        Assert.Contains(player.Sent, s => s.Contains("The door locks behind you."));
    }

    [Fact]
    public void Doorman_refuses_when_inactive()
    {
        using var kit = new HalloweenTestKit();
        var script = kit.Compile<IQuestScript>("Quest", "HalloweenDoorman.csx").Object;
        var reward = new QuestReward { Id = 1, Type = RewardType.Script, ScriptParams = "" };
        var grove = kit.Fixture.AddBaseMap(38, "Grove", 48, 48);
        var player = kit.Fixture.CommandPlayerOn(grove, 5, 5, "A");
        player.PlayerID = 505;
        grove.AddPlayer(player, kit.World);

        Assert.Equal("The door is locked tonight.", script.CanComplete(reward, player, kit.World));
    }

    [Fact]
    public void To_the_door_warps_from_town_to_outside_the_house()
    {
        using var kit = new HalloweenTestKit();
        var script = kit.Compile<IQuestScript>("Quest", "HalloweenToTheDoor.csx").Object;
        var reward = new QuestReward { Id = 1, Type = RewardType.Script, ScriptParams = "" };
        var town = kit.Fixture.AddBaseMap(1, "Minita", 80, 80);
        var grove = kit.Fixture.AddBaseMap(38, "Grove", 48, 48);
        var player = kit.Fixture.CommandPlayerOn(town, 53, 59, "A");
        player.PlayerID = 505;
        town.AddPlayer(player, kit.World);

        Assert.Equal("The house is dark tonight. Nobody's going.", script.CanComplete(reward, player, kit.World));

        kit.World.CommunityGoals.SetFlag("halloween.active", 1, null);
        Assert.Null(script.CanComplete(reward, player, kit.World));

        script.GiveReward(reward, null!, player, kit.World);
        CompleteWarp(kit, player);

        Assert.Equal(grove, player.Map);
        Assert.Equal((38, 37), (player.MapX, player.MapY));
    }

    private static void AttachCostume(HalloweenTestKit kit, int templateId)
        => kit.World.ItemHandler.GetTemplate(templateId)!.Script =
            kit.Compile<IItemScript>("Item", "HalloweenCostume.csx");

    [Fact]
    public void Turn_in_accepts_any_listed_item_and_takes_only_what_it_needs()
    {
        using var kit = new HalloweenTestKit();
        var script = kit.Compile<IQuestScript>("Quest", "HalloweenTurnIn.csx").Object;
        var requirement = new QuestRequirement
        {
            Id = 1,
            Type = RequirementType.Script,
            ScriptParams = $$"""{"items": [{{kit.MaskIds[0]}}, {{kit.MaskIds[1]}}], "count": 2, "label": "Any mask"}""",
        };
        var house = kit.Fixture.AddBaseMap(48, "House", 48, 48);
        var player = kit.Fixture.CommandPlayerOn(house, 5, 5, "A");

        Assert.True(player.Inventory.AddItem(CreateItem(kit, kit.MaskIds[1]), 1, kit.World));
        Assert.False(script.IsMet(requirement, player, kit.World));
        Assert.Equal("Any mask (1/2)", script.GetProgressText(requirement, player, kit.World));

        Assert.True(player.Inventory.AddItem(CreateItem(kit, kit.MaskIds[0]), 1, kit.World));
        Assert.True(player.Inventory.AddItem(CreateItem(kit, kit.MaskIds[1]), 1, kit.World));
        Assert.True(player.Inventory.AddItem(CreateItem(kit, kit.MaskIds[2]), 1, kit.World));
        Assert.True(script.IsMet(requirement, player, kit.World));
        Assert.Equal("Any mask (2/2)", script.GetProgressText(requirement, player, kit.World));

        script.OnTakeRequirement(requirement, player, kit.World);

        Assert.Equal(0, player.Inventory.GetItemCount(kit.MaskIds[0]));
        Assert.Equal(1, player.Inventory.GetItemCount(kit.MaskIds[1]));
        Assert.Equal(1, player.Inventory.GetItemCount(kit.MaskIds[2]));
    }

    [Fact]
    public void Turn_in_with_bad_params_is_never_met()
    {
        using var kit = new HalloweenTestKit();
        var script = kit.Compile<IQuestScript>("Quest", "HalloweenTurnIn.csx").Object;
        var house = kit.Fixture.AddBaseMap(48, "House", 48, 48);
        var player = kit.Fixture.CommandPlayerOn(house, 5, 5, "A");
        Assert.True(player.Inventory.AddItem(CreateItem(kit, kit.MaskIds[0]), 1, kit.World));

        foreach (var bad in new[] { "", "not json", """{"items": []}""", $$"""{"items": [{{kit.MaskIds[0]}}], "count": 0}""" })
        {
            var requirement = new QuestRequirement { Id = 1, Type = RequirementType.Script, ScriptParams = bad };
            Assert.False(script.IsMet(requirement, player, kit.World));
        }
    }

    private static Item CreateItem(HalloweenTestKit kit, int templateId)
    {
        var item = new Item();
        Assert.True(item.LoadFromTemplate(kit.World.ItemHandler.GetTemplate(templateId)!));
        kit.World.ItemHandler.AddAndAssignId(item, kit.World);
        return item;
    }

    private static void CompleteWarp(HalloweenTestKit kit, Player player)
    {
        var ev = new DoneLoadingMapEvent { Player = player, Ticks = kit.World.TimeNow };
        kit.World.EventHandler.AddEvent(ev);
        kit.World.EventHandler.Update(kit.World);
    }

    private static void SetWorldRandom(GameWorld world, Random rng)
        => typeof(GameWorld).GetField("rng", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(world, rng);
}
