using Goose;
using Goose.Testing;
using Xunit;

namespace Goose.Tests;

public class EnchantingNameTests
{
    [Fact]
    public void SetModifier_on_enchanted_item_keeps_suffix_last()
    {
        using var fixture = new TestWorldFixture();
        var template = fixture.AddBaseItemTemplate(1, "Sword", ItemTemplate.UseTypes.Weapon);
        var item = new Item();
        item.LoadFromTemplate(template);
        item.ItemProperties[ItemProperty.SurnameId] = 20;
        item.ItemProperties[ItemProperty.EnchantEarth] = 3;
        item.Name = "Sword of the Bear +3";

        fixture.World.ItemHandler.AddTitle(new ItemModifier { Id = 10, Name = "Ancient" });
        fixture.World.ItemHandler.AddSurname(new ItemModifier { Id = 20, Name = "of the Bear" });
        fixture.World.ItemHandler.AddSurname(new ItemModifier { Id = 21, Name = "of the Wolf" });

        fixture.World.ItemHandler.SetModifier(item, ItemProperty.TitleId, fixture.World.ItemHandler.GetTitle(10), fixture.World);
        Assert.Equal("Ancient Sword of the Bear +3", item.Name);

        fixture.World.ItemHandler.SetModifier(item, ItemProperty.SurnameId, fixture.World.ItemHandler.GetSurname(21), fixture.World);
        Assert.Equal("Ancient Sword of the Wolf +3", item.Name);
    }
}
