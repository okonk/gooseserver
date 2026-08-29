using System.Linq;
using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class VendorFixtureTests
{
    [Fact]
    public void CapturingPlayer_RecordsWhatTheServerSends()
    {
        using var fixture = new VendorFixture();

        fixture.World.Send(fixture.Player, "hello");

        Assert.Contains(fixture.Player.Sent, m => m.Contains("hello"));
    }

    [Fact]
    public void Vendor_IsInRangeAndVisibleToThePlayer()
    {
        using var fixture = new VendorFixture();

        Assert.Same(fixture.Player.Map, fixture.Vendor.Map);
        Assert.Contains(fixture.Player.Windows, w => w.Type == Window.WindowTypes.Vendor);
    }

    [Fact]
    public void VendorWindow_Populate_NonVendorNpc_DoesNotThrow()
    {
        using var fixture = new VendorFixture();
        fixture.Vendor.NPCTemplate.VendorItems = null;

        var window = fixture.Player.Windows.First(w => w.Type == Window.WindowTypes.Vendor);
        window.Populate(fixture.Player, fixture.World);

        Assert.Contains(fixture.Player.Sent, m => m.StartsWith("VCL"));
    }

    [Fact]
    public void VendorPurchase_OutOfRangeSlotId_IsRejected()
    {
        using var fixture = new VendorFixture();
        // Shrink the slot array by one so a settings-legal slotid (<= VendorSlotSize)
        // lands past the end of VendorItems.
        fixture.Vendor.NPCTemplate.VendorItems = new NPCVendorSlot[fixture.Settings.VendorSlotSize];

        var ev = new VendorPurchaseInventoryEvent { Player = fixture.Player };
        ev.Data = "VPI900," + fixture.Settings.VendorSlotSize;

        ev.Ready(fixture.World);

        Assert.DoesNotContain(fixture.Player.Sent, m => m.StartsWith("Purchased"));
    }
}
