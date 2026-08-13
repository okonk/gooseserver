using Goose;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
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
}
