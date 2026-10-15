using Goose;

namespace Goose.Tests;

public class ItemContainerGrowTests
{
    private static ItemSlot MakeSlot()
    {
        return new ItemSlot { Item = new Item() };
    }

    [Fact]
    public void GrowToPages_GrowsToPagesTimesSlotsPerPagePlusOne()
    {
        var container = new ItemContainer(5);

        bool grew = container.GrowToPages(3, 10);

        Assert.True(grew);
        Assert.Equal(31, container.MaxSlots);
    }

    [Fact]
    public void GrowToPages_SmallerRequest_IsNoOpAndReturnsFalse()
    {
        var container = new ItemContainer(20);

        bool grew = container.GrowToPages(1, 10);

        Assert.False(grew);
        Assert.Equal(20, container.MaxSlots);
    }

    [Fact]
    public void GrowToPages_ExactSizeRequest_ReturnsFalse()
    {
        var container = new ItemContainer(21);

        bool grew = container.GrowToPages(2, 10);

        Assert.False(grew);
        Assert.Equal(21, container.MaxSlots);
    }

    [Fact]
    public void GrowToPages_NeverShrinks_AndItemsInHighSlotsSurvive()
    {
        var container = new ItemContainer(25);
        var highSlot = MakeSlot();
        container.SetSlot(24, highSlot);

        container.GrowToPages(5, 10);
        bool shrank = container.GrowToPages(1, 10);

        Assert.False(shrank);
        Assert.Equal(51, container.MaxSlots);
        Assert.Same(highSlot, container.GetSlot(24));
    }

    [Fact]
    public void GrowToPages_PreservesIdentity_ExistingSlotChangedSubscriberStillFires()
    {
        var container = new ItemContainer(10);
        var holders = new Dictionary<int, ItemContainer> { [0] = container };

        int firedCount = 0;
        container.SlotChanged += (i, o, n) => firedCount++;

        container.GrowToPages(2, 10);

        var held = holders[0];
        Assert.Same(container, held);
        Assert.Equal(21, held.MaxSlots);

        var newSlot = MakeSlot();
        held.SetSlot(15, newSlot);

        Assert.Equal(1, firedCount);
        Assert.Same(newSlot, container.GetSlot(15));
    }
}
