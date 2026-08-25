using Goose.IntegrationTests.Fixtures;

namespace Goose.IntegrationTests;

public class DimensionDropTests
{
    private static GlobalScriptFixture Run()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);

        var sword = fixture.AddBaseItemTemplate(50, "Sword", ItemTemplate.UseTypes.Weapon);
        var potion = fixture.AddBaseItemTemplate(60, "Potion", ItemTemplate.UseTypes.OneTime);

        var npc = new NPCTemplate { NPCTemplateID = 162, Name = "Shadow Dog", Level = 40 };
        npc.BaseStats = new AttributeSet { HP = 3704 };
        npc.Drops = new List<NPCDropInfo>
        {
            new NPCDropInfo { ItemTemplate = sword, DropRate = 0.1m, Stack = 1 },
            new NPCDropInfo { ItemTemplate = potion, DropRate = 0.5m, Stack = 3 },
        };
        fixture.World.NPCHandler.AddTemplate(npc);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Dimension_npcs_drop_dimension_equipment()
    {
        using var fixture = Run();
        var drops = fixture.World.NPCHandler.GetNPCTemplate(162 + 100000 * 4).Drops;

        var sword = drops.Single(d => d.ItemTemplate.Name.EndsWith("Sword"));
        Assert.Equal(50 + 100000 * 4, sword.ItemTemplate.ID);
        Assert.Equal(0.1m, sword.DropRate);   // rate and stack are carried across unchanged
        Assert.Equal(1, sword.Stack);
    }

    [Fact]
    public void Consumable_drops_stay_at_dimension_zero()
    {
        using var fixture = Run();
        var drops = fixture.World.NPCHandler.GetNPCTemplate(100162).Drops;

        Assert.Equal(60, drops.Single(d => d.ItemTemplate.Name == "Potion").ItemTemplate.ID);
    }

    [Fact]
    public void The_base_drop_table_is_left_alone()
    {
        using var fixture = Run();
        var drops = fixture.World.NPCHandler.GetNPCTemplate(162).Drops;

        // NPCTemplate's copy constructor shares NPCDropInfo instances (NPCTemplate.cs:251),
        // so repointing must allocate new ones or every dimension rewrites dimension 0.
        Assert.Equal(50, drops.Single(d => d.ItemTemplate.Name == "Sword").ItemTemplate.ID);
        Assert.Equal(60, drops.Single(d => d.ItemTemplate.Name == "Potion").ItemTemplate.ID);
    }

    [Fact]
    public void Each_dimension_gets_its_own_drop_entries()
    {
        using var fixture = Run();

        var dim1 = fixture.World.NPCHandler.GetNPCTemplate(100162).Drops
            .Single(d => d.ItemTemplate.Name.EndsWith("Sword"));
        var dim2 = fixture.World.NPCHandler.GetNPCTemplate(200162).Drops
            .Single(d => d.ItemTemplate.Name.EndsWith("Sword"));

        Assert.NotSame(dim1, dim2);
        Assert.Equal(100050, dim1.ItemTemplate.ID);
        Assert.Equal(200050, dim2.ItemTemplate.ID);
    }
}
