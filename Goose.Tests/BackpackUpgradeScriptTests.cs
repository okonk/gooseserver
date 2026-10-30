using Goose;
using Goose.Quests;
using Goose.Scripting;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

public class BackpackUpgradeScriptTests
{
    private const string Misconfigured =
        "This quest's reward is misconfigured.\\nPlease report this to a GM.";

    private static readonly Lazy<IQuestScript> ShippedScript = new(CompileShippedScript);

    private static IQuestScript CompileShippedScript()
    {
        using var scratch = new QuestScriptFixture();
        return scratch.Compile(
            File.ReadAllText(Path.Combine(IllutiaData.DataDirectory(), "Scripts", "Quest", "BackpackUpgrade.csx")),
            "BackpackUpgrade.csx").Object;
    }

    private sealed class CapturingPlayer : Player
    {
        public CapturingPlayer() : base(0) { }
        public List<string> Sent { get; } = new();
        public override bool Send(string data) { this.Sent.Add(data); return true; }
    }

    private static ItemTemplate BackpackTemplate(int id, int slots) =>
        new ItemTemplate
        {
            ID = id, Name = "Backpack", Description = "A backpack", Value = 10,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.Backpack, UseType = ItemTemplate.UseTypes.Armor,
            Properties = new PropertiesDictionary { ["backpackSlots"] = slots },
        };

    private static Item MakeBackpack(GameWorld world, int id, int slots)
    {
        world.ItemHandler.AddTemplate(BackpackTemplate(id, slots));
        var item = new Item();
        item.LoadFromTemplate(world.ItemHandler.GetTemplate(id)!);
        world.ItemHandler.AddAndAssignId(item, world);
        return item;
    }

    private static CapturingPlayer PlayerWithBackpack(QuestScriptFixture fixture, int slots)
    {
        var player = new CapturingPlayer
        {
            Map = new Map { ID = 1 },
            Class = new Class { ClassName = "Default" },
            MaxStats = new AttributeSet(),
        };
        player.Inventory = new Inventory(player, fixture.Settings);

        var backpack = MakeBackpack(fixture.World, 100, slots);
        Assert.True(player.Inventory.AddItem(backpack, 1, fixture.World));
        Assert.True(player.Inventory.Equip(backpack, fixture.World));
        return player;
    }

    private static QuestReward Reward(string scriptParams) =>
        new QuestReward { Id = 1, Type = RewardType.Script, ScriptParams = scriptParams };

    [Fact]
    public void Upgrade_from_10_to_20_keeps_items_and_grows_container()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        var player = PlayerWithBackpack(fixture, 10);
        var backpack = player.Inventory.EquippedBackpack!;

