using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class ItemFamilyInventoryTests
{
    private const int BaseId = 100;
    private const int D1Id = 1001;
    private const int D2Id = 1002;
    private const int UnrelatedId = 200;

    private static (TestWorldFixture World, TestWorldFixture.CapturingPlayer Player) Setup()
    {
        var world = new TestWorldFixture();
        RegisterFamily(world);
        var player = new TestWorldFixture.CapturingPlayer
        {
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet(),
            Class = world.World.ClassHandler.GetClass(0)!,
            State = Player.States.Ready,
        };
        player.Inventory = new Inventory(player, world.Settings);
        return (world, player);
    }

    private static void RegisterFamily(TestWorldFixture world)
    {
        RegisterTemplate(world.World, BaseId, "Herb");
        RegisterTemplate(world.World, D1Id, "Herb I", variantOf: BaseId, dimension: 1);
        RegisterTemplate(world.World, D2Id, "Herb II", variantOf: BaseId, dimension: 2);
        RegisterTemplate(world.World, UnrelatedId, "Stone");
    }

    private static ItemTemplate RegisterTemplate(GameWorld world, int id, string name, int? variantOf = null, int dimension = 0)
    {
        var template = new ItemTemplate
        {
            ID = id, Name = name, Description = name,
            UseType = ItemTemplate.UseTypes.NoUse,
            StackSize = 10,
            BaseStats = new AttributeSet(),
            ScriptParams = "",
            VariantOfTemplateID = variantOf,
            Dimension = dimension,
        };
        world.ItemHandler.AddTemplate(template);
        return template;
    }

    private static ItemSlot Give(Player player, GameWorld world, ItemTemplate template, int stack)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        world.ItemHandler.AddAndAssignId(item, world);
        Assert.True(player.Inventory.AddItem(item, stack, world));
        return new ItemSlot { Item = item, Stack = stack };
    }

    private static ItemSlot GiveCustom(Player player, GameWorld world, ItemTemplate template, int stack)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        item.Description = "Custom created by GM";
        world.ItemHandler.AddAndAssignId(item, world);
        Assert.True(player.Inventory.AddItem(item, stack, world));
        return new ItemSlot { Item = item, Stack = stack };
    }

    private static ItemTemplate T(TestWorldFixture world, int id) => world.World.ItemHandler.GetTemplate(id)!;

    [Fact]
    public void HasItemFamily_FindsBaseAndVariantsInInventory()
    {
        var (world, player) = Setup();

        Assert.False(player.Inventory.HasItemFamily(BaseId, 0));
        Give(player, world.World, T(world, BaseId), 1);
        Assert.True(player.Inventory.HasItemFamily(BaseId, 0));

        var (world2, player2) = Setup();
        Give(player2, world2.World, T(world2, D1Id), 1);
        Assert.True(player2.Inventory.HasItemFamily(BaseId, 0));
        Assert.False(player2.Inventory.HasItemFamily(UnrelatedId, 0));
    }

    [Fact]
    public void HasItemFamily_FindsVariantsInEquippedSlots()
    {
        var (world, player) = Setup();
        var slot = Give(player, world.World, T(world, D1Id), 1);
        player.Inventory.GetInventorySlots()[1] = null;
        player.Inventory.GetEquippedSlots()[1] = slot;

        Assert.True(player.Inventory.HasItemFamily(BaseId, 0));
        Assert.False(player.Inventory.HasItemFamily(UnrelatedId, 0));
    }

    [Fact]
    public void HasItemFamily_FindsVariantsInCombineBag()
    {
        var (world, player) = Setup();
        var slot = Give(player, world.World, T(world, D2Id), 1);
        player.Inventory.GetInventorySlots()[1] = null;
        player.Inventory.GetCombineBagContainer().SetSlot(1, slot);

        Assert.True(player.Inventory.HasItemFamily(BaseId, 0));
        Assert.False(player.Inventory.HasItemFamily(UnrelatedId, 0));
    }

    [Fact]
    public void HasItemFamily_RespectsMinimumDimension()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, D1Id), 1);

        Assert.True(player.Inventory.HasItemFamily(BaseId, 0));
        Assert.True(player.Inventory.HasItemFamily(BaseId, 1));
        Assert.False(player.Inventory.HasItemFamily(BaseId, 2));

        Give(player, world.World, T(world, D2Id), 1);
        Assert.True(player.Inventory.HasItemFamily(BaseId, 2));
    }

    [Fact]
    public void HasItemFamily_AllowsCustomItems()
    {
        var (world, player) = Setup();
        GiveCustom(player, world.World, T(world, BaseId), 1);

        Assert.True(player.Inventory.HasItemFamily(BaseId, 0));
    }

    [Fact]
    public void GetInventoryItemFamilyCount_SumsAllDimensionStacks()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, BaseId), 3);
        Give(player, world.World, T(world, D1Id), 5);
        Give(player, world.World, T(world, D2Id), 2);

        Assert.Equal(10, player.Inventory.GetInventoryItemFamilyCount(BaseId));
        Assert.Equal(0, player.Inventory.GetInventoryItemFamilyCount(UnrelatedId));
    }

    [Fact]
    public void GetInventoryItemFamilyCount_ExcludesCustomItems()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, BaseId), 3);
        GiveCustom(player, world.World, T(world, BaseId), 4);

        Assert.Equal(3, player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }

    [Fact]
    public void GetInventoryItemFamilyCount_ExcludesEquippedAndCombineContainers()
    {
        var (world, player) = Setup();
        var equipped = Give(player, world.World, T(world, D1Id), 5);
        player.Inventory.GetInventorySlots()[1] = null;
        player.Inventory.GetEquippedSlots()[1] = equipped;

        var combine = Give(player, world.World, T(world, D2Id), 2);
        player.Inventory.GetInventorySlots()[1] = null;
        player.Inventory.GetCombineBagContainer().SetSlot(1, combine);

        Assert.Equal(0, player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }

    [Fact]
    public void TryRemoveInventoryItemFamily_RemovesTheExactRequestedQuantity()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, BaseId), 3);
        Give(player, world.World, T(world, D1Id), 5);

        Assert.True(player.Inventory.TryRemoveInventoryItemFamily(BaseId, 8, world.World));

        Assert.Null(player.Inventory.GetInventorySlots()[1]);
        Assert.Null(player.Inventory.GetInventorySlots()[2]);
        Assert.Equal(0, player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }

    [Fact]
    public void TryRemoveInventoryItemFamily_ConsumesLowerDimensionsFirst()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, BaseId), 3);
        Give(player, world.World, T(world, D1Id), 5);

        Assert.True(player.Inventory.TryRemoveInventoryItemFamily(BaseId, 4, world.World));

        Assert.Null(player.Inventory.GetInventorySlots()[1]);
        var remaining = player.Inventory.GetInventorySlots()[2]!;
        Assert.Equal(D1Id, remaining.Item.TemplateID);
        Assert.Equal(4, remaining.Stack);
    }

    [Fact]
    public void TryRemoveInventoryItemFamily_LeavesHigherDimensionUntouchedWhenLowerDimensionsSuffice()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, D2Id), 5);
        Give(player, world.World, T(world, BaseId), 3);

        Assert.True(player.Inventory.TryRemoveInventoryItemFamily(BaseId, 3, world.World));

        var high = player.Inventory.GetInventorySlots()[1]!;
        Assert.Equal(D2Id, high.Item.TemplateID);
        Assert.Equal(5, high.Stack);
        Assert.Null(player.Inventory.GetInventorySlots()[2]);
    }

    [Fact]
    public void TryRemoveInventoryItemFamily_ReturnsFalseAndMutatesNothingWhenShort()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, BaseId), 3);
        Give(player, world.World, T(world, D1Id), 5);

        Assert.False(player.Inventory.TryRemoveInventoryItemFamily(BaseId, 9, world.World));

        Assert.Equal(3, player.Inventory.GetInventorySlots()[1]!.Stack);
        Assert.Equal(5, player.Inventory.GetInventorySlots()[2]!.Stack);
        Assert.Equal(8, player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }

    [Fact]
    public void TryRemoveInventoryItemFamily_ExcludesCustomItems()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, BaseId), 3);
        GiveCustom(player, world.World, T(world, BaseId), 4);

        Assert.True(player.Inventory.TryRemoveInventoryItemFamily(BaseId, 3, world.World));

        var custom = player.Inventory.GetInventorySlots()[2]!;
        Assert.True(custom.Item.Custom);
        Assert.Equal(4, custom.Stack);
    }

    [Fact]
    public void TryRemoveInventoryItemFamily_NonPositiveQuantitySucceedsWithoutMutating()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, BaseId), 3);
        Give(player, world.World, T(world, D1Id), 5);

        Assert.True(player.Inventory.TryRemoveInventoryItemFamily(BaseId, 0, world.World));
        Assert.True(player.Inventory.TryRemoveInventoryItemFamily(BaseId, -2, world.World));

        Assert.Equal(3, player.Inventory.GetInventorySlots()[1]!.Stack);
        Assert.Equal(5, player.Inventory.GetInventorySlots()[2]!.Stack);
        Assert.Equal(8, player.Inventory.GetInventoryItemFamilyCount(BaseId));
    }

    [Fact]
    public void TryRemoveInventoryItemFamily_SendsEveryChangedSlot()
    {
        var (world, player) = Setup();
        Give(player, world.World, T(world, BaseId), 3);
        Give(player, world.World, T(world, D1Id), 5);
        player.Sent.Clear();

        Assert.True(player.Inventory.TryRemoveInventoryItemFamily(BaseId, 4, world.World));

        Assert.Contains(player.Sent, s => s.StartsWith("CIS1"));
        Assert.Contains(player.Sent, s => s.StartsWith("SIS"));
    }
}
