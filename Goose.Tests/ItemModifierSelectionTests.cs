namespace Goose.Tests;

public class ItemModifierSelectionTests
{
    private sealed class SequenceRandom(params int[] values) : Random
    {
        private readonly Queue<int> values = new(values);

        public bool IsEmpty => values.Count == 0;

        public override int Next(int maxValue)
        {
            var value = values.Dequeue();
            Assert.InRange(value, 0, maxValue - 1);
            return value;
        }

        public override int Next(int minValue, int maxValue)
        {
            var value = values.Dequeue();
            Assert.InRange(value, minValue, maxValue - 1);
            return value;
        }
    }

    private static ItemModifier Modifier(int id, string name, double chance,
        ItemTemplate.UseTypes useType = ItemTemplate.UseTypes.Weapon)
    {
        return new ItemModifier
        {
            Id = id,
            Name = name,
            Chance = chance,
            UseType = useType,
            Slot = ItemTemplate.ItemSlots.Misc,
        };
    }

    private static (GameWorld World, ItemTemplate Template) Arrange(
        Random random, params ItemModifier[] modifiers)
    {
        var settings = new GooseSettings
        {
            ItemSurnameChancePercent = 1.0,
            ItemTitleChancePercent = 0.0,
        };
        var world = new GameWorld(settings, null, random);
        var template = new ItemTemplate
        {
            ID = 1,
            Name = "Sword",
            Description = "",
            UseType = ItemTemplate.UseTypes.Weapon,
            Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
        };

        foreach (var modifier in modifiers)
            world.ItemHandler.AddSurname(modifier);

        return (world, template);
    }

    private static Item Roll(GameWorld world, ItemTemplate template)
    {
        var item = new Item();
        item.LoadFromTemplate(template);
        world.ItemHandler.RollTitleAndSurname(item, world);
        return item;
    }

    [Fact]
    public void Partial_chance_modifier_can_leave_the_candidate_pool_empty()
    {
        var random = new SequenceRandom(
            1, 1_000_000_000, 1,
            1, 1, 0, 1);
        var (world, template) = Arrange(random, Modifier(1, "Sometimes", 0.5));

        var missed = Roll(world, template);
        var selected = Roll(world, template);

        Assert.False(missed.HasProperty(ItemProperty.SurnameId));
        Assert.True(selected.HasProperty(ItemProperty.SurnameId));
        Assert.Equal(1, selected.GetProperty<int>(ItemProperty.SurnameId));
        Assert.True(random.IsEmpty);
    }

    [Fact]
    public void Guaranteed_candidate_does_not_block_other_successful_candidates()
    {
        var random = new SequenceRandom(
            1, 1, 1, 1, 1,
            1, 1, 1_000_000_000, 0, 1);
        var (world, template) = Arrange(random,
            Modifier(1, "Always", 1.0),
            Modifier(2, "Sometimes", 0.5));

        var partialWinner = Roll(world, template);
        var guaranteedWinner = Roll(world, template);

        Assert.True(partialWinner.HasProperty(ItemProperty.SurnameId));
        Assert.Equal(2, partialWinner.GetProperty<int>(ItemProperty.SurnameId));
        Assert.True(guaranteedWinner.HasProperty(ItemProperty.SurnameId));
        Assert.Equal(1, guaranteedWinner.GetProperty<int>(ItemProperty.SurnameId));
        Assert.True(random.IsEmpty);
    }

    [Fact]
    public void Inapplicable_modifiers_do_not_enter_the_candidate_pool()
    {
        var random = new SequenceRandom(1, 1, 0, 1);
        var (world, template) = Arrange(random,
            Modifier(1, "Weapon", 1.0),
            Modifier(2, "Armor", 1.0, ItemTemplate.UseTypes.Armor));

        var item = Roll(world, template);

        Assert.True(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal(1, item.GetProperty<int>(ItemProperty.SurnameId));
        Assert.True(random.IsEmpty);
    }
}
