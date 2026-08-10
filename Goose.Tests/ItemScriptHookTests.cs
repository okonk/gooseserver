using Goose.Scripting;
using Goose.Tests.Collections;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class ItemScriptHookTests : IDisposable
{
    private readonly GooseSettings previousSettings = GameWorld.Settings;

    public ItemScriptHookTests()
    {
        // The native surname roll is a RollChance(ItemSurnameChancePercent) call
        // (ItemHandler.cs). The shipped GooseSettings.json puts that at 0.5, which would
        // make Returning_false_leaves_the_native_rolls_running a coin flip, so pin the
        // chances: surnames always roll, titles never do.
        GameWorld.Settings = new GooseSettings
        {
            ItemSurnameChancePercent = 1.0,
            ItemTitleChancePercent = 0.0,
        };
    }

    public void Dispose()
    {
        GameWorld.Settings = previousSettings;
    }

    private sealed class SpyScript : BaseItemScript
    {
        public bool Suppress;
        public int RollCalls;

        public override bool OnRollModifiersEvent(Item item, GameWorld world)
        {
            this.RollCalls++;
            return this.Suppress;
        }
    }

    private static (GameWorld World, Item Item, SpyScript Spy) Arrange()
    {
        var world = new GameWorld(null);
        var spy = new SpyScript();
        var template = new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "",
            UseType = ItemTemplate.UseTypes.Weapon, Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
            Script = ScriptStub.For<IItemScript>(spy),
        };
        world.ItemHandler.AddTemplate(template);

        var item = new Item();
        item.LoadFromTemplate(template);
        return (world, item, spy);
    }

    [Fact]
    public void Roll_hook_runs_before_the_use_type_filter()
    {
        var (world, item, spy) = Arrange();
        // A Scroll would be filtered out by RollTitleAndSurname's early return - the hook
        // must still see it, because dimension tomes need CanPickup and the upgrade rule.
        item.Template.UseType = ItemTemplate.UseTypes.Scroll;

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.Equal(1, spy.RollCalls);
    }

    [Fact]
    public void Returning_true_suppresses_the_native_rolls()
    {
        var (world, item, spy) = Arrange();
        spy.Suppress = true;
        world.ItemHandler.AddSurname(new ItemModifier
        {
            Id = 1, Name = "of the Bear", Chance = 1.0,
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.Weapon,
        });

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.False(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal("Sword", item.Name);
    }

    [Fact]
    public void Returning_false_leaves_the_native_rolls_running()
    {
        var (world, item, spy) = Arrange();
        spy.Suppress = false;
        world.ItemHandler.AddSurname(new ItemModifier
        {
            Id = 1, Name = "of the Bear", Chance = 1.0,
            Slot = ItemTemplate.ItemSlots.Misc, UseType = ItemTemplate.UseTypes.Weapon,
        });

        world.ItemHandler.RollTitleAndSurname(item, world);

        Assert.True(item.HasProperty(ItemProperty.SurnameId));
        Assert.Equal("Sword of the Bear", item.Name);
    }

    [Fact]
    public void An_item_with_no_script_rolls_natively()
    {
        var world = new GameWorld(null);
        var template = new ItemTemplate
        {
            ID = 1, Name = "Sword", Description = "",
            UseType = ItemTemplate.UseTypes.Weapon, Slot = ItemTemplate.ItemSlots.OneHanded,
            BaseStats = new AttributeSet(),
        };
        var item = new Item();
        item.LoadFromTemplate(template);

        var exception = Record.Exception(() => world.ItemHandler.RollTitleAndSurname(item, world));

        Assert.Null(exception);
    }

    [Fact]
    public void CanPickup_defaults_to_allowing()
    {
        var (world, item, _) = Arrange();
        Assert.Null(new BaseItemScript().CanPickup(new Player(0), item, world));
    }
}
