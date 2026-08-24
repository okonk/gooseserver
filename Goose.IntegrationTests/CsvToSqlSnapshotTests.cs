using Goose.IntegrationTests.Fixtures;

namespace Goose.IntegrationTests;

/// <summary>Semantic snapshot of the generator: executes the freshly generated script into a
/// SQLite database and compares what SQLite ends up holding — every table, index, column
/// definition and row — against a checked-in rendering of the same thing.
///
/// THIS REPLACES CsvToSqlEquivalenceTests, which compared the generator against a script recorded
/// before the descriptor rewrite. That was a migration gate: it proved the rewrite changed nothing,
/// and by construction it could only ever say "you have changed the schema", never "the change is
/// the one you meant". The first intentional divergence (npc_templates.body_state defaulting to the
/// unarmed 3) was therefore a wall to knock down rather than a review, which is exactly what its own
/// doc said to expect. The successor it named is this file.
///
/// WHAT IT STILL CATCHES is everything the gate did, because the comparison machinery below is the
/// same: a missing or extra table, index, column or row, a changed type, default or nullability, and
/// any changed cell value that survives SQLite's type affinity. Column ORDER is deliberately
/// tolerated (definitions are sorted, rows are selected by sorted column name) — two tables
/// legitimately declare their columns in a different order than the hand-written schema did, and
/// order is not something the importer or the server can observe.
///
/// WHAT CHANGES is who the authority is. The snapshot is REGENERABLE, so an intentional schema
/// change is a reviewable diff in this repository rather than a test to delete:
///
///     GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.IntegrationTests --filter FullyQualifiedName~CsvToSqlSnapshot
///
/// then read the diff and commit it if it is what you meant. Regenerating without reading the diff
/// is the one way to make this test worthless, and no flag can stop that — the review is the test.
///
/// THE INPUT IS FIXED: Fixtures/aspereta-data.xlsx, a real workbook. The snapshot is therefore a
/// statement about the generator, not about today's game data.</summary>
public class CsvToSqlSnapshotTests(CsvToSqlSnapshotFixture fixture) : IClassFixture<CsvToSqlSnapshotFixture>
{
    private const string SnapshotFile = "generated.snapshot";

    private static string FixtureDir => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact]
    public void Generated_script_matches_the_recorded_snapshot()
    {
        var actual = fixture.GenerateSnapshot();

        if (Environment.GetEnvironmentVariable("GOOSE_UPDATE_SNAPSHOT") == "1")
        {
            fixture.RegenerateSnapshot(actual);
            // Not a silent pass: a run that rewrote the fixture has verified nothing, and saying so
            // is what keeps GOOSE_UPDATE_SNAPSHOT out of anyone's habitual test command.
            Assert.Fail($"Rewrote {fixture.SourceSnapshotPath}. Review the diff, commit it if it is what you meant, " +
                        "then run the tests again without GOOSE_UPDATE_SNAPSHOT.");
        }

        var path = Path.Combine(FixtureDir, SnapshotFile);
        Assert.True(File.Exists(path),
            $"No snapshot at {path}. Record one with GOOSE_UPDATE_SNAPSHOT=1 (see this class's doc).");

        var expected = fixture.ReadExpectedSnapshot();
        if (expected == actual) return;

        // Reported as ONE line of difference rather than as two megabytes of string inequality:
        // xUnit's own diff on a million-character string is unreadable, and the first divergence is
        // what a reader needs — the schema is rendered before any row of any table, so a DDL change
        // always surfaces ahead of the rows it moved.
        Assert.Fail(Describe(Lines(expected), Lines(actual), fixture.SourceSnapshotPath));
    }

    private static string[] Lines(string text) =>
        text.Replace("\r\n", "\n").Split('\n');

    private static string Describe(string[] expected, string[] actual, string sourcePath)
    {
        var shared = Math.Min(expected.Length, actual.Length);
        for (int i = 0; i < shared; i++)
        {
            if (expected[i] == actual[i]) continue;
            return $"The generated schema no longer matches {SnapshotFile}.\n" +
                   $"  line {i + 1} expected: {expected[i]}\n" +
                   $"  line {i + 1} actual:   {actual[i]}\n" +
                   $"({expected.Length} snapshot lines, {actual.Length} generated.)\n" +
                   Regenerate(sourcePath);
        }

        var longer = expected.Length > actual.Length ? expected : actual;
        var side = expected.Length > actual.Length ? "only in the snapshot" : "only in the generated";
        return $"The generated schema no longer matches {SnapshotFile}.\n" +
               $"  line {shared + 1} {side}: {longer[shared]}\n" +
               $"({expected.Length} snapshot lines, {actual.Length} generated.)\n" +
               Regenerate(sourcePath);
    }

    private static string Regenerate(string sourcePath) =>
        $"If the change is intentional, rewrite {sourcePath} with GOOSE_UPDATE_SNAPSHOT=1 and " +
        "review the diff.";
}
