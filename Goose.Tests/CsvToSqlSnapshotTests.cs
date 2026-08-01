using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Text;
using CsvToSql.Core;

namespace Goose.Tests;

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
///     GOOSE_UPDATE_SNAPSHOT=1 dotnet test Goose.Tests --filter FullyQualifiedName~CsvToSqlSnapshot
///
/// then read the diff and commit it if it is what you meant. Regenerating without reading the diff
/// is the one way to make this test worthless, and no flag can stop that — the review is the test.
///
/// THE INPUT IS FIXED: Fixtures/aspereta-data.xlsx, a real workbook. The snapshot is therefore a
/// statement about the generator, not about today's game data.</summary>
public class CsvToSqlSnapshotTests
{
    private const string SnapshotFile = "generated.snapshot";

    private static string FixtureDir => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    /// <summary>The fixture directory in the SOURCE tree, which is where a regenerated snapshot has
    /// to land: FixtureDir above is the copy under bin/, and rewriting that one would "pass" until
    /// the next build overwrote it from source. CallerFilePath is resolved by the compiler, so this
    /// needs no assumption about how deep bin/Debug/net10.0 happens to be.</summary>
    private static string SourceFixtureDir([CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here)!, "Fixtures");

    [Fact]
    public void Generated_script_matches_the_recorded_snapshot()
    {
        using var fs = File.OpenRead(Path.Combine(FixtureDir, "aspereta-data.xlsx"));
        var actual = Render(Snapshot(CsvToSqlConverter.ConvertWorkbook(fs)));

        var sourcePath = Path.Combine(SourceFixtureDir(), SnapshotFile);
        if (Environment.GetEnvironmentVariable("GOOSE_UPDATE_SNAPSHOT") == "1")
        {
            File.WriteAllText(sourcePath, actual);
            // Not a silent pass: a run that rewrote the fixture has verified nothing, and saying so
            // is what keeps GOOSE_UPDATE_SNAPSHOT out of anyone's habitual test command.
            Assert.Fail($"Rewrote {sourcePath}. Review the diff, commit it if it is what you meant, " +
                        "then run the tests again without GOOSE_UPDATE_SNAPSHOT.");
        }

        var path = Path.Combine(FixtureDir, SnapshotFile);
        Assert.True(File.Exists(path),
            $"No snapshot at {path}. Record one with GOOSE_UPDATE_SNAPSHOT=1 (see this class's doc).");

        var expected = File.ReadAllText(path);
        if (expected == actual) return;

        // Reported as ONE line of difference rather than as two megabytes of string inequality:
        // xUnit's own diff on a million-character string is unreadable, and the first divergence is
        // what a reader needs — the schema is rendered before any row of any table, so a DDL change
        // always surfaces ahead of the rows it moved.
        Assert.Fail(Describe(Lines(expected), Lines(actual), sourcePath));
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

    /// <summary>One deterministic text rendering of the whole snapshot: objects in name order, each
    /// one's own lines in the order Snapshot built them. Text rather than a structure so the
    /// checked-in artefact is something a human reviews in a diff, which is the whole point of the
    /// successor.</summary>
    private static string Render(Dictionary<string, List<string>> snapshot)
    {
        var sb = new StringBuilder();
        foreach (var name in snapshot.Keys.OrderBy(k => k, StringComparer.Ordinal))
            foreach (var line in snapshot[name])
                sb.Append(line).Append('\n');
        return sb.ToString();
    }

    /// <summary>Executes a script into a temp database and returns object name -> its schema
    /// rendered order-insensitively, followed (for tables) by every row rendered as
    /// name=value pairs sorted by column name, in rowid order.</summary>
    private static Dictionary<string, List<string>> Snapshot(string script)
    {
        var path = Path.Combine(Path.GetTempPath(), $"goose-{Guid.NewGuid():N}.db");
        try
        {
            using var conn = new SQLiteConnection($"Data Source={path}");
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = script;
                cmd.ExecuteNonQuery();
            }

            var objects = new List<(string Name, string Type, string Sql)>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT name, type, sql FROM sqlite_master WHERE type IN ('table','index') " +
                    "AND name NOT LIKE 'sqlite_%' ORDER BY name";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    objects.Add((r.GetString(0), r.GetString(1), r.IsDBNull(2) ? "" : r.GetString(2)));
            }

            var result = new Dictionary<string, List<string>>();
            foreach (var (name, type, sql) in objects)
            {
                if (type != "table")
                {
                    // An index has no rows of its own; its normalised DDL is the whole story.
                    result[name] = new List<string> { "INDEX: " + Normalise(sql) };
                    continue;
                }

                var lines = new List<string> { "TABLE: " + name };
                lines.AddRange(ColumnDefinitions(sql).Select(d => "  COL: " + d));

                // Sorted by name so a declaration-order change cannot show up as a row diff.
                var columns = ColumnNames(conn, name);
                var select = string.Join(", ", columns.Select(c => $"\"{c}\""));

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT {select} FROM {name} ORDER BY rowid";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        var vals = new string[r.FieldCount];
                        for (int i = 0; i < r.FieldCount; i++)
                            vals[i] = columns[i] + "=" +
                                      (r.IsDBNull(i) ? "<null>" : r.GetValue(i).ToString());
                        lines.Add(string.Join("|", vals));
                    }
                }

