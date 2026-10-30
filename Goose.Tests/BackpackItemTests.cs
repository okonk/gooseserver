using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class BackpackItemTests
{
    private static (TestWorldFixture World, ItemTemplate Template, Item Item) Setup(
        Action<ItemTemplate>? configureTemplate = null, Action<Item>? configureItem = null)
    {
        var world = new TestWorldFixture();
        var template = world.AddBaseItemTemplate(100, "Backpack", ItemTemplate.UseTypes.Armor, configureTemplate);
        var item = new Item();
        item.LoadFromTemplate(template);
        configureItem?.Invoke(item);
        return (world, template, item);
    }

    [Fact]
    public void TemplateOnly_ReadsSlotsAndDefaultColumns()
    {
        var (_, _, item) = Setup(configureTemplate: t => t.Properties["backpackSlots"] = 10);

        Assert.Equal(10, item.BackpackSlots);
        Assert.Equal(5, item.BackpackColumns);
    }

    [Fact]
    public void ItemOverrideWinsOverTemplate()
    {
        var (_, _, item) = Setup(
            configureTemplate: t => t.Properties["backpackSlots"] = 10,
            configureItem: i => i.ItemProperties[ItemProperty.BackpackSlots] = 30);

        Assert.Equal(30, item.BackpackSlots);
    }

    [Fact]
    public void SetBackpackSize_RoundTripsThroughJson()
    {
        var (_, template, item) = Setup();

        item.SetBackpackSize(20, 3);

        var json = JsonHelper.Serialize(item);
        var reloaded = JsonHelper.Deserialize<Item>(json)!;
        reloaded.Template = template;

        Assert.Equal(20, reloaded.BackpackSlots);
        Assert.Equal(3, reloaded.BackpackColumns);
    }

    [Fact]
    public void RefreshBackpackDescription_Twice_AppendsOnce()
    {
        var (_, _, item) = Setup();

        item.SetBackpackSize(20, 5);
        item.RefreshBackpackDescription();

        Assert.Equal("A Backpack Holds 20 items.", item.Description);
    }

    [Fact]
    public void RefreshBackpackDescription_EmptyTemplateDescription_HasNoLeadingSpace()
    {
        var (_, _, item) = Setup(configureTemplate: t => t.Description = "");

        item.SetBackpackSize(20, 5);

        Assert.Equal("Holds 20 items.", item.Description);
    }

    [Fact]
    public void NoPropertyAndNoOverride_DefaultsToZero()
    {
        var (_, _, item) = Setup();

        Assert.Equal(0, item.BackpackSlots);
        Assert.Equal(5, item.BackpackColumns);
    }
}
