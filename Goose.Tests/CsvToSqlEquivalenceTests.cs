using System.Data.SQLite;
using System.Text;
using CsvToSql.Core;

namespace Goose.Tests;

/// <summary>Semantic gate on the generator: executes the recorded baseline script and the
/// freshly generated script into separate SQLite databases and compares schema and every row.
///
/// Comparison is by column NAME, never by position, because two tables legitimately declare
/// their columns in a different order than the pre-descriptor schema did (quests'
/// pass_text/fail_text, npc_templates' armor_pierce). Column ORDER is therefore the one thing
/// this test deliberately tolerates; everything else — a missing/extra table or index, a
/// missing/extra column, a changed type, default, or nullability, a missing/extra row, or any
/// changed cell value that survives SQLite's type affinity — still fails it.
///
/// THE FIXTURE IS FROZEN. `Fixtures/baseline.sql` was recorded at commit 06e2648, before any
/// descriptor work. Never regenerate it from this branch's generator — that would make this
/// test compare the generator against itself and silently pass. If it is ever lost, restore it
/// with `git show 06e2648:Goose.Tests/Fixtures/baseline.sql`.
///
/// THIS IS A MIGRATION GATE, NOT A PERMANENT TEST. It proves the descriptor rewrite changed
/// nothing. Delete it the first time the schema *intentionally* diverges from the pre-descriptor
/// schema — do not add exemptions to keep it passing. The natural successor is a regenerable
/// checked-in snapshot of ConvertWorkbook's output, so an intentional schema change shows up as
/// a reviewable diff rather than as a wall to knock down.</summary>
public class CsvToSqlEquivalenceTests
{
    private static string FixtureDir => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact]
    public void Generated_script_produces_identical_database_to_baseline()
    {
        var baseline = File.ReadAllText(Path.Combine(FixtureDir, "baseline.sql"));

        using var fs = File.OpenRead(Path.Combine(FixtureDir, "aspereta-data.xlsx"));
        var generated = CsvToSqlConverter.ConvertWorkbook(fs);

        var expected = Snapshot(baseline);
        var actual = Snapshot(generated);

        Assert.Equal(expected.Keys.OrderBy(k => k, StringComparer.Ordinal),
                     actual.Keys.OrderBy(k => k, StringComparer.Ordinal));

        foreach (var name in expected.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var e = expected[name];
            var a = actual[name];

            for (int i = 0; i < Math.Min(e.Count, a.Count); i++)
                if (e[i] != a[i])
                    Assert.Fail(
                        $"'{name}' line {i + 1} differs:\n  expected: {e[i]}\n  actual:   {a[i]}");

            if (e.Count != a.Count)
                Assert.Fail($"'{name}': expected {e.Count} snapshot lines, got {a.Count}. " +
                            $"{FirstDifference(e, a)}");
        }
    }

    /// <summary>Only reached once every common line has already matched, so the difference is
    /// always the first line past the shorter snapshot.</summary>
    private static string FirstDifference(List<string> e, List<string> a)
    {
        var shared = Math.Min(e.Count, a.Count);
        var longer = e.Count > a.Count ? e : a;
        var side = e.Count > a.Count ? "only in expected" : "only in actual";
        return $"line {shared + 1} {side}: '{longer[shared]}'";
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

    /// <summary>Removes /* */ and -- comments, which the old hand-written schema carried and
    /// the generator does not. They are annotations, not semantics — nothing this test is
    /// meant to catch can hide in one.</summary>
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
