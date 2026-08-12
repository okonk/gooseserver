using Goose;
using Goose.Events;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class VendorPurchaseCurrencyTests
{
    private static void Purchase(VendorFixture fixture, int slotId)
    {
        var ev = new VendorPurchaseInventoryEvent
        {
            Player = fixture.Player,
            Data = "VPI" + fixture.Vendor.LoginID + "," + slotId,
        };
        ev.Ready(fixture.World);
    }

    private static ItemTemplate Sword(GlobalScriptFixture _, long value = 100, int credits = 0) =>
        new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "A Sword", Value = value, Credits = credits,
            BaseStats = new AttributeSet(), StackSize = 1, ScriptParams = "",
            Slot = ItemTemplate.ItemSlots.OneHanded,
        };

    /// <summary>Parity: an ordinary item at an ordinary vendor still costs gold, and the
    /// message still reads exactly as it did.</summary>
    [Fact]
    public void GoldPurchase_DebitsGoldAndKeepsTheMessage()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Gold = 500;
        fixture.Stock(1, Sword(null, value: 100));

        Purchase(fixture, 1);

        Assert.Equal(400, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("Purchased Sword for 100 gold."));
    }

    [Fact]
    public void GoldPurchase_RefusesWhenTheBalanceIsShort()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Gold = 10;
        fixture.Stock(1, Sword(null, value: 100));

        Purchase(fixture, 1);

        Assert.Equal(10, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("you don't have enough gold."));
    }

    /// <summary>Parity: a credit dealer still charges the credits_value, not the gold value.</summary>
    [Fact]
    public void CreditPurchase_DebitsCreditsAndKeepsTheMessage()
    {
        using var fixture = new VendorFixture();
        fixture.VendorDealsIn(Currency.Credits);
        fixture.Player.Credits = 50;
        fixture.Player.Gold = 9999;
        fixture.Stock(1, Sword(null, value: 100, credits: 10));

        Purchase(fixture, 1);

        Assert.Equal(40, fixture.Player.Credits);
        Assert.Equal(9999, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("Purchased Sword for 10 credits."));
    }

    /// <summary>The new behaviour: an item override wins over the vendor's currency.</summary>
    [Fact]
    public void ItemOverride_ChargesTheOverrideCurrencyAtAGoldVendor()
    {
        using var fixture = new VendorFixture();
        fixture.World.CurrencyHandler.Register(new TestSpiritCurrency());
        fixture.Player.Gold = 9999;
        fixture.Player.BaseStats.SP = 500;

        var template = Sword(null, value: 100);
        template.CurrencyId = "spirit";
        fixture.Stock(1, template);

        Purchase(fixture, 1);

        Assert.Equal(400, fixture.Player.BaseStats.SP);
        Assert.Equal(9999, fixture.Player.Gold);
    }

    /// <summary>A full inventory must cost nothing - the charge happens only after AddItem
    /// succeeds, exactly as before the retrofit.</summary>
    [Fact]
    public void FullInventory_ChargesNothing()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Gold = 500;
        fixture.Stock(1, Sword(null, value: 100));
        FillInventory(fixture);

        Purchase(fixture, 1);

        Assert.Equal(500, fixture.Player.Gold);
        Assert.Contains(fixture.Player.Sent, m => m.Contains("your inventory is full."));
    }

    /// <summary>The cost < 0 guard: the old code would have granted gold for a
    /// negative-Value template (RemoveGold(-100) passes the balance check and adds).
    /// The refusal must leave the wallet untouched.</summary>
    [Fact]
    public void NegativePrice_RefusesThePurchase()
    {
        using var fixture = new VendorFixture();
        fixture.Player.Gold = 500;
        fixture.Stock(1, Sword(null, value: -100));

        Purchase(fixture, 1);

        Assert.Equal(500, fixture.Player.Gold);
        Assert.DoesNotContain(fixture.Player.Sent, m => m.Contains("Purchased"));
    }

    private static void FillInventory(VendorFixture fixture)
    {
        var filler = new ItemTemplate
        {
            ID = 99, Name = "Rock", Value = 1, BaseStats = new AttributeSet(),
            StackSize = 1, ScriptParams = "", Slot = ItemTemplate.ItemSlots.OneHanded,
        };
        for (int i = 0; i < GameWorld.Settings.InventorySize; i++) fixture.Carry(filler);
    }

    /// <summary>A minimal spirit stand-in. The real one lives in Dimensions.csx (Task 9);
    /// this test must not depend on that script compiling.</summary>
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
