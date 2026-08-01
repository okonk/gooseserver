using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class CompositeTests
{
    [Fact]
    public void Graphic_names_its_two_columns()
    {
        var g = Composite.Graphic("graphic_tile", file: "graphic_file");

        Assert.Equal(CompositeKind.Graphic, g.Kind);
        Assert.Equal(new[] { "graphic_tile", "graphic_file" }, g.Columns);
    }

    [Fact]
    public void Rgba_names_four_columns_in_order()
    {
        var c = Composite.Rgba("body_r", "body_g", "body_b", "body_a");

        Assert.Equal(new[] { "body_r", "body_g", "body_b", "body_a" }, c.Columns);
    }

    [Fact]
    public void Bitmask_records_source_sheet()
    {
        var b = Composite.Bitmask("class_restrictions", from: "Classes");

        Assert.Equal("Classes", b.SourceSheet);
        Assert.Equal(new[] { "class_restrictions" }, b.Columns);
    }

    [Fact]
    public void IdList_records_target_sheet()
    {
        var l = Composite.IdList("quest_ids", refSheet: "Quests");

        Assert.Equal("Quests", l.SourceSheet);
    }

    [Fact]
    public void EquipSlots_covers_one_column()
    {
        var e = Composite.EquipSlots("equipped_items");

        Assert.Equal(CompositeKind.EquipSlots, e.Kind);
        Assert.Equal(new[] { "equipped_items" }, e.Columns);
    }
}
