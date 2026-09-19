using Goose.IntegrationTests.Fixtures;
using Goose.Testing;

namespace Goose.IntegrationTests;

public class DimensionTooltipStatsTests
{
    private static GlobalScriptFixture Run()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.MinLevel = 50;              // tier 0.5
            t.BaseStats.Haste = 0.02;
            t.BaseStats.HPStaticRegen = 100;
        });
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Dimension_item_reports_the_suffix_and_the_template_together()
    {
        using var fixture = Run();

        var speed = ItemOfDimension(fixture, dim: 3);
        fixture.World.ItemHandler.GetSurname(900005)!.ApplyStats(speed, fixture.World);

        var fields = P.ItemSlot(speed, fixture.World, 1, 1).Split('|');
        Assert.Equal("spirit", fields[^2]);
        // Clone template = base + additive bake (Items.csx:135): Haste 0.02 + 0.02*1.5 = 0.05,
        // MeleeDamage (int)(10*3*0.5) = 15, HPStaticRegen 100 + 150 = 250.
        // of Speed adds 0.04 * 3 * 0.5 = 0.06 haste (AttributeSet.java:428) -> 0.11.
        Assert.Equal("1100,0,0,150000,0,0,0,250", fields[^1]);

        var regen = ItemOfDimension(fixture, dim: 3);
        fixture.World.ItemHandler.GetSurname(900000)!.ApplyStats(regen, fixture.World);

        fields = P.ItemSlot(regen, fixture.World, 1, 1).Split('|');
        Assert.Equal("spirit", fields[^2]);
        // of Vita Regen adds 0.015 * 3 * 0.5 = 0.0225 percent and (int)(1500 * 3 * 0.5) = 2250
        // flat (AttributeSet.java:430,431) on top of the clone template above.
        Assert.Equal("500,0,0,150000,0,0,225,2500", fields[^1]);
    }

    private static Item ItemOfDimension(GlobalScriptFixture fixture, int dim)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(50 + 100000 * dim)!);
        return item;
    }
}
