using Goose;
using Goose.Events;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

public class VendorSellCurrencyTests
{
    private static void Sell(VendorFixture fixture, int slotId, int stack)
    {
        var ev = new VendorSellInventoryEvent
        {
            Player = fixture.Player,
            Data = "VSI" + fixture.Vendor.LoginID + "," + slotId + "," + stack,
        };
        ev.Ready(fixture.World);
    }

    private static ItemTemplate Sword(long value = 100) =>
        new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "A Sword", Value = value,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };

    [Fact]
    public void GoldSale_PaysHalfValueAndKeepsTheMessage()
    {
        using var fixture = new VendorFixture();
        fixture.Carry(Sword(value: 100));

        Sell(fixture, 1, 1);

        Assert.Equal(50, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("Sold Sword for 50 gold."));
    }

    [Fact]
    public void GoldSale_RefusesAWorthlessItem()
    {
        using var fixture = new VendorFixture();
        fixture.Carry(Sword(value: 0));

        Sell(fixture, 1, 1);

        Assert.Equal(0, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("I have no interest in purchasing Sword."));
        Assert.NotNull(fixture.Player.Inventory.GetSlot(1));
    }

    /// <summary>Parity: credit dealers buy nothing, and the item stays in the bag.</summary>
    [Fact]
    public void CreditDealer_RefusesEverySale()
    {
        using var fixture = new VendorFixture();
        fixture.VendorDealsIn(Currency.Credits);
        fixture.Carry(Sword(value: 100));

        Sell(fixture, 1, 1);

        Assert.Equal(0, fixture.Player.Gold);
        Assert.Equal(0, fixture.Player.Credits);
        Assert.NotNull(fixture.Player.Inventory.GetSlot(1));
    }

    /// <summary>Partial stacks: price and message count must come from the sold stack,
    /// and the remainder must stay in the bag - the parity edge the retrofit's single
    /// `price` collapsed (old code computed message price from sellslot.Stack).</summary>
    [Fact]
    public void GoldSale_PartialStackPaysForTheSoldCountOnly()
    {
        using var fixture = new VendorFixture();
        fixture.Carry(Sword(value: 100), stack: 5);

        Sell(fixture, 1, 2);

        Assert.Equal(100, fixture.Player.Gold);   // 2 * 100 / 2
        Assert.Contains(fixture.Player.Sent, m => m.Contains("Sold Sword (2) for 100 gold."));
        Assert.NotNull(fixture.Player.Inventory.GetSlot(1));
    }

    /// <summary>The new behaviour, and the decision from the design: an item override wins,
    /// so a dimension item sells for spirit even at a credit dealer that buys nothing else.</summary>
    [Fact]
    public void ItemOverride_PaysTheOverrideCurrencyEvenAtACreditDealer()
    {
        using var fixture = new VendorFixture();
        fixture.World.CurrencyHandler.Register(new TestSpiritCurrency());
        fixture.VendorDealsIn(Currency.Credits);

        var template = Sword(value: 100);
        template.CurrencyId = "spirit";
        fixture.Carry(template);

        Sell(fixture, 1, 1);

        Assert.Equal(50, fixture.Player.BaseStats.SP);
        Assert.Equal(0, fixture.Player.Gold);
    }

    private sealed class TestSpiritCurrency : ICurrency
    {
        public string Id => "spirit";
        public string Name => "spirit";
        public string ShortName => "sp";
        public long GetBalance(Player player) => player.BaseStats.SP;
        public long GetBuyPrice(ItemTemplate template, int stack) => template.Value * stack;
        public long GetSellPrice(Item item, int stack) => stack * item.Value / 2;
        public void Add(Player player, long amount, GameWorld world) => player.BaseStats.SP += amount;
        public void Remove(Player player, long amount, GameWorld world) => player.BaseStats.SP -= amount;
    }
}
