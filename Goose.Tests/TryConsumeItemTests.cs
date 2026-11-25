using Goose.Testing;

namespace Goose.Tests;

public class TryConsumeItemTests
{
    private const int TemplateId = 500;
    private const int OtherTemplateId = 501;

    private static Item NewItem(TestWorldFixture fixture, int templateId)
    {
        var template = fixture.AddBaseItemTemplate(templateId, "Item " + templateId,
            ItemTemplate.UseTypes.OneTime, t => t.StackSize = 10);
        var item = new Item();
        item.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        return item;
    }

    private static (TestWorldFixture Fixture, Player Player) Setup()
    {
        var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var player = fixture.CommandPlayerOn(map, 5, 5);
        map.AddPlayer(player, fixture.World);
        return (fixture, player);
    }

    [Fact]
    public void ExactStack_PartialConsume_LeavesRemainder()
    {
        var (fixture, player) = Setup();
        var item = NewItem(fixture, TemplateId);
        player.Inventory.AddItem(item, 3, fixture.World);

        bool consumed = player.Inventory.TryConsumeItem(item, 1, fixture.World);

        Assert.True(consumed);
        var slot = player.Inventory.GetSlot(1);
        Assert.NotNull(slot);
        Assert.Equal(2, slot!.Stack);
    }

    [Fact]
    public void ExactItemGone_SameTemplateInLaterSlot_FallbackConsumesIt()
    {
        var (fixture, player) = Setup();

        var unrelated = NewItem(fixture, OtherTemplateId);
        player.Inventory.AddItem(unrelated, 5, fixture.World);

        var exact = NewItem(fixture, TemplateId);
        player.Inventory.AddItem(exact, 1, fixture.World);

        player.Inventory.RemoveItem(exact, 1, fixture.World);

        var fallback = NewItem(fixture, TemplateId);
        player.Inventory.AddItem(fallback, 4, fixture.World);

        Assert.Equal(5, player.Inventory.GetSlot(1)!.Stack);
        Assert.Equal(4, player.Inventory.GetSlot(2)!.Stack);

        bool consumed = player.Inventory.TryConsumeItem(exact, 1, fixture.World);

        Assert.True(consumed);
        Assert.Equal(5, player.Inventory.GetSlot(1)!.Stack);
        Assert.Equal(3, player.Inventory.GetSlot(2)!.Stack);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveCount_ReturnsFalse_InventoryUnchanged(long count)
    {
        var (fixture, player) = Setup();
        var item = NewItem(fixture, TemplateId);
        player.Inventory.AddItem(item, 3, fixture.World);

        bool consumed = player.Inventory.TryConsumeItem(item, count, fixture.World);

        Assert.False(consumed);
        var slot = player.Inventory.GetSlot(1);
        Assert.NotNull(slot);
        Assert.Equal(3, slot!.Stack);
        Assert.Same(item, slot.Item);
        for (int i = 2; i <= 30; i++)
            Assert.Null(player.Inventory.GetSlot(i));
    }

    [Fact]
    public void NoStacks_ReturnsFalse_InventoryUnchanged()
    {
        var (fixture, player) = Setup();
        var item = NewItem(fixture, TemplateId);

        bool consumed = player.Inventory.TryConsumeItem(item, 1, fixture.World);

        Assert.False(consumed);
        for (int i = 1; i <= 30; i++)
            Assert.Null(player.Inventory.GetSlot(i));
    }
}
