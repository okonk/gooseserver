using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class EnchantingAttemptTests
{
    private sealed class SequenceRandom(params int[] values) : Random
    {
        private readonly Queue<int> values = new(values);

        public bool IsEmpty => values.Count == 0;

        public override int Next(int minValue, int maxValue)
        {
            var value = values.Dequeue();
            Assert.InRange(value, minValue, maxValue - 1);
            return value;
        }
    }

    private static (Item Item, EnchantSettings Settings) Arrange(
        TestWorldFixture fixture,
        ItemTemplate.UseTypes useType = ItemTemplate.UseTypes.Weapon,
        ItemTemplate.ItemSlots slot = ItemTemplate.ItemSlots.OneHanded,
        int maxLevel = 10,
        int[]? failPercent = null)
    {
        var template = fixture.AddBaseItemTemplate(1, "Sword", useType, t => t.Slot = slot);
        var item = new Item();
        item.LoadFromTemplate(template);
        var settings = new EnchantSettings
        {
            MaxLevel = maxLevel,
            FailPercent = failPercent ?? [0, 0, 0, 0, 0, 10, 20, 30, 40, 50],
        };
        return (item, settings);
    }

    [Fact]
    public void CanEnchant_allows_armors_and_weapons()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture, useType: ItemTemplate.UseTypes.Armor);

        Assert.True(Enchanting.CanEnchant(item, settings, out string reason));
        Assert.True(string.IsNullOrEmpty(reason));
    }

    [Fact]
    public void CanEnchant_refuses_mounts()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture, slot: ItemTemplate.ItemSlots.Mount);

        Assert.False(Enchanting.CanEnchant(item, settings, out _));
    }

    [Fact]
    public void CanEnchant_refuses_backpacks()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture, slot: ItemTemplate.ItemSlots.Backpack);

        Assert.False(Enchanting.CanEnchant(item, settings, out _));
    }

    [Fact]
    public void CanEnchant_refuses_items_that_cannot_be_used()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture, useType: ItemTemplate.UseTypes.NoUse);

        Assert.False(Enchanting.CanEnchant(item, settings, out _));
    }

    [Fact]
    public void CanEnchant_refuses_items_at_max()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 10;

        Assert.False(Enchanting.CanEnchant(item, settings, out _));
    }

    [Fact]
    public void CanEnchant_refuses_items_at_a_lowered_max()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 7;
        item.ItemProperties[ItemProperty.EnchantMaxLoss] = 3;

        Assert.Equal(10, settings.MaxLevel);
        Assert.Equal(7, Enchanting.Max(item, settings));
        Assert.False(Enchanting.CanEnchant(item, settings, out _));
    }

    [Fact]
    public void FailPercent_returns_the_target_level_chance_and_zero_when_full()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 5;
        Assert.Equal(10, Enchanting.FailPercent(item, settings));
        item.ItemProperties[ItemProperty.EnchantEarth] = 9;
        Assert.Equal(50, Enchanting.FailPercent(item, settings));
        item.ItemProperties[ItemProperty.EnchantEarth] = 10;
        Assert.Equal(0, Enchanting.FailPercent(item, settings));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Resolve_succeeds_without_rolling_when_fail_percent_is_zero(int currentLevel)
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = currentLevel;

        var random = new SequenceRandom(1);
        var outcome = Enchanting.Resolve(item, EnchantElement.Earth, false, settings, random);

        Assert.Equal(EnchantOutcome.Success, outcome);
        Assert.Equal(currentLevel + 1, Enchanting.Count(item, EnchantElement.Earth));
        Assert.Equal($"Sword +{currentLevel + 1}", item.Name);
        Assert.False(random.IsEmpty);
    }

    [Fact]
    public void Resolve_fails_when_roll_equals_fail_percent()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 5;

        var outcome = Enchanting.Resolve(item, EnchantElement.Earth, false, settings, new SequenceRandom(10));

        Assert.Equal(EnchantOutcome.Destroyed, outcome);
        Assert.Equal(5, Enchanting.Level(item));
    }

    [Fact]
    public void Resolve_succeeds_when_roll_is_just_above_fail_percent()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 5;

        var outcome = Enchanting.Resolve(item, EnchantElement.Earth, false, settings, new SequenceRandom(11));

        Assert.Equal(EnchantOutcome.Success, outcome);
        Assert.Equal(6, Enchanting.Level(item));
    }

    [Fact]
    public void Resolve_protected_failure_lowers_max_and_keeps_counts_and_name()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 5;
        Enchanting.ApplySuffix(item);

        var outcome = Enchanting.Resolve(item, EnchantElement.Earth, true, settings, new SequenceRandom(10));

        Assert.Equal(EnchantOutcome.ProtectedFailure, outcome);
        Assert.Equal(9, Enchanting.Max(item, settings));
        Assert.Equal(5, Enchanting.Count(item, EnchantElement.Earth));
        Assert.Equal("Sword +5", item.Name);
    }

    [Fact]
    public void Resolve_destroyed_keeps_counts_unchanged()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 5;
        item.ItemProperties[ItemProperty.EnchantFire] = 2;

        var outcome = Enchanting.Resolve(item, EnchantElement.Water, false, settings, new SequenceRandom(5));

        Assert.Equal(EnchantOutcome.Destroyed, outcome);
        Assert.Equal(5, Enchanting.Count(item, EnchantElement.Earth));
        Assert.Equal(2, Enchanting.Count(item, EnchantElement.Fire));
        Assert.Equal(0, Enchanting.Count(item, EnchantElement.Water));
        Assert.Equal(10, Enchanting.Max(item, settings));
        Assert.Equal("Sword", item.Name);
    }

    [Fact]
    public void Resolve_mixed_elements_updates_total_level_and_suffix()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 3;
        item.ItemProperties[ItemProperty.EnchantFire] = 2;
        Enchanting.ApplySuffix(item);

        var outcome = Enchanting.Resolve(item, EnchantElement.Water, false, settings, new SequenceRandom(50));

        Assert.Equal(EnchantOutcome.Success, outcome);
        Assert.Equal(6, Enchanting.Level(item));
        Assert.Equal(1, Enchanting.Count(item, EnchantElement.Water));
        Assert.Equal("Sword +6", item.Name);
    }

    [Fact]
    public void Resolve_protected_failure_at_seven_with_max_nine_still_allows_reaching_eight()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 7;
        item.ItemProperties[ItemProperty.EnchantMaxLoss] = 1;
        Enchanting.ApplySuffix(item);
        Assert.Equal(9, Enchanting.Max(item, settings));

        var outcome = Enchanting.Resolve(item, EnchantElement.Earth, true, settings, new SequenceRandom(5));
        Assert.Equal(EnchantOutcome.ProtectedFailure, outcome);
        Assert.Equal(8, Enchanting.Max(item, settings));
        Assert.Equal(7, Enchanting.Count(item, EnchantElement.Earth));
        Assert.Equal("Sword +7", item.Name);

        Assert.True(Enchanting.CanEnchant(item, settings, out _));
        outcome = Enchanting.Resolve(item, EnchantElement.Earth, true, settings, new SequenceRandom(50));
        Assert.Equal(EnchantOutcome.Success, outcome);
        Assert.Equal(8, Enchanting.Level(item));
        Assert.Equal("Sword +8", item.Name);

        Assert.False(Enchanting.CanEnchant(item, settings, out _));
    }

    [Fact]
    public void Resolve_throws_when_the_item_cannot_be_enchanted()
    {
        using var fixture = new TestWorldFixture();
        var (item, settings) = Arrange(fixture);
        item.ItemProperties[ItemProperty.EnchantEarth] = 10;

        Assert.Throws<InvalidOperationException>(() =>
            Enchanting.Resolve(item, EnchantElement.Earth, false, settings, new SequenceRandom(50)));
    }
}
