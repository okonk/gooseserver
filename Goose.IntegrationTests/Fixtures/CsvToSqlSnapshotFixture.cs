using System.Data.SQLite;
using System.Runtime.CompilerServices;
using System.Text;
using CsvToSql.Core;

namespace Goose.IntegrationTests.Fixtures;

public sealed class CsvToSqlSnapshotFixture : IDisposable
{
    private const string SnapshotFile = "generated.snapshot";

    private static string FixtureDir => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public string SourceSnapshotPath { get; }

    public CsvToSqlSnapshotFixture()
    {
        // This file lives inside Fixtures/, so the checked-in snapshot sits next to it —
        // no second Fixtures segment. CallerFilePath is resolved by the compiler.
        SourceSnapshotPath = Path.Combine(SourceDirectory(), SnapshotFile);
    }

    private static string SourceDirectory([CallerFilePath] string here = "") =>
        Path.GetDirectoryName(here)!;

    public string GenerateSnapshot()
    {
        using var fs = File.OpenRead(Path.Combine(FixtureDir, "aspereta-data.xlsx"));
        return Render(Snapshot(CsvToSqlConverter.ConvertWorkbook(fs)));
    }

    public string ReadExpectedSnapshot() =>
        File.ReadAllText(Path.Combine(FixtureDir, SnapshotFile));

    public void RegenerateSnapshot(string contents) =>
        File.WriteAllText(SourceSnapshotPath, contents);

    public void Dispose() { }

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
