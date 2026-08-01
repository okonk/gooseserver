using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class TableDdlTests
{
    [Fact]
    public void Emits_drop_create_and_columns()
    {
        var ddl = TableDdl.Emit("npc_drops", new[]
        {
            Col.Id("npc_template_id", SqlType.Int).Ref("NPCs"),
            Col.Id("item_template_id", SqlType.Int).Ref("Items"),
            Col.Int("stack"),
            Col.Decimal("droprate"),
        }, indexes: null);

        Assert.Equal(
            "DROP TABLE IF EXISTS npc_drops;\n" +
            "CREATE TABLE npc_drops (\n" +
            "  npc_template_id INT NOT NULL,\n" +
            "  item_template_id INT NOT NULL,\n" +
            "  stack INT NOT NULL,\n" +
            "  droprate DECIMAL(9,4) NOT NULL\n" +
            ");\n", ddl);
    }

    [Fact]
    public void Primary_key_column_omits_not_null()
    {
        var ddl = TableDdl.Emit("classes", new[]
        {
            Col.Id("class_id").PrimaryKey(),
            Col.Text("class_name"),
        }, indexes: null);

        Assert.Contains("  class_id INTEGER PRIMARY KEY,\n", ddl);
    }

    [Fact]
    public void Default_precedes_not_null()
    {
        var ddl = TableDdl.Emit("t", new[] { Col.Int("stack", def: 1) }, indexes: null);

        Assert.Contains("  stack INT DEFAULT 1 NOT NULL\n", ddl);
    }

    [Fact]
    public void Nullable_column_keeps_default_and_drops_not_null()
    {
        var ddl = TableDdl.Emit("quests", new[]
        {
            Col.Int("class_restrictions", SqlType.BigInt, def: 0).Nullable(),
            Col.Int("stack"),
        }, indexes: null);

        Assert.Contains("  class_restrictions BIGINT DEFAULT 0,\n", ddl);
        Assert.DoesNotContain("class_restrictions BIGINT DEFAULT 0 NOT NULL", ddl);
        Assert.Contains("  stack INT NOT NULL\n", ddl);
    }

    [Fact]
    public void Nullable_column_without_default_emits_bare_type()
    {
        var ddl = TableDdl.Emit("t", new[] { Col.Text("note").Required().Nullable() }, indexes: null);

        Assert.Contains("  note TEXT\n", ddl);
    }

    [Fact]
    public void Empty_index_list_emits_no_index()
    {
        // SchemaRegistry normalises null to an empty array, so this is the branch
        // 19 of the 21 tables actually take.
        var ddl = TableDdl.Emit("t", new[] { Col.Int("stack") }, indexes: Array.Empty<string>());

        Assert.DoesNotContain("CREATE INDEX", ddl);
        Assert.EndsWith(");\n", ddl);
    }

    [Fact]
    public void Text_default_is_quoted_by_caller()
    {
        var ddl = TableDdl.Emit("t", new[] { Col.Text("script_path", def: "''") }, indexes: null);

        Assert.Contains("  script_path TEXT DEFAULT '' NOT NULL\n", ddl);
    }

    [Fact]
    public void Bool_default_renders_as_quoted_char()
    {
        var ddl = TableDdl.Emit("t", new[] { Col.Bool("lore", def: false) }, indexes: null);

        Assert.Contains("  lore CHAR(1) DEFAULT '0' NOT NULL\n", ddl);
    }

    [Fact]
    public void Index_follows_create_table()
    {
        var ddl = TableDdl.Emit("npc_vendor_items", new[]
        {
            Col.Id("npc_template_id", SqlType.Int),
        }, indexes: new[] { "npc_template_id" });

        Assert.EndsWith(
            ");\n" +
            "CREATE INDEX npc_vendor_items_npc_template_id_idx " +
            "ON npc_vendor_items(npc_template_id);\n", ddl);
    }
}
