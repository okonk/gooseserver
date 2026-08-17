using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>Regression tests for code-review-2026-08-15 finding C1: a CHANGE packet with
/// id1 == id2 made Inventory.SwapSlots pass the same ItemSlot as both parameters, and
/// ItemSlot.SwapSlots' merge branch doubled the stack in place (from.Item == to.Item,
/// to.CanStack(from) passes whenever 2*Stack <= StackSize) — a player-reachable
/// infinite-gold exploit on the Open CHANGE packet.</summary>
public class InventoryChangeSlotTests
{
    /// <summary>A stackable template (StackSize >= 2), the shape every exploit item had.</summary>
    private static ItemTemplate Piles(int id, string name, int stackSize) =>
        new ItemTemplate
        {
            ID = id, Name = name, Description = name, Value = 10,
            BaseStats = new AttributeSet(), StackSize = stackSize, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };

    /// <summary>Builds an item + slot the way the vendor fixture does, and pins it to a
    /// known slot so the tests can name slot ids in CHANGE packets.</summary>
    private static ItemSlot PutInSlot(VendorFixture fixture, ItemTemplate template, int slotId, long stack)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        var slot = new ItemSlot { Item = item, Stack = stack };
        fixture.Player.Inventory.SetSlot(slotId, slot);
        return slot;
    }

    /// <summary>Primary: CHANGE n,n must be a no-op — same stack, same slot, nothing
    /// duplicated, nothing lost.</summary>
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

        // Nothing was duplicated into any other slot.
        for (int i = 2; i <= GameWorld.Settings.InventorySize; i++)
        {
            Assert.Null(fixture.Player.Inventory.GetSlot(i));
        }
    }

    /// <summary>Defense in depth: ItemSlot.SwapSlots called with the same slot object as
    /// both refs must not double the stack, even if a caller bypasses Inventory.</summary>
    [Fact]
    public void ItemSlotSwapSameRef_DoesNotDoubleStack()
    {
        using var fixture = new VendorFixture();
        var slot = PutInSlot(fixture, Piles(1, "Pile", stackSize: 10), 1, 4);

        ItemSlot.SwapSlots(ref slot, ref slot);

        Assert.Equal(4, slot.Stack);
    }

    /// <summary>Adversarial: a genuine swap between two DIFFERENT slots of same-template
    /// stackables must merge exactly as before the fix (to absorbs from, from is cleared).</summary>
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

    /// <summary>Adversarial: a swap of two different-slot different-template items must
    /// still exchange the slots as before the fix.</summary>
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

    /// <summary>Event level: the actual exploit packet, "CHANGE1,1", driven through
    /// InventoryChangeSlotEvent. Low cost in this codebase — the vendor tests drive their
    /// events the same way (set Player + Data, call Ready) — so this covers parsing,
    /// validation, and the guard together. Pre-fix this doubled the stack; also assert
    /// the operation is a silent no-op (no slot-update packets sent to the client).</summary>
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
