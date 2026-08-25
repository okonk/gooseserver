using Goose;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class InventoryUseMapFlagTests
{
    private static ItemTemplate Template(ItemTemplate.UseTypes useType) =>
        new ItemTemplate
        {
            ID = 1, Name = "Test Item", Description = "", Value = 0, Credits = 0,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            UseType = useType,
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };

    [Fact]
    public void NoItemsMap_BlocksAllItemUse()
    {
        using var fixture = new VendorFixture();
        fixture.Map.CanUseItems = false;

        fixture.Carry(Template(ItemTemplate.UseTypes.OneTime));
        fixture.Player.Inventory.Use(1, fixture.World);

        Assert.Contains(fixture.Player.Sent, s => s.Contains("You can't use items in this map."));
    }

    [Fact]
    public void NoItemsMap_BlocksEquipment()
    {
        using var fixture = new VendorFixture();
        fixture.Map.CanUseItems = false;

        fixture.Carry(Template(ItemTemplate.UseTypes.Armor));
        fixture.Player.Inventory.Use(1, fixture.World);

        Assert.Contains(fixture.Player.Sent, s => s.Contains("You can't use items in this map."));
    }

    [Fact]
    public void ItemsEnabledMap_DoesNotBlock()
    {
        using var fixture = new VendorFixture();
        fixture.Map.CanUseItems = true;

        fixture.Carry(Template(ItemTemplate.UseTypes.NoUse));
        fixture.Player.Inventory.Use(1, fixture.World);

        Assert.DoesNotContain(fixture.Player.Sent, s => s.Contains("You can't use items in this map."));
    }
}
