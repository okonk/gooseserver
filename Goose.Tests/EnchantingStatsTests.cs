using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class EnchantingStatsTests
{
    private static Item ItemWithCounts(TestWorldFixture fixture, double templateSpellDamage = 0)
    {
        var template = fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.BaseStats = new AttributeSet { SpellDamage = templateSpellDamage };
        });

        var item = new Item();
        item.LoadFromTemplate(template);
        return item;
    }

    private static ItemProperty Property(EnchantElement element) => element switch
    {
        EnchantElement.Earth => ItemProperty.EnchantEarth,
        EnchantElement.Water => ItemProperty.EnchantWater,
        EnchantElement.Fire => ItemProperty.EnchantFire,
        EnchantElement.Air => ItemProperty.EnchantAir,
        _ => ItemProperty.EnchantSpirit,
    };

    [Fact]
    public void RefreshStats_adds_enchant_stats_after_the_multiplier()
    {
        using var fixture = new TestWorldFixture();
        var item = ItemWithCounts(fixture, templateSpellDamage: 0.05);
        item.StatMultiplier = 2;
        item.ItemProperties[ItemProperty.EnchantEarth] = 3;

        item.RefreshStats(fixture.Settings);

        Assert.Equal(0.05 * 2 + 0.03, item.TotalStats.SpellDamage, 10);
    }

    [Fact]
    public void RefreshStats_reflects_current_per_level_settings()
    {
        using var fixture = new TestWorldFixture();
        var item = ItemWithCounts(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 3;

        item.RefreshStats(fixture.Settings);
        var first = item.TotalStats.SpellDamage;

        fixture.Settings.Enchanting.EarthSpellDamagePercent = 2;
        item.RefreshStats(fixture.Settings);

        Assert.Equal(0.03, first, 10);
        Assert.Equal(0.06, item.TotalStats.SpellDamage, 10);
    }

    [Fact]
    public void Enchant_properties_round_trip_through_database_json()
    {
        using var fixture = new TestWorldFixture();
        var item = ItemWithCounts(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 3;
        item.ItemProperties[ItemProperty.EnchantWater] = 2;
        item.ItemProperties[ItemProperty.EnchantMaxLoss] = 1;

        string json = JsonHelper.Serialize(item);
        Item restored = JsonHelper.Deserialize<Item>(json)!;

        Assert.Equal(3, Enchanting.Count(restored, EnchantElement.Earth));
        Assert.Equal(2, Enchanting.Count(restored, EnchantElement.Water));
        Assert.Equal(0, Enchanting.Count(restored, EnchantElement.Fire));
        Assert.Equal(5, Enchanting.Level(restored));
        Assert.Equal(fixture.Settings.Enchanting.MaxLevel - 1, Enchanting.Max(restored, fixture.Settings.Enchanting));
    }

    [Theory]
    [InlineData(EnchantElement.Earth, 1)]
    [InlineData(EnchantElement.Water, 1)]
    [InlineData(EnchantElement.Fire, 0.5)]
    [InlineData(EnchantElement.Air, 0.04)]
    [InlineData(EnchantElement.Spirit, 0.04)]
    public void Enchanting_stats_map_each_element_to_its_stat(EnchantElement element, double percent)
    {
        using var fixture = new TestWorldFixture();
        var item = ItemWithCounts(fixture);
        item.ItemProperties[Property(element)] = 4;

        var stats = Enchanting.Stats(item, fixture.Settings.Enchanting);

        foreach (EnchantElement other in Enum.GetValues<EnchantElement>())
        {
            double expected = other == element ? 4 * percent / 100 : 0;
            Assert.Equal(expected, StatOf(stats, other), 10);
        }
    }

    private static double StatOf(AttributeSet stats, EnchantElement element) => element switch
    {
        EnchantElement.Earth => stats.SpellDamage,
        EnchantElement.Water => stats.SpellCrit,
        EnchantElement.Fire => stats.DamageReduction,
        EnchantElement.Air => stats.MPPercentRegen,
        _ => stats.HPPercentRegen,
    };
}
