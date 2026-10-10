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
        Assert.Equal("gold", fields[^3]);
        // Clone template = base + additive bake (Items.csx:135): Haste 0.02 + 0.02*1.5 = 0.05,
        // MeleeDamage (int)(10*3*0.5) = 15, HPStaticRegen 100 + 150 = 250.
        // of Speed adds 0.04 * 3 * 0.5 = 0.06 haste (AttributeSet.java:428) -> 0.11.
        Assert.Equal("1100,0,0,150000,0,0,0,250", fields[^2]);
        Assert.Equal("0", fields[^1]);

        var regen = ItemOfDimension(fixture, dim: 3);
        fixture.World.ItemHandler.GetSurname(900000)!.ApplyStats(regen, fixture.World);

        fields = P.ItemSlot(regen, fixture.World, 1, 1).Split('|');
        Assert.Equal("gold", fields[^3]);
        // of Vita Regen adds 0.015 * 3 * 0.5 = 0.0225 percent and (int)(1500 * 3 * 0.5) = 2250
        // flat (AttributeSet.java:430,431) on top of the clone template above.
        Assert.Equal("500,0,0,150000,0,0,225,2500", fields[^2]);
    }

    [Fact]
    public void Vendor_stock_tooltip_labels_ordinary_dimension_gear_as_gold()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon, t =>
        {
            t.MinLevel = 50;
            t.BaseStats.Haste = 0.02;
            t.BaseStats.HPStaticRegen = 100;
        });

        var merchant = new NPCTemplate
        {
            NPCTemplateID = 60, Name = "Merchant", Level = 1, ClassID = 3,
            NPCType = NPCTemplate.Types.Vendor,
            CanBeKilled = false, CanMove = false,
            AttackSpeed = 1.0, MoveSpeed = 1.0,
            AlliesString = "", Allies = new List<NPCTemplate>(), Drops = new List<NPCDropInfo>(),
            BaseStats = new AttributeSet { HP = 100 },
        };
        merchant.VendorItems = new NPCVendorSlot[fixture.Settings.VendorSlotSize + 1];
        merchant.VendorItems[1] = new NPCVendorSlot
        {
            Slot = 1, ItemTemplate = fixture.World.ItemHandler.GetTemplate(50)!,
            Stack = 1, CanSeeStats = true,
        };
        fixture.World.NPCHandler.AddTemplate(merchant);
        fixture.World.NPCHandler.SpawnNPC(fixture.World, 1, 20, 20, merchant, shouldRespawn: false);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var vendor = fixture.World.MapHandler.GetMap(1 + 100000 * 3)!.NPCs
            .Single(n => n.NPCTemplateID == 60 + 100000 * 3);
        var stock = vendor.VendorItems![1];
        Assert.Equal(50 + 100000 * 3, stock.ItemTemplate.ID);

        var fields = P.VendorItemSlot(stock.ItemTemplate, fixture.World, vendor, 1, 1).Split('|');
        Assert.Equal("gold", fields[^3]);
    }

    private static Item ItemOfDimension(GlobalScriptFixture fixture, int dim)
    {
        var item = new Item();
        item.LoadFromTemplate(fixture.World.ItemHandler.GetTemplate(50 + 100000 * dim)!);
        return item;
    }
}
