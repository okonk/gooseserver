using ClosedXML.Excel;
using CsvToSql.Core;
using CsvToSql.Core.Schema;

namespace Goose.IntegrationTests.Schema;

public class EditorOnlySheetTests
{
    [Fact]
    public void Converter_does_not_require_editor_only_worksheets()
    {
        using var workbook = new XLWorkbook(FixturePath());
        foreach (var t in SchemaRegistry.EditorOnlyTables)
            Assert.False(workbook.Worksheets.Contains(t.Sheet));

        var sql = Convert(workbook);

        foreach (var t in SchemaRegistry.EditorOnlyTables)
            Assert.DoesNotContain(t.Table, sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Converter_ignores_editor_only_worksheets_when_present()
    {
        using var without = new XLWorkbook(FixturePath());
        var expected = Convert(without);

        using var with = new XLWorkbook(FixturePath());
        foreach (var t in SchemaRegistry.EditorOnlyTables)
        {
            var sheet = with.Worksheets.Add(t.Sheet);
            for (var i = 0; i < t.Columns.Count; i++)
                sheet.Cell(1, i + 1).Value = t.Columns[i].Name;
            sheet.Cell(2, 1).Value = "not a number";
        }

        Assert.Equal(expected, Convert(with));
    }

    private static string Convert(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return CsvToSqlConverter.ConvertWorkbook(stream);
    }

    private static string FixturePath() =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "aspereta-data.xlsx");
}
