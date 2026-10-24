using Goose;
using Goose.Testing;

namespace Goose.Tests;

public class InventoryBindOnPickupTests
{
    private const int BopItem = 101;

    private static (TestWorldFixture Fixture, TestWorldFixture.CapturingPlayer Player) Setup()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town");
        fixture.AddBaseItemTemplate(BopItem, "Bop", ItemTemplate.UseTypes.NoUse, t =>
        {
            t.IsBindOnPickup = true;
            t.StackSize = 100;
        });
        return (fixture, fixture.CommandPlayerOn(map, 5, 5, "A"));
    }

    private static Item LoadItem(TestWorldFixture fixture)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(BopItem)!);
        return item;
    }

    [Fact]
    public void AddItem_BindsBindOnPickup()
    {
        var (fixture, player) = Setup();

        Assert.True(player.Inventory.AddItem(LoadItem(fixture), 5, fixture.World));

        Assert.True(player.Inventory.GetSlot(1)!.Item.IsBound);
    }

    [Fact]
    public void AddItem_BindsExistingUnboundStack()
    {
        var (fixture, player) = Setup();
        player.Inventory.SetSlot(1, new ItemSlot { Item = LoadItem(fixture), Stack = 5 });

        Assert.True(player.Inventory.AddItem(LoadItem(fixture), 5, fixture.World));

        var slot = player.Inventory.GetSlot(1)!;
        Assert.Equal(10, slot.Stack);
        Assert.True(slot.Item.IsBound);
    }
}
