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

    [Fact]
    public void Output_matches_recorded_baseline()
    {
        using var fs = File.OpenRead(Path.Combine(FixtureDir, "aspereta-data.xlsx"));
        var actual = CsvToSqlConverter.ConvertWorkbook(fs);
        var expected = File.ReadAllText(Path.Combine(FixtureDir, "baseline.sql"));

        AssertSameLines(Normalise(expected), Normalise(actual));
    }

    /// <summary>Compares line by line rather than as one 900 KB string. Every generated line is
    /// a whole INSERT or VALUES clause, so the first mismatch names the table and shows the bad
    /// value — where Assert.Equal would report only a character offset.</summary>
    private static void AssertSameLines(string expected, string actual)
    {
        var e = expected.Split('\n');
        var a = actual.Split('\n');

        for (int i = 0; i < Math.Min(e.Length, a.Length); i++)
            Assert.True(e[i] == a[i],
                $"line {i + 1} differs:\n  expected: {e[i]}\n  actual:   {a[i]}");

        Assert.Equal(e.Length, a.Length);
    }

    private static string Normalise(string sql) => sql.Replace("\r\n", "\n");
}
