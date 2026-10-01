using Goose;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>The item packets carry a trailing minimum experience so the client can show the
/// gate it enforces in Player.CanUse instead of only failing the attempt.</summary>
public class PacketMinExperienceTests
{
    private static ItemTemplate Template(long minExperience = 0)
    {
        return new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "A Sword", Value = 100,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
            MinExperience = minExperience,
        };
    }

    private static string LastField(string packet) => packet.Split('|')[^1];

    [Fact]
    public void ItemSlot_carries_the_minimum_experience()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template(20_000_000));

        Assert.Equal("20000000", LastField(P.ItemSlot(item, fixture.World, 1, 1)));
    }

    [Fact]
    public void ItemSlot_reports_no_gate_as_zero()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template());

        Assert.Equal("0", LastField(P.ItemSlot(item, fixture.World, 1, 1)));
    }

    [Fact]
    public void VendorItemSlot_carries_the_minimum_experience()
    {
        using var fixture = new VendorFixture();

        var packet = P.VendorItemSlot(Template(2_000_000_000), fixture.World, fixture.Vendor, 1, 1);

        Assert.Equal("2000000000", LastField(packet));
    }

    /// <summary>Appended, not inserted beside MinLevel: the client parses the fields before it
    /// positionally, so they must keep their offsets.</summary>
    [Fact]
    public void ItemSlot_keeps_the_earlier_fields_where_the_client_expects_them()
    {
        using var fixture = new VendorFixture();
        var template = Template(20_000_000);
        template.MinLevel = 50;
        template.MaxLevel = 60;
        var item = fixture.Carry(template);

        var fields = P.ItemSlot(item, fixture.World, 1, 1).Split('|');

        Assert.Equal("50", fields[27]);
        Assert.Equal("60", fields[28]);
        Assert.Equal("20000000", fields[^1]);
    }
}
