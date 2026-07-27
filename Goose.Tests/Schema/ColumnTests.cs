using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class ColumnTests
{
    [Fact]
    public void Int_carries_type_and_default()
    {
        var c = Col.Int("stack", SqlType.Int, def: 1);

        Assert.Equal("stack", c.Name);
        Assert.Equal(ColumnKind.Int, c.Kind);
        Assert.Equal("1", c.Default);
        Assert.False(c.IsRequired);
    }

    [Fact]
    public void Required_column_has_no_default()
    {
        var c = Col.Text("item_name", SqlType.Text).Required();

        Assert.True(c.IsRequired);
        Assert.Null(c.Default);
    }

    [Fact]
    public void PrimaryKey_implies_required_and_integer()
    {
        var c = Col.Id("item_template_id").PrimaryKey();

        Assert.True(c.IsPrimaryKey);
        Assert.True(c.IsRequired);
        Assert.Equal(SqlType.Integer, c.Type);
    }

    [Fact]
    public void Ref_records_target_sheet()
    {
        var c = Col.Id("npc_template_id").Ref("NPCs");

        Assert.Equal("NPCs", c.RefSheet);
    }

    [Fact]
    public void Enum_exposes_member_names_in_declaration_order()
    {
        var c = Col.Enum<SampleKind>("kind");

        Assert.Equal(ColumnKind.Enum, c.Kind);
        Assert.Equal(new[] { "First", "Second" }, c.EnumNames);
    }

    private enum SampleKind { First = 0, Second = 1 }
}
