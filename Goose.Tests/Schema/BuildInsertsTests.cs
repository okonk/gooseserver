using ClosedXML.Excel;
using CsvToSql;
using CsvToSql.Core.Schema;

namespace Goose.Tests.Schema;

public class BuildInsertsTests
{
    private class TestConverter : CsvToSqlBase
    {
        public override Column[] GetColumnDescriptors() => new[]
        {
            Col.Id("npc_id", SqlType.Integer).PrimaryKey(),
            Col.Text("npc_name"),
        };
    }

    [Fact]
    public void Emits_one_insert_per_row_with_schema_data()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("NPCs");
        sheet.Cell(1, 1).Value = "ID";
        sheet.Cell(1, 2).Value = "name";
        sheet.Cell(2, 1).Value = 1;
        sheet.Cell(2, 2).Value = "Rat";

        var converter = new TestConverter();
        var sql = converter.BuildInserts(sheet, "npc_templates", converter.GetColumnDescriptors());

        Assert.Contains("INSERT INTO npc_templates (npc_id, npc_name)\nVALUES (1, 'Rat');", sql);
    }

    [Fact]
    public void Row_with_content_only_past_schema_width_emits_no_insert()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Quest Reqs");
        sheet.Cell(1, 1).Value = "ID";
        sheet.Cell(1, 2).Value = "name";
        sheet.Cell(2, 1).Value = 1;
        sheet.Cell(2, 2).Value = "Rat";
        sheet.Cell(3, 4).FormulaA1 = "VLOOKUP(B3,Quests!A:C,2,0)";

        var converter = new TestConverter();
        var sql = converter.BuildInserts(sheet, "quest_requirements",
                                         converter.GetColumnDescriptors());

        Assert.DoesNotContain("quest_requirements ()", sql);
        Assert.DoesNotContain("VALUES ();", sql);
        Assert.Contains("VALUES (1, 'Rat');", sql);
    }
}
