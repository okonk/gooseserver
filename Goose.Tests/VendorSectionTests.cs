using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class VendorSectionTests
{
    private static ItemTemplate Item(int id, string name) => new()
    {
        ID = id, Name = name, Description = name, Value = 10, BaseStats = new AttributeSet(),
        StackSize = 1, ScriptParams = "", Slot = ItemTemplate.ItemSlots.OneHanded,
    };

    private static NPCVendorSection Section(VendorFixture fixture, string name, params ItemTemplate[] items)
    {
        var slots = new NPCVendorSlot?[fixture.Settings.VendorSlotSize + 1];
        for (int i = 0; i < items.Length; i++)
            slots[i + 1] = new NPCVendorSlot { Slot = i + 1, ItemTemplate = items[i], Stack = 1 };
        return new NPCVendorSection { Name = name, Slots = slots };
    }

    private static VendorFixture SectionedShop(bool withGeneral)
    {
        var fixture = new VendorFixture();
        fixture.Player.Windows.Clear();
        fixture.Player.Gold = 1000;
        if (withGeneral) fixture.Stock(1, Item(1, "Potion"));
        else fixture.Vendor.NPCTemplate.VendorItems = null;
        fixture.Vendor.NPCTemplate.VendorSections =
        [
            Section(fixture, "Enchanting", Item(2, "Rune"), Item(3, "Dust")),
            Section(fixture, "Head Cosmetics", Item(4, "Hat")),
        ];
        return fixture;
    }

    [Fact]
    public void Sectioned_vendor_opens_a_section_list_with_general_stock_first()
    {
        using var fixture = SectionedShop(withGeneral: true);

        fixture.Vendor.OpenVendor(fixture.Player, fixture.World);

        Assert.IsType<OptionListWindow>(Assert.Single(fixture.Player.Windows));
        Assert.Contains(fixture.Player.Sent, p => p.Contains("General"));
        Assert.Contains(fixture.Player.Sent, p => p.Contains("Enchanting"));
        Assert.Contains(fixture.Player.Sent, p => p.Contains("Head Cosmetics"));
    }

    [Fact]
    public void Picking_a_section_opens_a_vendor_window_that_sells_that_sections_slots()
    {
        using var fixture = SectionedShop(withGeneral: true);
        fixture.Vendor.OpenVendor(fixture.Player, fixture.World);
        var list = (OptionListWindow)fixture.Player.Windows[0];

        list.LineClicked(1, fixture.Vendor.LoginID, fixture.Player, fixture.World);

        var vendor = Assert.Single(fixture.Player.Windows);
        Assert.Equal(Window.WindowTypes.Vendor, vendor.Type);
        Assert.Equal("Enchanting", vendor.Title);

        new VendorPurchaseInventoryEvent { Player = fixture.Player, Data = "VPI" + fixture.Vendor.LoginID + ",2" }.Ready(fixture.World);

        Assert.Contains(fixture.Player.Sent, m => m.Contains("Purchased Dust"));
        Assert.DoesNotContain(fixture.Player.Sent, m => m.Contains("Purchased Potion"));
    }

    [Fact]
    public void Sections_only_vendor_lists_just_its_sections()
    {
        using var fixture = SectionedShop(withGeneral: false);

        fixture.Vendor.OpenVendor(fixture.Player, fixture.World);
        var list = (OptionListWindow)fixture.Player.Windows[0];
        list.LineClicked(0, fixture.Vendor.LoginID, fixture.Player, fixture.World);

        Assert.DoesNotContain(fixture.Player.Sent, p => p.Contains("General"));
        Assert.Equal("Enchanting", Assert.Single(fixture.Player.Windows).Title);
    }

    [Fact]
    public void Unsectioned_vendor_still_opens_its_shop_directly()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Windows.Clear();
        fixture.Stock(1, Item(1, "Potion"));

        fixture.Vendor.OpenVendor(fixture.Player, fixture.World);

        var window = Assert.Single(fixture.Player.Windows);
        Assert.Equal(Window.WindowTypes.Vendor, window.Type);
        Assert.Null(window.VendorSlots);
    }
}
