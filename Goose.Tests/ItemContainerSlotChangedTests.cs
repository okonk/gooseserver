using Goose;

namespace Goose.Tests;

public class ItemContainerSlotChangedTests
{
    private static ItemSlot MakeSlot()
    {
        return new ItemSlot { Item = new Item() };
    }

    [Fact]
    public void SetSlot_FiresWithOldAndNew()
    {
        var container = new ItemContainer(3);
        var oldSlot = MakeSlot();
        var newSlot = MakeSlot();
        container.SetSlot(1, oldSlot);

        int? index = null;
        ItemSlot? firedOld = null;
        ItemSlot? firedNew = null;
        container.SlotChanged += (i, o, n) =>
        {
            index = i;
            firedOld = o;
            firedNew = n;
        };

        container.SetSlot(1, newSlot);

        Assert.Equal(1, index);
        Assert.Same(oldSlot, firedOld);
        Assert.Same(newSlot, firedNew);
    }

    [Fact]
    public void SetSlot_SameReference_DoesNotFire()
    {
        var container = new ItemContainer(3);
        var slot = MakeSlot();
        container.SetSlot(1, slot);

        int firedCount = 0;
        container.SlotChanged += (i, o, n) => firedCount++;

        container.SetSlot(1, slot);

        Assert.Equal(0, firedCount);
    }

    [Fact]
    public void SetSlot_OutOfRange_DoesNotFire()
    {
        var container = new ItemContainer(3);

        int firedCount = 0;
        container.SlotChanged += (i, o, n) => firedCount++;

        container.SetSlot(-1, MakeSlot());
        container.SetSlot(3, MakeSlot());

        Assert.Equal(0, firedCount);
    }

    [Fact]
    public void NotifySlotChanged_FiresWithSameSlotBothSides()
    {
        var container = new ItemContainer(3);
        var slot = MakeSlot();
        container.SetSlot(2, slot);

        int? index = null;
        ItemSlot? firedOld = null;
        ItemSlot? firedNew = null;
        container.SlotChanged += (i, o, n) =>
        {
            index = i;
            firedOld = o;
            firedNew = n;
        };

        container.NotifySlotChanged(2);

        Assert.Equal(2, index);
        Assert.Same(slot, firedOld);
        Assert.Same(slot, firedNew);
    }
}
