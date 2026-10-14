using ClosedXML.Excel;
using CsvToSql;
using CsvToSql.Core;
using CsvToSql.Core.Schema;

namespace Goose.IntegrationTests.Schema;

public class OptionalSheetTests
{
    private const string Sheet = "Community Goals";
    private const string Table = "community_goals";

    [Fact]
    public void Optional_sheet_absent_emits_ddl_without_inserts()
    {
        using var workbook = new XLWorkbook(FixturePath());
        Assert.False(workbook.Worksheets.Contains(Sheet));

        var sql = Convert(Schemas(optional: true));

        Assert.Contains($"CREATE TABLE {Table} (", sql, StringComparison.Ordinal);
        Assert.DoesNotContain($"INSERT INTO {Table}", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Required_sheet_absent_still_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Convert(Schemas(optional: false)));

        Assert.Contains(Sheet, ex.Message, StringComparison.Ordinal);
    }

    private static IReadOnlyList<TableSchema> Schemas(bool optional)
    {
        var schemas = new List<TableSchema>(SchemaRegistry.Tables);
        schemas.Add(new TableSchema(Sheet, Table, new ItemsCsvToSql(), optional: optional));
        return schemas;
    }

    private static string Convert(IReadOnlyList<TableSchema> schemas)
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook(FixturePath()))
            workbook.SaveAs(stream);
        stream.Position = 0;
        return CsvToSqlConverter.ConvertWorkbook(stream, schemas);
    }

    private static string FixturePath() =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "aspereta-data.xlsx");
}
