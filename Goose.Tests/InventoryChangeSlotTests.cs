using Goose;
using Goose.Events;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
// Regression tests for code-review-2026-08-15 finding C1: CHANGE n,n (id1 == id2)
// passed the same ItemSlot to both swap parameters and doubled the stack in place.
public class InventoryChangeSlotTests
{
    private static ItemTemplate Piles(int id, string name, int stackSize) =>
        new ItemTemplate
        {
            ID = id, Name = name, Description = name, Value = 10,
            BaseStats = new AttributeSet(), StackSize = stackSize, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };

    private static ItemSlot PutInSlot(VendorFixture fixture, ItemTemplate template, int slotId, long stack)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        var slot = new ItemSlot { Item = item, Stack = stack };
        fixture.Player.Inventory.SetSlot(slotId, slot);
        return slot;
    }

    [Fact]
    public void InventorySwapSameSlot_LeavesStackableItemUnchanged()
    {
        using var fixture = new VendorFixture();
        // 2*4 <= 10, so the pre-fix CanStack(self, self) check passed and doubled.
        PutInSlot(fixture, Piles(1, "Pile", stackSize: 10), 1, 4);

        fixture.Player.Inventory.SwapSlots(1, 1, fixture.World);

        var slot = fixture.Player.Inventory.GetSlot(1);
        Assert.NotNull(slot);
        Assert.Equal(4, slot.Stack);

        for (int i = 2; i <= GameWorld.Settings.InventorySize; i++)
        {
            Assert.Null(fixture.Player.Inventory.GetSlot(i));
        }
    }

    [Fact]
    public void ItemSlotSwapSameRef_DoesNotDoubleStack()
    {
        using var fixture = new VendorFixture();
        var slot = PutInSlot(fixture, Piles(1, "Pile", stackSize: 10), 1, 4);

        ItemSlot.SwapSlots(ref slot, ref slot);

        Assert.Equal(4, slot.Stack);
    }

    [Fact]
    public void InventorySwapDifferentSlots_SameTemplateStillMerges()
    {
        using var fixture = new VendorFixture();
        PutInSlot(fixture, Piles(1, "Pile", stackSize: 10), 1, 4);
        PutInSlot(fixture, Piles(1, "Pile", stackSize: 10), 2, 2);

        fixture.Player.Inventory.SwapSlots(1, 2, fixture.World);

        Assert.Null(fixture.Player.Inventory.GetSlot(1));
        var merged = fixture.Player.Inventory.GetSlot(2);
        Assert.NotNull(merged);
        Assert.Equal(6, merged.Stack);
    }

    [Fact]
    public void InventorySwapDifferentSlots_DifferentTemplatesStillSwap()
    {
        using var fixture = new VendorFixture();
        ItemSlot a = PutInSlot(fixture, Piles(1, "Pile", stackSize: 10), 1, 4);
        ItemSlot b = PutInSlot(fixture, Piles(2, "Other", stackSize: 10), 2, 3);

        fixture.Player.Inventory.SwapSlots(1, 2, fixture.World);

        Assert.Same(b.Item, fixture.Player.Inventory.GetSlot(1)?.Item);
        Assert.Same(a.Item, fixture.Player.Inventory.GetSlot(2)?.Item);
    }

    [Fact]
    public void ChangePacket_SameSlotTwice_DoesNotDuplicate()
    {
        using var fixture = new VendorFixture();
        PutInSlot(fixture, Piles(1, "Pile", stackSize: 10), 1, 4);
        Assert.Equal(Goose.Player.States.Ready, fixture.Player.State);

        var ev = new InventoryChangeSlotEvent
        {
            Player = fixture.Player,
            Data = "CHANGE1,1",
        };
        ev.Ready(fixture.World);

        var slot = fixture.Player.Inventory.GetSlot(1);
        Assert.NotNull(slot);
        Assert.Equal(4, slot.Stack);
        Assert.Empty(fixture.Player.Sent);
    }
}
