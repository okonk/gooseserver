using Goose;
using Xunit;

namespace Goose.Tests;

public class BuiltInCurrencyTests
{
    private static ItemTemplate Template(long value = 100, int credits = 0) =>
        new ItemTemplate { ID = 1, Name = "Sword", Value = value, Credits = credits, BaseStats = new AttributeSet() };

    private static Item ItemOf(ItemTemplate template)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        return item;
    }

    [Fact]
    public void Gold_NamesMatchTheStringsTheVendorEventsPrint()
    {
        var gold = new GoldCurrency();

        Assert.Equal(Currency.Gold, gold.Id);
        Assert.Equal("gold", gold.Name);
        Assert.Equal("gp", gold.ShortName);
    }

    [Fact]
    public void Gold_PricesBuysAtValueTimesStack()
    {
        Assert.Equal(300, new GoldCurrency().GetBuyPrice(Template(value: 100), 3));
    }

    [Fact]
    public void Gold_PaysHalfValueOnSell()
    {
        Assert.Equal(150, new GoldCurrency().GetSellPrice(ItemOf(Template(value: 100)), 3));
    }

    /// <summary>Reproduces VendorSellInventoryEvent.cs:78 - a worthless item is refused.</summary>
    [Fact]
    public void Gold_RefusesToBuyAWorthlessItem()
    {
        Assert.True(new GoldCurrency().GetSellPrice(ItemOf(Template(value: 0)), 1) < 0);
    }

    [Fact]
    public void Gold_BalanceIsThePlayersGold()
    {
        var player = new Player(0) { Gold = 4200 };

        Assert.Equal(4200, new GoldCurrency().GetBalance(player));
    }

    [Fact]
    public void Credits_NamesMatchTheStringsTheVendorEventsPrint()
    {
        var credits = new CreditsCurrency();

        Assert.Equal(Currency.Credits, credits.Id);
        Assert.Equal("credits", credits.Name);
        Assert.Equal("cr", credits.ShortName);
    }

    [Fact]
    public void Credits_PricesBuysAtTheCreditsValue()
    {
        Assert.Equal(20, new CreditsCurrency().GetBuyPrice(Template(credits: 10), 2));
    }

    /// <summary>Reproduces VendorSellInventoryEvent.cs:75 - credit dealers buy nothing.
    /// Unconditional, because credits_value defaults to 0 (items.sql:46) so the old
    /// Credits >= 0 test was true for every row.</summary>
    [Fact]
    public void Credits_RefusesEverySale()
    {
        Assert.True(new CreditsCurrency().GetSellPrice(ItemOf(Template(value: 500, credits: 10)), 1) < 0);
    }

    /// <summary>Player.Credits is an int (Player.cs:427). Clamp rather than wrap.</summary>
    [Fact]
    public void Credits_ClampsRatherThanWrappingOnOverflow()
    {
        var player = new Player(0) { Credits = 5 };

        new CreditsCurrency().Add(player, (long)int.MaxValue + 1000, world: null!);

        Assert.Equal(int.MaxValue, player.Credits);
    }

    [Fact]
    public void BuiltInsAreRegisteredOnAFreshWorld()
    {
        var world = new GameWorld(new GooseSettings());

        Assert.NotNull(world.CurrencyHandler.Get(Currency.Gold));
        Assert.NotNull(world.CurrencyHandler.Get(Currency.Credits));
    }
}
