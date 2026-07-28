using CsvToSql.Core;

namespace Goose.Tests;

public class CsvToSqlBaselineTests
{
    private static string FixtureDir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact(Skip = "Run manually to re-record the baseline")]
    public void RecordBaseline()
    {
        using var fs = File.OpenRead(Path.Combine(FixtureDir, "aspereta-data.xlsx"));
        var sql = CsvToSqlConverter.ConvertWorkbook(fs);

        // Written to the source tree, not the build output, so it can be committed.
        var target = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "baseline.sql");
        File.WriteAllText(Path.GetFullPath(target), sql);
    }

}
