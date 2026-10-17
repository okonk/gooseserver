using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class InventoryAddItemTests
{
    private static (TestWorldFixture World, TestWorldFixture.CapturingPlayer Player) Setup()
    {
        var world = new TestWorldFixture();
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

    private static ItemTemplate RegisterTemplate(GameWorld world, int id, int stackSize)
    {
        var template = new ItemTemplate
        {
            ID = id, Name = "Item" + id, Description = "Item" + id,
            UseType = ItemTemplate.UseTypes.NoUse,
            StackSize = stackSize,
            BaseStats = new AttributeSet(),
            ScriptParams = "",
        };
        world.ItemHandler.AddTemplate(template);
        return template;
    }

    private static Item MakeItem(GameWorld world, ItemTemplate template)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        world.ItemHandler.AddAndAssignId(item, world);
        return item;
    }

    [Fact]
    public void AddItem_PrefersExistingStackOverEarlierFreeSlot()
    {
        var (fixture, player) = Setup();
        using var _ = fixture;
        var world = fixture.World;
        var template = RegisterTemplate(world, 100, 10);

        player.Inventory.SetSlot(3, new ItemSlot { Item = MakeItem(world, template), Stack = 2 });

        Assert.True(player.Inventory.AddItem(MakeItem(world, template), 3, world));

        Assert.Null(player.Inventory.GetSlot(1));
        Assert.Equal(5, player.Inventory.GetSlot(3)!.Stack);
    }

    [Fact]
    public void AddItem_UsesFirstFreeSlotWhenNoStackFits()
    {
        var (fixture, player) = Setup();
        using var _ = fixture;
        var world = fixture.World;
        var template = RegisterTemplate(world, 100, 10);

        player.Inventory.SetSlot(3, new ItemSlot { Item = MakeItem(world, template), Stack = 9 });

        Assert.True(player.Inventory.AddItem(MakeItem(world, template), 3, world));

        Assert.Equal(3, player.Inventory.GetSlot(1)!.Stack);
        Assert.Equal(9, player.Inventory.GetSlot(3)!.Stack);
    }
}
