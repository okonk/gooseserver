using Goose;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;
using Xunit;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class SpiritCurrencyTests
{
    private const int Offset = 100000;   // Dimensions.csx:19

    /// <summary>OnLoaded spawns the dimension-0 warden on map 1 (WardenMapId), so every
    /// test that drives OnLoaded needs a base map first.</summary>
    private static void Loaded(GlobalScriptFixture fixture)
    {
        fixture.AddBaseMap(1, "Town");
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
    }

    [Fact]
    public void OnLoaded_RegistersSpirit()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);

        Loaded(fixture);

        Assert.NotNull(fixture.World.CurrencyHandler.Get("spirit"));
    }

    [Fact]
    public void DimensionClones_ArePricedInSpirit()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);

        Loaded(fixture);

        var clone = fixture.World.ItemHandler.GetTemplate(1 + Offset);
        Assert.Equal("spirit", clone.CurrencyId);
    }

    /// <summary>Base templates must keep the default so ordinary vendors keep taking gold.</summary>
    [Fact]
    public void BaseTemplates_KeepTheDefaultCurrency()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);

        Loaded(fixture);

        Assert.Null(fixture.World.ItemHandler.GetTemplate(1).CurrencyId);
    }

    [Fact]
    public void Spirit_PricesBuysAtValueAndSellsAtHalf()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        Loaded(fixture);

        var spirit = fixture.World.CurrencyHandler.Get("spirit");
        var clone = fixture.World.ItemHandler.GetTemplate(1 + Offset);
        var item = new Item();
        item.LoadFromTemplate(clone);

        Assert.Equal(clone.Value * 2, spirit.GetBuyPrice(clone, 2));
        Assert.Equal(clone.Value / 2, spirit.GetSellPrice(item, 1));
    }

    /// <summary>The wallet is BaseStats.SP, which persists as players.player_sp. MaxStats.SP
    /// is separate accounting, so both must move together or MaxSP clamps CurrentSP down.</summary>
    [Fact]
    public void Spirit_AddMovesBothTheBalanceAndTheMaximum()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        Loaded(fixture);

        var map = fixture.World.MapHandler.GetMap(1);
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit");

        spirit.Add(player, 500, fixture.World);

        Assert.Equal(500, spirit.GetBalance(player));
        Assert.Equal(500, player.MaxStats.SP);
        Assert.Equal(player.MaxSP, player.CurrentSP);
    }

    [Fact]
    public void Spirit_RemoveMovesBothBack()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        Loaded(fixture);

        var map = fixture.World.MapHandler.GetMap(1);
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit");

        spirit.Add(player, 500, fixture.World);
        spirit.Remove(player, 200, fixture.World);

        Assert.Equal(300, spirit.GetBalance(player));
        Assert.Equal(300, player.MaxStats.SP);
        Assert.Equal(player.MaxSP, player.CurrentSP);
    }

    /// <summary>Gear that grants SP raises MaxStats.SP only, so it cannot inflate the wallet.
    /// Documenting the asymmetry rather than guarding it - see the design doc.</summary>
    [Fact]
    public void Spirit_BalanceIgnoresGearGrantedSP()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t => t.Value = 100);
        Loaded(fixture);

        var map = fixture.World.MapHandler.GetMap(1);
        var player = fixture.PlayerOn(map, 5, 5);
        var spirit = fixture.World.CurrencyHandler.Get("spirit");

        spirit.Add(player, 100, fixture.World);
        player.AddStats(new AttributeSet { SP = 1000 }, fixture.World);   // as equipping would

        Assert.Equal(100, spirit.GetBalance(player));
    }
}