        fixture.World.ItemHandler.AddTemplate(new ItemTemplate
        {
            ID = 200, Name = "Sword", Description = "A sword", Value = 10,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        });
        var sword = new Item();
        sword.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(200)!);
        fixture.World.ItemHandler.AddAndAssignId(sword, fixture.World);
        player.Inventory.GetBackpackContainer().SetSlot(7, new ItemSlot { Item = sword, Stack = 1 });

        var reward = Reward("""{"from": 10, "to": 20, "columns": 5}""");
        Assert.Null(script.CanComplete(reward, player, fixture.World));
        script.GiveReward(reward, null!, player, fixture.World);

        Assert.Same(sword, player.Inventory.GetBackpackContainer().GetSlot(7)?.Item);
        Assert.Equal(21, player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.Equal(20, backpack.BackpackSlots);
        Assert.EndsWith("Holds 20 items.", backpack.Description);
        Assert.Contains(player.Sent, s => s.Contains("[Quest Reward]: Backpack upgraded to 20 slots."));
    }

    [Fact]
    public void Upgrade_for_20_slot_backpack_is_refused_on_a_10_slot_one()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        var player = PlayerWithBackpack(fixture, 10);
        var backpack = player.Inventory.EquippedBackpack!;
        var reward = Reward("""{"from": 20, "to": 40}""");

        Assert.Equal("This upgrade is for a 20-slot backpack.",
            script.CanComplete(reward, player, fixture.World));

        script.GiveReward(reward, null!, player, fixture.World);

        Assert.Equal(10, backpack.BackpackSlots);
        Assert.Equal(11, player.Inventory.GetBackpackContainer().MaxSlots);
    }

    [Fact]
    public void Running_the_same_tier_twice_is_refused_the_second_time()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        var player = PlayerWithBackpack(fixture, 10);
        var backpack = player.Inventory.EquippedBackpack!;
        var reward = Reward("""{"from": 10, "to": 20}""");

        Assert.Null(script.CanComplete(reward, player, fixture.World));
        script.GiveReward(reward, null!, player, fixture.World);
        Assert.Equal(20, backpack.BackpackSlots);

        Assert.Equal("This upgrade is for a 10-slot backpack.",
            script.CanComplete(reward, player, fixture.World));
        script.GiveReward(reward, null!, player, fixture.World);

        Assert.Equal(20, backpack.BackpackSlots);
        Assert.Equal(21, player.Inventory.GetBackpackContainer().MaxSlots);
    }

    [Fact]
    public void Upgrade_without_a_backpack_is_refused()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        var player = new Player(0);
        player.Inventory = new Inventory(player, fixture.Settings);
        var reward = Reward("""{"from": 10, "to": 20}""");

        Assert.Equal("You need to be wearing your backpack.",
            script.CanComplete(reward, player, fixture.World));

        script.GiveReward(reward, null!, player, fixture.World);

        Assert.Null(player.Inventory.EquippedBackpack);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("""{"from": 20, "to": 10}""")]
    [InlineData("""{"from": 0, "to": 20}""")]
    [InlineData("""{"from": "ten", "to": 20}""")]
    [InlineData("""{"from": 10, "to": 20, "bogus": 1}""")]
    public void Bad_params_or_to_not_greater_than_from_is_misconfigured(string scriptParams)
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        var player = PlayerWithBackpack(fixture, 10);
        var backpack = player.Inventory.EquippedBackpack!;
        var reward = Reward(scriptParams);

        Assert.Equal(Misconfigured, script.CanComplete(reward, player, fixture.World));
        Assert.Equal(0, script.GetRequiredInventorySpace(reward, player, fixture.World));

        script.GiveReward(reward, null!, player, fixture.World);

        Assert.Equal(10, backpack.BackpackSlots);
        Assert.Equal(11, player.Inventory.GetBackpackContainer().MaxSlots);
    }

    [Fact]
    public void Upgrade_to_501_slots_is_misconfigured()
    {
        using var fixture = new QuestScriptFixture();
        var script = ShippedScript.Value;
        var player = PlayerWithBackpack(fixture, 10);
        var backpack = player.Inventory.EquippedBackpack!;
        var reward = Reward("""{"from": 10, "to": 501}""");

        Assert.Equal(Misconfigured, script.CanComplete(reward, player, fixture.World));

        script.GiveReward(reward, null!, player, fixture.World);

        Assert.Equal(10, backpack.BackpackSlots);
        Assert.Equal(11, player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.DoesNotContain(player.Sent, s => s.Contains("[Quest Reward]"));
    }

    [Fact]
    public void Upgrade_resends_MKW_for_an_open_backpack_window()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture.World, 100, 10);
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(backpack, fixture.World));
        var win = fixture.Player.Windows.OfType<BackpackWindow>().Single();

        fixture.Player.Sent.Clear();
        Assert.True(fixture.Player.Inventory.UpgradeBackpack(20, 5, fixture.World));

        var mkw = fixture.Player.Sent.Where(m => m.StartsWith($"MKW{win.ID},")).Last();
        Assert.EndsWith(",20,5", mkw);
        Assert.Equal(21, fixture.Player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.Equal(20, fixture.Player.Inventory.EquippedBackpack!.BackpackSlots);
    }

    [Fact]
    public void UpgradeBackpack_to_501_slots_returns_false_and_changes_nothing()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture.World, 100, 10);
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(backpack, fixture.World));

        fixture.Player.Sent.Clear();
        Assert.False(fixture.Player.Inventory.UpgradeBackpack(501, 5, fixture.World));

        Assert.Equal(10, fixture.Player.Inventory.EquippedBackpack!.BackpackSlots);
        Assert.Equal(11, fixture.Player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.DoesNotContain(fixture.Player.Sent, m => m.StartsWith("MKW"));
    }

    [Fact]
    public void UpgradeBackpack_to_fewer_slots_returns_false_and_changes_nothing()
    {
        using var fixture = new VendorFixture();
        var backpack = MakeBackpack(fixture.World, 100, 10);
        Assert.True(fixture.Player.Inventory.AddItem(backpack, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(backpack, fixture.World));

        fixture.Player.Sent.Clear();
        Assert.False(fixture.Player.Inventory.UpgradeBackpack(5, 5, fixture.World));

        Assert.Equal(10, fixture.Player.Inventory.EquippedBackpack!.BackpackSlots);
        Assert.Equal(11, fixture.Player.Inventory.GetBackpackContainer().MaxSlots);
        Assert.DoesNotContain(fixture.Player.Sent, m => m.StartsWith("MKW"));
    }
}
