using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class BundleConfigTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            Directory.Delete(dir, recursive: true);
    }

    private static string ConfigPath => Path.Combine(
        AppContext.BaseDirectory, "sheets.json");

    /// <summary>Writes a throwaway config; the directory is removed in Dispose.</summary>
    private string WriteTempConfig(string json)
    {
        var dir = Path.Combine(Path.GetTempPath(), "bundle-config-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        var path = Path.Combine(dir, "sheets.json");
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void Loads_the_checked_in_config()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.NotEmpty(config.IconSheets);
        Assert.Equal(2048, config.AtlasWidth);
    }

    [Fact]
    public void Icon_sheets_include_both_datasets()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.Contains(20107, config.IconSheets);   // Aspereta spellbook/buff icon sheet
        Assert.Contains(2269, config.IconSheets);    // Illutia item sheet
        Assert.Contains(20398, config.IconSheets);   // Aspereta item sheet
    }

    [Fact]
    public void Part_categories_cover_all_nine_directories()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.Equal(
            new[] { "Bodies", "Chest", "Effects", "Eyes", "Feet", "Hair", "Hands", "Helms", "Legs" },
            config.PartCategories.OrderBy(c => c).ToArray());
    }

    [Fact]
    public void Part_clips_are_the_four_resting_poses()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.Equal(
            new[] { "idle-no-equip-down", "idle-down", "idle-equip-down", "mounted-idle-down" },
            config.PartClips);
    }

    [Fact]
    public void Sentinel_sheet_zero_is_not_included()
    {
        var config = BundleConfig.Load(ConfigPath);

        // graphic_file 0 means "no graphic" and has no manifest entry.
        Assert.DoesNotContain(0, config.IconSheets);
    }

    [Fact]
    public void Icon_sheets_are_sorted_and_duplicate_free()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.Equal(config.IconSheets.Order().Distinct().ToArray(), config.IconSheets.ToArray());
    }

    [Fact]
    public void Effects_category_is_one_of_the_part_categories()
    {
        var config = BundleConfig.Load(ConfigPath);

        Assert.Contains(config.EffectsCategory, config.PartCategories);
    }

    /// <summary>The icons bundle skips any listed sheet the manifest does not have, silently and
    /// without a count discrepancy. That would leave the file's whole purpose — hand-adding
    /// sheets to widen the palette before the data references them — with no feedback loop, so
    /// the check lives here instead. Passes today; only fires on a bad hand-edit.</summary>
    [SkippableFact]
    public void Every_icon_sheet_exists_in_the_client_manifest()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var config = BundleConfig.Load(ConfigPath);
        var manifest = Manifest.Load(ManifestTests.AssetRoot!);

        var missing = config.IconSheets.Where(s => !manifest.Sheets.ContainsKey(s)).ToArray();

        Assert.True(missing.Length == 0,
            $"sheets.json lists sheets absent from the manifest: {string.Join(", ", missing)}");
    }

    [Fact]
    public void A_missing_file_names_the_path()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "no-such-config.json");

        var e = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));

        Assert.Contains(path, e.Message);
    }

    [Fact]
    public void Malformed_json_names_the_path()
    {
        var path = WriteTempConfig("{ not json");

        var e = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));

        Assert.Contains(path, e.Message);
    }

    [Fact]
    public void A_json_null_document_names_the_path()
    {
        var path = WriteTempConfig("null");

        var e = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));

        Assert.Contains(path, e.Message);
    }

    /// <summary>A zero or negative atlasWidth would reach the packer as a degenerate width
    /// rather than failing at the boundary. (A missing one is fine: it deserialises to the
    /// property default of 2048.)</summary>
    [Theory]
    [InlineData("""{"atlasWidth":0}""")]
    [InlineData("""{"atlasWidth":-1}""")]
    public void Non_positive_atlas_width_names_the_path(string json)
    {
        var path = WriteTempConfig(json);

        var e = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));

        Assert.Contains(path, e.Message);
        Assert.Contains("atlasWidth", e.Message);
    }

    /// <summary>System.Text.Json does not enforce nullable annotations: an explicit `null` calls
    /// the setter and blows straight past the `= []` initialiser, so a null collection reached
    /// Bundles as a NullReferenceException rather than a named build error. Missing and empty are
    /// folded into the same check — all three mean "this bundle would be built from nothing",
    /// which silently overwrote the committed artifact with an empty one and exited 0.</summary>
    [Theory]
    [InlineData("iconSheets", "null")]
    [InlineData("iconSheets", "[]")]
    [InlineData("iconSheets", null)]
    [InlineData("partCategories", "null")]
    [InlineData("partCategories", "[]")]
    [InlineData("partCategories", null)]
    [InlineData("partClips", "null")]
    [InlineData("partClips", "[]")]
    [InlineData("partClips", null)]
    public void An_empty_or_null_or_missing_collection_names_the_path_and_the_property(
        string property, string? value)
    {
        var path = WriteTempConfig(Config(property, value));

        var e = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));

        Assert.Contains(path, e.Message);
        Assert.Contains(property, e.Message);
    }

    /// <summary>A null or blank category would reach Path.Combine and silently resolve to the
    /// asset root itself. (Absent is fine: it deserialises to the property default, "Effects".)</summary>
    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public void A_null_or_blank_effects_category_names_the_path_and_the_property(string value)
    {
        var path = WriteTempConfig(Config("effectsCategory", value));

        var e = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));

        Assert.Contains(path, e.Message);
        Assert.Contains("effectsCategory", e.Message);
    }

    [Fact]
    public void A_config_with_every_required_property_loads()
    {
        var path = WriteTempConfig(Config("atlasWidth", "512"));

        var config = BundleConfig.Load(path);

        Assert.Equal(512, config.AtlasWidth);
        Assert.Equal("Effects", config.EffectsCategory);
    }

    /// <summary>An otherwise-valid config with one property overridden, or — when
    /// <paramref name="value"/> is null — omitted entirely.</summary>
    private static string Config(string property, string? value)
    {
        var fields = new Dictionary<string, string>
        {
            ["atlasWidth"] = "2048",
            ["iconSheets"] = "[1]",
            ["partCategories"] = "[\"Bodies\"]",
            ["partClips"] = "[\"idle-down\"]",
        };

        if (value is null)
            fields.Remove(property);
        else
            fields[property] = value;

        return "{" + string.Join(",", fields.Select(f => $"\"{f.Key}\":{f.Value}")) + "}";
    }
}
