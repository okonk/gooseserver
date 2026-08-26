using Goose;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

/// <summary>The item packets carry a trailing currency name so the client can label Value
/// with it instead of guessing gold-or-credits from the Donation flag.</summary>
public class PacketCurrencyTests
{
    /// <summary>A script currency, standing in for spirit without compiling Dimensions.csx.</summary>
    private sealed class StubCurrency : ICurrency
    {
        public string Id => "spirit";
        public string Name => "spirit";
        public string ShortName => "sp";
        public long GetBalance(Player player) => 0;
        public long GetBuyPrice(ItemTemplate template, int stack) => template.Value * stack;
        public long GetSellPrice(Item item, int stack) => stack * item.Value / 2;
        public void Add(Player player, long amount, GameWorld world) { }
        public void Remove(Player player, long amount, GameWorld world) { }
    }

    private static ItemTemplate Template(string? currencyId = null)
    {
        return new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "A Sword", Value = 100, CurrencyId = currencyId,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };
    }

    /// <summary>The name is the last field, so an old client that stops parsing at GraphicA
    /// is unaffected.</summary>
    private static string LastField(string packet) => packet.Substring(packet.LastIndexOf('|') + 1);

    [Fact]
    public void ItemSlot_NamesGoldForAnOrdinaryItem()
    {
        using var fixture = new VendorFixture();
        var item = fixture.Carry(Template());

        var packet = P.ItemSlot(item, fixture.World, 1, 1);

        Assert.Equal("gold", LastField(packet));
    }

    [Fact]
    public void ItemSlot_NamesTheItemsOwnCurrency()
    {
        using var fixture = new VendorFixture();
        fixture.World.CurrencyHandler.Register(new StubCurrency());
        var item = fixture.Carry(Template("spirit"));

        var packet = P.ItemSlot(item, fixture.World, 1, 1);

        Assert.Equal("spirit", LastField(packet));
    }

    /// <summary>A credit dealer's stock carries no item-level currency, so the label can only
    /// come from the vendor.</summary>
    [Fact]
    public void VendorItemSlot_NamesTheVendorsCurrency()
    {
        using var fixture = new VendorFixture();
        fixture.VendorDealsIn(Currency.Credits);

        var packet = P.VendorItemSlot(Template(), fixture.World, fixture.Vendor, 1, 1);

        Assert.Equal("credits", LastField(packet));
    }

    /// <summary>Resolve gives the item override precedence, so a spirit-priced item reads as
    /// spirit even on a credit dealer's shelf (CurrencyHandler.cs:36-40).</summary>
    [Fact]
    public void VendorItemSlot_LetsTheItemCurrencyBeatTheVendors()
    {
        using var fixture = new VendorFixture();
        fixture.World.CurrencyHandler.Register(new StubCurrency());
        fixture.VendorDealsIn(Currency.Credits);

        var packet = P.VendorItemSlot(Template("spirit"), fixture.World, fixture.Vendor, 1, 1);

        Assert.Equal("spirit", LastField(packet));
    }

    [Fact]
    public void VendorSlot_PassesTheWindowsVendorThrough()
    {
        using var fixture = new VendorFixture();
        fixture.VendorDealsIn(Currency.Credits);
        var window = fixture.Player.Windows.Find(w => w.Type == Window.WindowTypes.Vendor);

        var packet = P.VendorSlot(window!, Template(), fixture.World, 1, 1);

        Assert.Equal("credits", LastField(packet));
    }

    /// <summary>The name is appended, not inserted: everything the client already parses has
    /// to keep its field position.</summary>
    [Fact]
    public void ItemSlot_KeepsGraphicAInItsExistingField()
    {
        using var fixture = new VendorFixture();
        var template = Template();
        template.GraphicA = 123;
        var item = fixture.Carry(template);

        var fields = P.ItemSlot(item, fixture.World, 1, 1).Split('|');

        Assert.Equal("123", fields[^2]);
        Assert.Equal("gold", fields[^1]);
    }
}
