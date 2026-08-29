using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class DestroyItemEventTests
{
    private static ItemTemplate CustomHelmet(int id, string description) =>
        new ItemTemplate
        {
            ID = id, Name = "Ticket", Description = description, Value = 10,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.Helmet,
        };

    [Fact]
    public void DestroyEquippedCustomItem_MissingTicketTemplate_LeavesItemEquipped()
    {
        using var fixture = new VendorFixture();
        fixture.World.Settings.RippedCustomTicketId = 99999;

        var item = new Item();
        item.LoadFromTemplate(CustomHelmet(777, "Custom created by Tester"));
        fixture.World.ItemHandler.AddAndAssignId(item, fixture.World);
        Assert.True(fixture.Player.Inventory.AddItem(item, 1, fixture.World));
        Assert.True(fixture.Player.Inventory.Equip(item, fixture.World));

        // Equipped client slot ids are InventorySize + (int)EquipSlot + 1
        // (see Inventory.Unequip(int, GameWorld)).
        int slotId = fixture.Settings.InventorySize + (int)Inventory.EquipSlots.Head + 1;
        var ev = new DestroyItemEvent
        {
            Player = fixture.Player,
            Data = $"DITM{slotId}",
        };
        ev.Ready(fixture.World);

        Assert.Same(item, fixture.Player.Inventory.GetEquippedSlot(Inventory.EquipSlots.Head)?.Item);
        foreach (var slot in fixture.Player.Inventory.GetInventorySlots())
        {
            Assert.NotSame(item, slot?.Item);
        }
    }
}