                result[name] = lines;
            }

            return result;
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static List<string> ColumnNames(SQLiteConnection conn, string table)
    {
        var names = new List<string>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{table}\")";
        using var r = cmd.ExecuteReader();
        while (r.Read()) names.Add(r.GetString(1));
        names.Sort(StringComparer.Ordinal);
        return names;
    }

    /// <summary>Splits a CREATE TABLE body into one normalised definition string per column,
    /// sorted. Sorting is what makes declaration order irrelevant; each definition still
    /// carries its column's full type, default, nullability and key clauses verbatim, so a
    /// change to any of those still registers.</summary>
    private static List<string> ColumnDefinitions(string createTable)
    {
        int open = createTable.IndexOf('(');
        int close = createTable.LastIndexOf(')');
        Assert.True(open >= 0 && close > open, $"Unparseable CREATE TABLE: {createTable}");

        var body = StripComments(createTable.Substring(open + 1, close - open - 1));

        var defs = new List<string>();
        int depth = 0, start = 0;
        bool inString = false;
        for (int i = 0; i < body.Length; i++)
        {
            char ch = body[i];
            // A quoted default may contain commas and parens (equipped_items), so string
            // literals are opaque to the splitter. '' inside a literal closes then reopens,
            // which lands in the same place.
            if (ch == '\'') inString = !inString;
            else if (inString) continue;
            else if (ch == '(') depth++;
            else if (ch == ')') depth--;
            else if (ch == ',' && depth == 0)
            {
                defs.Add(Normalise(body.Substring(start, i - start)));
                start = i + 1;
            }
        }
        Assert.False(inString, $"Unterminated string literal in CREATE TABLE: {createTable}");
        defs.Add(Normalise(body.Substring(start)));

        defs.RemoveAll(d => d.Length == 0);
        defs.Sort(StringComparer.Ordinal);
        return defs;
    }

    /// <summary>Removes /* */ and -- comments. The generator emits none today, but the splitter
    /// above is what reads a CREATE TABLE, and a comment introduced into one must not be able to
    /// hide a column definition from it.</summary>
    private static string StripComments(string body)
    {
        var sb = new StringBuilder();
        bool inString = false;
        for (int i = 0; i < body.Length; i++)
        {
            if (!inString && i + 1 < body.Length && body[i] == '/' && body[i + 1] == '*')
            {
                int end = body.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? body.Length : end + 1;
                sb.Append(' ');
                continue;
            }
            if (!inString && i + 1 < body.Length && body[i] == '-' && body[i + 1] == '-')
            {
                int end = body.IndexOf('\n', i);
                i = end < 0 ? body.Length : end;
                sb.Append(' ');
                continue;
            }
            if (body[i] == '\'') inString = !inString;
            sb.Append(body[i]);
        }
        return sb.ToString();
    }

    /// <summary>Collapses whitespace so indentation and CRLF differences do not register.</summary>
    private static string Normalise(string sql) =>
        string.Join(" ", sql.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ")
                           .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
