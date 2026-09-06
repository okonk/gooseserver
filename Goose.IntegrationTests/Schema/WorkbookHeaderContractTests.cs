using ClosedXML.Excel;
using CsvToSql.Core.Schema;

namespace Goose.IntegrationTests.Schema;

public class WorkbookHeaderContractTests
{
    [Theory]
    [InlineData("Maps")]
    [InlineData("NPCs")]
    [InlineData("NPC Spawns")]
    [InlineData("Warptiles")]
    public void Every_descriptor_header_matches_the_workbook_row_one(string sheet)
    {
        var table = SchemaRegistry.Tables.Single(t => t.Sheet == sheet);
        using var workbook = new XLWorkbook(FixturePath());
        var worksheet = workbook.Worksheet(sheet);

        for (var i = 0; i < table.Columns.Count; i++)
        {
            var column = table.Columns[i];
            Assert.False(string.IsNullOrWhiteSpace(column.Header),
                $"{sheet} column {i + 1} ({column.Name}) has no verified header.");
            Assert.Equal(worksheet.Cell(1, i + 1).GetValue<string>(), column.Header,
                StringComparer.Ordinal);
        }
    }

    private static string FixturePath() =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "aspereta-data.xlsx");
}
