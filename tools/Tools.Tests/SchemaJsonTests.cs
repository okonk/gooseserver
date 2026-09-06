using System.Text.Json;
using CsvToSql.Core.Schema;
using Goose.Tools.SchemaGen;

namespace Tools.Tests;

public class SchemaJsonTests
{
    // Registry order, not alphabetical: the artifact must preserve emission order.
    private static readonly string[] ConsumedSheets = { "NPCs", "NPC Spawns", "Warptiles", "Maps" };

    [Fact]
    public void Output_is_parseable_camel_case_json()
    {
        var json = SchemaJson.Render(SchemaModel.Build());
        using var doc = JsonDocument.Parse(json);

        var sheet = doc.RootElement.GetProperty("sheets").EnumerateArray().First();
        Assert.True(sheet.TryGetProperty("table", out _));
        var column = sheet.GetProperty("columns").EnumerateArray().First();
        Assert.True(column.TryGetProperty("name", out _));
        Assert.True(column.TryGetProperty("kind", out _));
        Assert.True(column.TryGetProperty("sql", out _));
        Assert.True(column.TryGetProperty("required", out _));
        Assert.True(column.TryGetProperty("pk", out _));

        Assert.DoesNotContain("\"Sheets\"", json);
        Assert.DoesNotContain("\"Columns\"", json);
        Assert.DoesNotContain("\"Name\"", json);
    }

    [Fact]
    public void Output_uses_lf_line_endings_on_every_platform()
    {
        Assert.DoesNotContain('\r', SchemaJson.Render(SchemaModel.Build()));
    }

    [Fact]
    public void Omits_null_optional_fields()
    {
        using var doc = JsonDocument.Parse(SchemaJson.Render(SchemaModel.Build()));

        // map_x is a plain required Int: no default, no reference, no enum members.
        var column = doc.RootElement.GetProperty("sheets").EnumerateArray()
            .Single(s => s.GetProperty("sheet").GetString() == "Warptiles")
            .GetProperty("columns").EnumerateArray()
            .Single(c => c.GetProperty("name").GetString() == "map_x");

        Assert.False(column.TryGetProperty("default", out _));
        Assert.False(column.TryGetProperty("ref", out _));
        Assert.False(column.TryGetProperty("enumNames", out _));
    }

    [Fact]
    public void Contains_exactly_the_four_consumed_sheets_in_registry_order()
    {
        using var doc = JsonDocument.Parse(SchemaJson.Render(SchemaModel.Build()));

        var names = doc.RootElement.GetProperty("sheets").EnumerateArray()
            .Select(s => s.GetProperty("sheet").GetString()).ToList();

        Assert.Equal(ConsumedSheets, names);
    }

    [Fact]
    public void Columns_match_the_registry_in_full_order()
    {
        using var doc = JsonDocument.Parse(SchemaJson.Render(SchemaModel.Build()));

        foreach (var sheet in doc.RootElement.GetProperty("sheets").EnumerateArray())
        {
            var name = sheet.GetProperty("sheet").GetString();
            var expected = SchemaRegistry.Tables.Single(t => t.Sheet == name)
                .Columns.Select(c => c.Name).ToList();
            var actual = sheet.GetProperty("columns").EnumerateArray()
                .Select(c => c.GetProperty("name").GetString()!).ToList();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Every_column_carries_its_verified_header()
    {
        using var doc = JsonDocument.Parse(SchemaJson.Render(SchemaModel.Build()));

        var columns = doc.RootElement.GetProperty("sheets").EnumerateArray()
            .SelectMany(s => s.GetProperty("columns").EnumerateArray()).ToList();

        Assert.NotEmpty(columns);
        Assert.All(columns, c =>
        {
            var header = c.GetProperty("header").GetString();
            Assert.False(string.IsNullOrWhiteSpace(header));
        });
    }

    [Fact]
    public void Main_with_one_path_writes_only_js()
    {
        var dir = CreateTempDir();
        try
        {
            var js = Path.Combine(dir, "schema.js");
            var json = Path.Combine(dir, "schema.json");

            Assert.Equal(0, Program.Main(new[] { js }));
            Assert.True(File.Exists(js));
            Assert.False(File.Exists(json));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Main_with_two_paths_writes_js_then_json()
    {
        var dir = CreateTempDir();
        try
        {
            var js = Path.Combine(dir, "schema.js");
            var json = Path.Combine(dir, "schema.json");

            Assert.Equal(0, Program.Main(new[] { js, json }));
            Assert.True(File.Exists(js));
            Assert.True(File.Exists(json));

            using var doc = JsonDocument.Parse(File.ReadAllText(json));
            Assert.Equal(ConsumedSheets, doc.RootElement.GetProperty("sheets").EnumerateArray()
                .Select(s => s.GetProperty("sheet").GetString()).ToList());
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Main_with_an_invalid_argument_count_fails_without_writing(int count)
    {
        var dir = CreateTempDir();
        try
        {
            var exit = Program.Main(new string[count]);

            Assert.Equal(1, exit);
            Assert.Empty(Directory.GetFiles(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Main_when_the_json_write_fails_returns_failure()
    {
        var dir = CreateTempDir();
        try
        {
            var js = Path.Combine(dir, "schema.js");
            var blocker = Path.Combine(dir, "blocked");
            File.WriteAllText(blocker, "");
            var json = Path.Combine(blocker, "schema.json");

            var exit = Program.Main(new[] { js, json });

            Assert.Equal(1, exit);
            Assert.True(File.Exists(js));
            Assert.False(File.Exists(json));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "schemagen-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
