using Goose;
using Xunit;

namespace Goose.Tests;

public class DataCurrencyTests
{
    private static DataCurrency Currency(bool vendorBuyback = true, CurrencyIcon? icon = null) =>
        new DataCurrency("spirit", "spirit", "sp", icon ?? new CurrencyIcon(3, 17), vendorBuyback);

    [Fact]
    public void Add_then_Remove_updates_balance()
    {
        var player = new Player(0);
        var currency = Currency();

        currency.Add(player, 500, null);

        Assert.Equal(500, currency.GetBalance(player));

        currency.Remove(player, 200, null);

        Assert.Equal(300, currency.GetBalance(player));
    }

    [Fact]
    public void Remove_below_zero_floors_at_zero()
    {
        var player = new Player(0);
        var currency = Currency();
        currency.Add(player, 10, null);

        currency.Remove(player, 500, null);

        Assert.Equal(0, currency.GetBalance(player));
    }

    [Fact]
    public void Add_saturates_at_long_max()
    {
        var player = new Player(0);
        var currency = Currency();
        currency.Add(player, long.MaxValue - 1, null);

        currency.Add(player, 5, null);

        Assert.Equal(long.MaxValue, currency.GetBalance(player));
    }

    [Fact]
    public void Balance_survives_properties_json_round_trip()
    {
        var player = new Player(0);
        var currency = Currency();
        currency.Add(player, 500, null);

        string json = JsonHelper.Serialize(player.Properties.Clone());
        player.LoadPropertiesFromColumn(json);

        Assert.Equal(500, currency.GetBalance(player));
    }

    [Fact]
    public void GetBuyPrice_is_template_value_times_stack()
    {
        var currency = Currency();
        var template = new ItemTemplate { Value = 100 };

        Assert.Equal(300, currency.GetBuyPrice(template, 3));
    }

    [Fact]
    public void GetSellPrice_refuses_when_buyback_disabled()
    {
        var currency = Currency(vendorBuyback: false);
        var item = new Item { Value = 100 };

        Assert.Equal(-1, currency.GetSellPrice(item, 1));
    }

    [Fact]
    public void GetSellPrice_pays_half_when_enabled()
    {
        var currency = Currency(vendorBuyback: true);
        var item = new Item { Value = 100 };

        Assert.Equal(150, currency.GetSellPrice(item, 3));
    }

    [Fact]
    public void GetSellPrice_refuses_zero_value()
    {
        var currency = Currency(vendorBuyback: true);
        var item = new Item { Value = 0 };

        Assert.Equal(-1, currency.GetSellPrice(item, 5));
    }

    [Fact]
    public void Icon_is_null_for_zero_sheet_and_graphic()
    {
        var currency = new DataCurrency("spirit", "spirit", "sp", new CurrencyIcon(0, 0), true);

        Assert.Null(currency.Icon);
    }

    [Fact]
    public void Icon_is_kept_when_sheet_or_graphic_is_nonzero()
    {
        var currency = new DataCurrency("spirit", "spirit", "sp", new CurrencyIcon(3, 0), true);

        Assert.Equal(new CurrencyIcon(3, 0), currency.Icon);
    }

    [Fact]
    public void Update_replaces_name_short_name_icon_and_buyback()
    {
        var currency = new DataCurrency("spirit", "spirit", "sp", null, true);

        currency.Update("mana", "mn", new CurrencyIcon(1, 2), false);

        Assert.Equal("mana", currency.Name);
        Assert.Equal("mn", currency.ShortName);
        Assert.Equal(new CurrencyIcon(1, 2), currency.Icon);
        Assert.Equal(-1, currency.GetSellPrice(new Item { Value = 100 }, 1));
    }

    [Fact]
    public void PropertyKey_is_prefixed_with_currency()
    {
        Assert.Equal("currency.spirit", DataCurrency.PropertyKey("spirit"));
    }
}
