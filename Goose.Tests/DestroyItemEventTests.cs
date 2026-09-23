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
            Slot = ItemTemplate.ItemSlots.Helmet, UseType = ItemTemplate.UseTypes.Armor,
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

    [Fact]
    public void DestroyCustomFlaggedConsumable_DestroysItWithoutRefundingAPiece()
    {
        using var fixture = new VendorFixture();
        fixture.World.Settings.RippedCustomTicketId = 697;
        fixture.World.ItemHandler.AddTemplate(RippedTicketPiece(697));

        var potion = new Item();
        potion.LoadFromTemplate(CustomFlaggedPotion(698));
        fixture.World.ItemHandler.AddAndAssignId(potion, fixture.World);
        Assert.True(fixture.Player.Inventory.AddItem(potion, 5, fixture.World));

        var ev = new DestroyItemEvent
        {
            Player = fixture.Player,
            Data = "DITM1",
        };
        ev.Ready(fixture.World);

        Assert.Null(fixture.Player.Inventory.GetSlot(1));
        foreach (var slot in fixture.Player.Inventory.GetInventorySlots())
        {
            Assert.NotEqual(697, slot?.Item.TemplateID);
        }
    }

    private static ItemTemplate RippedTicketPiece(int id) =>
        new ItemTemplate
        {
            ID = id, Name = "Ripped Ticket Piece", Description = "", Value = 0,
            BaseStats = new AttributeSet(), StackSize = 99, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.Misc,
        };

    private static ItemTemplate CustomFlaggedPotion(int id) =>
        new ItemTemplate
        {
            ID = id, Name = "Hair Dye", Description = "Custom created by Tester", Value = 1000,
            BaseStats = new AttributeSet(), StackSize = 99, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.OneTime,
        };
}
