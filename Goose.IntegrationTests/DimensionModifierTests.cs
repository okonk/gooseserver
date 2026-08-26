using Goose.IntegrationTests.Fixtures;

namespace Goose.IntegrationTests;

public class DimensionModifierTests
{
    private static GlobalScriptFixture Run()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon,
            t => t.MinLevel = 50);   // tier 0.5
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Registers_six_surnames_and_two_titles_that_can_never_roll_natively()
    {
        using var fixture = Run();

        Assert.Equal(6, fixture.World.ItemHandler.SurnameCount);
        Assert.Equal(2, fixture.World.ItemHandler.TitleCount);

        // Chance 0 makes RollModifier's range empty (ItemHandler.cs:272-277), so these can
        // only ever be applied explicitly by the dimension script.
        for (int i = 0; i < 6; i++)
            Assert.Equal(0, fixture.World.ItemHandler.GetSurname(900000 + i)!.Chance);

        Assert.Equal("of Vita Regen", fixture.World.ItemHandler.GetSurname(900000)!.Name);
        Assert.Equal("of Speed", fixture.World.ItemHandler.GetSurname(900005)!.Name);
        Assert.Equal("Legendary", fixture.World.ItemHandler.GetTitle(900100)!.Name);
        Assert.Equal("Stunted", fixture.World.ItemHandler.GetTitle(900101)!.Name);
    }

    [Theory]
    [InlineData(900002, "SpellCrit")]
    [InlineData(900003, "SpellDamage")]
    [InlineData(900004, "DamageReduction")]
    [InlineData(900005, "Haste")]
    public void Percentage_suffixes_add_four_percent_per_dimension_and_tier(int surnameId, string stat)
    {
        using var fixture = Run();
        var item = ItemOfDimension(fixture, dim: 3);   // tier 0.5

        fixture.World.ItemHandler.GetSurname(surnameId)!.ApplyStats(item, fixture.World);

        // AttributeSet.java:422,428,437,438 - 0.04 * dim * tier
        var expected = 0.04m * 3 * 0.5m;
        Assert.Equal(expected, StatOf(item.BaseStats, stat));
    }

    [Fact]
    public void Vita_regen_adds_both_regen_stats()
    {
        using var fixture = Run();
        var item = ItemOfDimension(fixture, dim: 3);

        fixture.World.ItemHandler.GetSurname(900000)!.ApplyStats(item, fixture.World);

        Assert.Equal(0.015m * 3 * 0.5m, item.BaseStats.HPPercentRegen);   // AttributeSet.java:430
        Assert.Equal((int)(1500 * 3 * 0.5), item.BaseStats.HPStaticRegen); // AttributeSet.java:431
        Assert.Equal(0, item.BaseStats.MPStaticRegen);
    }

    [Fact]
    public void Rarity_titles_only_touch_the_stat_multiplier()
    {
        using var fixture = Run();
        var legendary = ItemOfDimension(fixture, dim: 1);
        var stunted = ItemOfDimension(fixture, dim: 1);

        fixture.World.ItemHandler.GetTitle(900100)!.ApplyStats(legendary, fixture.World);
        fixture.World.ItemHandler.GetTitle(900101)!.ApplyStats(stunted, fixture.World);

        Assert.Equal(1.25, legendary.StatMultiplier);   // Item.java:394
        Assert.Equal(0.5, stunted.StatMultiplier);      // Item.java:398
    }

    [Fact]
    public void Rarity_multiplier_parses_invariant_under_any_culture()
    {
        using var fixture = Run();
        var legendary = ItemOfDimension(fixture, dim: 1);

        // The ScriptParams must be parsed invariant, like ItemModifierScript.csx's JSON.
        // Under de-DE, double.Parse("1.25") returns 125 - a silent 100x stat inflation -
        // and under fr-FR/ar-SA it throws, which ItemModifier.ApplyStats swallows so the
        // title never applies. CurrentCulture is thread-local and restored in the finally,
        // so the flip cannot leak to parallel tests.
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            fixture.World.ItemHandler.GetTitle(900100)!.ApplyStats(legendary, fixture.World);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }

        Assert.Equal(1.25, legendary.StatMultiplier);
    }

    private static Item ItemOfDimension(GlobalScriptFixture fixture, int dim)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(50 + 100000 * dim)!);
        return item;
    }

    private static decimal StatOf(AttributeSet stats, string name) => name switch
    {
        "SpellCrit" => stats.SpellCrit,
        "SpellDamage" => stats.SpellDamage,
        "DamageReduction" => stats.DamageReduction,
        "Haste" => stats.Haste,
        _ => throw new ArgumentException(name),
    };
}
