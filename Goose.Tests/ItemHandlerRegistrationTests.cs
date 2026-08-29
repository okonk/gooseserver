
namespace Goose.Tests;

public class ItemHandlerRegistrationTests
{
    private static ItemTemplate Sample() => new ItemTemplate
    {
        ID = 42, Name = "Sword", Description = "A sword", UseType = ItemTemplate.UseTypes.Weapon,
        Slot = ItemTemplate.ItemSlots.OneHanded, Type = ItemTemplate.ItemTypes.OneHandedSword,
        MinLevel = 10, MaxLevel = 50, MinExperience = 100, MaxExperience = 200,
        BaseStats = new AttributeSet { HP = 5, Strength = 3, Haste = 0.25m },
        WeaponDamage = 7, WeaponDelay = 3, Value = 1000, ClassRestrictions = 6,
        GraphicEquipped = 1, GraphicTile = 2, GraphicFile = 3,
        GraphicR = 200, GraphicG = 150, GraphicB = 100, GraphicA = 120,
        IsLore = true, IsBindOnPickup = true, IsBindOnEquip = true, IsEvent = true,
        StackSize = 1, BodyState = 4, SpellEffectID = 9, SpellEffectChance = 5m,
        LearnSpellID = 11, Credits = 12, ScriptParams = "params",
    };

    [Fact]
    public void Copy_constructor_copies_every_property()
    {
        var copy = new ItemTemplate(Sample());

        Assert.Equal(42, copy.ID);
        Assert.Equal("Sword", copy.Name);
        Assert.Equal(ItemTemplate.UseTypes.Weapon, copy.UseType);
        Assert.Equal(ItemTemplate.ItemSlots.OneHanded, copy.Slot);
        Assert.Equal(ItemTemplate.ItemTypes.OneHandedSword, copy.Type);
        Assert.Equal(1000, copy.Value);
        Assert.Equal(6, copy.ClassRestrictions);
        Assert.Equal(120, copy.GraphicA);
        Assert.True(copy.IsLore && copy.IsBindOnPickup && copy.IsBindOnEquip && copy.IsEvent);
        Assert.Equal(4, copy.BodyState);
        Assert.Equal(11, copy.LearnSpellID);
        Assert.Equal("params", copy.ScriptParams);
        Assert.Equal(5, copy.BaseStats.HP);
        Assert.Equal(0.25m, copy.BaseStats.Haste);
    }

    [Fact]
    public void Copy_constructor_gives_the_copy_its_own_stats()
    {
        var basic = Sample();
        var copy = new ItemTemplate(basic);

        copy.BaseStats.HP = 999;

        // A shared AttributeSet would make every dimension clone mutate the base item.
        Assert.Equal(5, basic.BaseStats.HP);
    }

    [Fact]
    public void AddTemplate_registers_a_template_retrievable_by_id()
    {
        var world = new GameWorld(new GooseSettings());
        var template = Sample();

        world.ItemHandler.AddTemplate(template);

        Assert.Same(template, world.ItemHandler.GetTemplate(42));
        Assert.Contains(template, world.ItemHandler.GetTemplates());
    }

    [Fact]
    public void AddTemplate_MissingBaseStats_IsRejected()
    {
        var world = new GameWorld(new GooseSettings());
        world.ItemHandler.AddTemplate(new ItemTemplate { ID = 43, Name = "Bad", BaseStats = null! });

        Assert.Null(world.ItemHandler.GetTemplate(43));
    }

    [Fact]
    public void LoadFromTemplate_InvalidTemplate_ReturnsFalse()
    {
        var item = new Item();

        Assert.False(item.LoadFromTemplate(new ItemTemplate { ID = 45, Name = "", BaseStats = null! }));
    }

    [Fact]
    public void AddTitle_and_AddSurname_register_into_separate_dictionaries()
    {
        var world = new GameWorld(new GooseSettings());
        var title = new ItemModifier { Id = 1, Name = "Legendary" };
        var surname = new ItemModifier { Id = 1, Name = "of Speed" };

        world.ItemHandler.AddTitle(title);
        world.ItemHandler.AddSurname(surname);

        Assert.Equal(1, world.ItemHandler.TitleCount);
        Assert.Equal(1, world.ItemHandler.SurnameCount);
        Assert.Same(title, world.ItemHandler.GetTitle(1));
        Assert.Same(surname, world.ItemHandler.GetSurname(1));
    }
}
