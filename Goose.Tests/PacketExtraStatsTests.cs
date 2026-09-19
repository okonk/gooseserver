using Goose;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class PacketExtraStatsTests
{
    private static ItemTemplate Template()
    {
        return new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "A Sword", Value = 100,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };
    }

    private static string LastField(string packet) => packet.Split('|')[^1];

    [Fact]
    public void ItemSlot_sends_nothing_extra_for_an_ordinary_item()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template());

        var packet = P.ItemSlot(item, fixture.World, 1, 1);

        Assert.Equal("", LastField(packet));
        Assert.EndsWith("|", packet);
    }

    [Fact]
    public void ItemSlot_appends_after_the_currency()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template());
        item.BaseStats.Haste = 0.04;
        item.RefreshStats();

        var fields = P.ItemSlot(item, fixture.World, 1, 1).Split('|');

        Assert.Equal("gold", fields[^2]);
        Assert.Equal("400", fields[^1]);
    }

    [Fact]
    public void VendorItemSlot_reports_template_stats()
    {
        using var fixture = new VendorFixture();
        var template = Template();
        template.BaseStats.Haste = 0.04;

        var fields = P.VendorItemSlot(template, fixture.World, fixture.Vendor, 1, 1).Split('|');

        Assert.Equal("400", fields[^1]);
        Assert.Equal("gold", fields[^2]);
    }

    [Fact]
    public void ItemSlot_reports_basis_points_in_the_documented_order()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template());
        item.BaseStats = new AttributeSet
        {
            Haste = 0.01,
            SpellDamage = 0.02,
            SpellCrit = 0.03,
            MeleeDamage = 0.04,
            MeleeCrit = 0.05,
            DamageReduction = 0.06,
            HPPercentRegen = 0.07,
            HPStaticRegen = 700,
            MPPercentRegen = 0.08,
            MPStaticRegen = 800,
            SPPercentRegen = 0.09,
            SPStaticRegen = 900,
        };
        item.RefreshStats();

        var packet = P.ItemSlot(item, fixture.World, 1, 1);

        Assert.Equal("100,200,300,400,500,600,700,700,800,800,900,900", LastField(packet));
    }

    [Fact]
    public void ItemSlot_trims_trailing_zeros_and_keeps_leading_ones()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template());
        item.BaseStats.SpellDamage = 0.08;
        item.RefreshStats();

        Assert.Equal("0,800", LastField(P.ItemSlot(item, fixture.World, 1, 1)));

        item.BaseStats.SpellDamage = 0;
        item.BaseStats.Haste = 0.04;
        item.RefreshStats();

        Assert.Equal("400", LastField(P.ItemSlot(item, fixture.World, 1, 1)));
    }

    [Fact]
    public void ItemSlot_reports_total_stats_scaled_by_the_multiplier()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template());
        item.BaseStats.SpellDamage = 0.04;
        item.StatMultiplier = 1.1;
        item.RefreshStats();

        Assert.Equal("0,440", LastField(P.ItemSlot(item, fixture.World, 1, 1)));
    }

    [Fact]
    public void ItemSlot_reports_both_halves_of_a_regen_stat()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template());
        item.BaseStats.HPPercentRegen = 0.015;
        item.BaseStats.HPStaticRegen = 1500;
        item.RefreshStats();

        Assert.Equal("0,0,0,0,0,0,150,1500", LastField(P.ItemSlot(item, fixture.World, 1, 1)));
    }
}
