using CsvToSql.Core.Schema;
using Goose.Tools.SchemaGen;

namespace Tools.Tests;

public class SchemaModelTests
{
    [Fact]
    public void Includes_every_registered_sheet()
    {
        var model = SchemaModel.Build();

        Assert.Equal(21, model.Sheets.Count);
    }

    [Fact]
    public void Items_sheet_carries_table_and_primary_key()
    {
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");

        Assert.Equal("item_templates", items.Table);
        Assert.Equal("item_template_id", items.Columns[0].Name);
        Assert.True(items.Columns[0].Pk);
        Assert.True(items.Columns[0].Required);
        Assert.Equal("INTEGER", items.Columns[0].Sql);
    }

    [Fact]
    public void Enum_columns_expose_member_names()
    {
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");
        var usetype = items.Columns.Single(c => c.Name == "item_usetype");

        Assert.Equal("Enum", usetype.Kind);
        Assert.NotNull(usetype.EnumNames);
        Assert.NotEmpty(usetype.EnumNames!);
    }

    [Fact]
    public void Non_enum_columns_omit_the_member_names()
    {
        // Null rather than empty: Task 2 omits null properties, so a non-enum column must not
        // carry an enumNames key at all. Column.EnumNames itself returns an empty array here.
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");

        Assert.Null(items.Columns.Single(c => c.Name == "item_description").EnumNames);
    }

    [Fact]
    public void Foreign_keys_expose_target_sheet()
    {
        var drops = SchemaModel.Build().Sheets.Single(s => s.Sheet == "NPC Drops");

        Assert.Equal("NPCs", drops.Columns.Single(c => c.Name == "npc_template_id").Ref);
        Assert.Equal("Items", drops.Columns.Single(c => c.Name == "item_template_id").Ref);
    }

    [Fact]
    public void Optional_columns_report_their_default()
    {
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");
        var desc = items.Columns.Single(c => c.Name == "item_description");

        Assert.False(desc.Required);
        Assert.Equal("''", desc.Default);
    }

    [Fact]
    public void Mandatory_graphic_columns_are_required()
    {
        var model = SchemaModel.Build();

        // sqlTemplate.sql:35 and :204 — NOT NULL with no DEFAULT.
        Assert.True(model.Sheets.Single(s => s.Sheet == "Items")
                         .Columns.Single(c => c.Name == "graphic_tile").Required);
        Assert.True(model.Sheets.Single(s => s.Sheet == "Spells")
                         .Columns.Single(c => c.Name == "spellbook_graphic").Required);
    }

    [Fact]
    public void Composites_are_reported_with_their_columns()
    {
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");
        var graphic = items.Composites.Single(c => c.Kind == "Graphic");

        Assert.Equal(new[] { "graphic_tile", "graphic_file" }, graphic.Columns);
    }

    [Fact]
    public void Composites_carry_their_source_sheet()
    {
        // ItemsCsvToSql.cs:75 — Composite.Bitmask("class_restrictions", from: "Classes").
        var items = SchemaModel.Build().Sheets.Single(s => s.Sheet == "Items");
        var bitmask = items.Composites.Single(c => c.Kind == "Bitmask");

        Assert.Equal(new[] { "class_restrictions" }, bitmask.Columns);
        Assert.Equal("Classes", bitmask.Source);
    }

    [Fact]
    public void Sheets_preserve_registry_order()
    {
        // Declaration order is emission order, and column index i maps to worksheet cell i + 1
        // (see Column's doc comment), so neither list may be reordered.
        var model = SchemaModel.Build();

        Assert.Equal(SchemaRegistry.Tables.Select(t => t.Sheet), model.Sheets.Select(s => s.Sheet));
        Assert.Equal(
            SchemaRegistry.Tables.Single(t => t.Sheet == "Items").Columns.Select(c => c.Name),
            model.Sheets.Single(s => s.Sheet == "Items").Columns.Select(c => c.Name));
    }

    [Fact]
    public void Indexes_are_reported()
    {
        var vendor = SchemaModel.Build().Sheets.Single(s => s.Sheet == "NPC Vendor Items");

        Assert.Equal(new[] { "npc_template_id" }, vendor.Indexes);
    }
}
