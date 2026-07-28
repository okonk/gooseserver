using System.Data.SQLite;
using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

/// <summary>The Queries array in SheetDeriver is a silent-failure surface: drop a query or
/// loosen a WHERE clause and the result is a smaller but entirely plausible sheet list. Nothing
/// downstream notices — sheets.json is static, so no test changes and the only symptom is a
/// missing icon in the editor much later. These tests pin the rules that live nowhere else.</summary>
public class SheetDeriverTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            Directory.Delete(dir, recursive: true);
    }

    /// <summary>Builds a throwaway database with only the columns SheetDeriver reads. The
    /// directory is removed in Dispose.</summary>
    private string WriteDb(params string[] statements)
    {
        var dir = Path.Combine(Path.GetTempPath(), "sheet-deriver-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);

        var path = Path.Combine(dir, "test.db");
        using var conn = new SQLiteConnection($"Data Source={path}");
        conn.Open();

        foreach (var sql in Schema.Concat(statements))
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        return path;
    }

    private static readonly string[] Schema =
    [
        "CREATE TABLE item_templates (graphic_file INTEGER)",
        "CREATE TABLE spells (spellbook_graphic_file INTEGER)",
        """
        CREATE TABLE spell_effects (
            buff_graphic INTEGER, buff_graphic_file INTEGER,
            spell_animation INTEGER, spell_animation_file INTEGER)
        """,
    ];

    /// <summary>Each of the four source columns contributes a distinct sheet, so dropping any
    /// one query loses exactly one expected value.</summary>
    [Fact]
    public void All_four_source_columns_contribute()
    {
        var db = WriteDb(
            "INSERT INTO item_templates VALUES (11)",
            "INSERT INTO spells VALUES (22)",
            "INSERT INTO spell_effects VALUES (1, 33, 1, 44)");

        Assert.Equal(new[] { 11, 22, 33, 44 }, SheetDeriver.Derive(db).ToArray());
    }

    /// <summary>buff_graphic_file is meaningless when buff_graphic is 0 — the row has no buff
    /// icon at all, whatever the file column happens to hold. Same for spell_animation.
    /// Loosening either WHERE clause pulls in sheets no graphic ever references.</summary>
    [Fact]
    public void Graphic_zero_excludes_its_file_column()
    {
        var db = WriteDb(
            "INSERT INTO spell_effects VALUES (0, 1234, 0, 5678)",
            "INSERT INTO spell_effects VALUES (1, 99, 1, 98)");

        var sheets = SheetDeriver.Derive(db);

        Assert.Equal(new[] { 98, 99 }, sheets.ToArray());
        Assert.DoesNotContain(1234, sheets);
        Assert.DoesNotContain(5678, sheets);
    }

    /// <summary>graphic_file 0 is the "no graphic" sentinel and has no manifest entry, so it
    /// must never reach the bundle. Negatives are not expected but are equally unusable.</summary>
    [Fact]
    public void Sentinel_and_negative_sheets_are_filtered()
    {
        var db = WriteDb(
            "INSERT INTO item_templates VALUES (0)",
            "INSERT INTO item_templates VALUES (-1)",
            "INSERT INTO item_templates VALUES (7)");

        Assert.Equal(new[] { 7 }, SheetDeriver.Derive(db).ToArray());
    }

    [Fact]
    public void Nulls_are_skipped()
    {
        var db = WriteDb(
            "INSERT INTO item_templates VALUES (NULL)",
            "INSERT INTO item_templates VALUES (7)");

        Assert.Equal(new[] { 7 }, SheetDeriver.Derive(db).ToArray());
    }

    /// <summary>The same sheet arriving from several queries, and several times within one,
    /// collapses to a single sorted entry.</summary>
    [Fact]
    public void Sheets_are_unioned_deduplicated_and_sorted()
    {
        var db = WriteDb(
            "INSERT INTO item_templates VALUES (50)",
            "INSERT INTO item_templates VALUES (50)",
            "INSERT INTO item_templates VALUES (9)",
            "INSERT INTO spells VALUES (50)",
            "INSERT INTO spell_effects VALUES (1, 50, 1, 20)");

        Assert.Equal(new[] { 9, 20, 50 }, SheetDeriver.Derive(db).ToArray());
    }

    [Fact]
    public void An_empty_database_derives_nothing()
    {
        Assert.Empty(SheetDeriver.Derive(WriteDb()));
    }

    [Fact]
    public void A_missing_database_names_the_path()
    {
        var path = Path.Combine(Path.GetTempPath(), $"no-such-{Guid.NewGuid():N}.db");

        var e = Assert.Throws<InvalidDataException>(() => SheetDeriver.Derive(path));

        Assert.Contains(path, e.Message);
    }

    /// <summary>Schema drift must not look like "this dataset references no sheets".</summary>
    [Fact]
    public void A_missing_table_names_the_database_and_the_query()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sheet-deriver-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        var path = Path.Combine(dir, "empty.db");

        using (var conn = new SQLiteConnection($"Data Source={path}"))
        {
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE unrelated (x INTEGER)";
            cmd.ExecuteNonQuery();
        }

        var e = Assert.Throws<InvalidDataException>(() => SheetDeriver.Derive(path));

        Assert.Contains(path, e.Message);
        Assert.Contains("item_templates", e.Message);
    }

    /// <summary>The derive output exists to be pasted into sheets.json, which wraps its array
    /// too — an unwrapped single line means a human reflows it by hand. The width need not match
    /// the checked-in file's (72 here, up to 82 there), only be narrow enough to read.</summary>
    [Fact]
    public void Formatted_output_wraps_within_the_indented_width()
    {
        var lines = SheetDeriver.Format(Enumerable.Range(1000, 40))
            .Split('\n');

        Assert.All(lines, line => Assert.True(line.Length <= 72, $"too long: {line}"));
        Assert.All(lines, line => Assert.StartsWith("    ", line));
        Assert.True(lines.Length > 1, "expected the list to wrap across lines");

        var values = string.Join("", lines).Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => int.Parse(v.Trim())).ToArray();
        Assert.Equal(Enumerable.Range(1000, 40).ToArray(), values);
    }
}
