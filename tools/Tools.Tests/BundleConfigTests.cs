using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class BundleConfigTests
{
    private static string ConfigPath => Path.Combine(
        AppContext.BaseDirectory, "sheets.json");

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

    [Fact]
    public void A_missing_file_names_the_path()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "no-such-config.json");

        var ex = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));
        Assert.Contains(path, ex.Message);
    }

    [Fact]
    public void Malformed_json_names_the_path()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bundle-config-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not json");
        try
        {
            var ex = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));
            Assert.Contains(path, ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void A_json_null_document_names_the_path()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bundle-config-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "null");
        try
        {
            var ex = Assert.Throws<InvalidDataException>(() => BundleConfig.Load(path));
            Assert.Contains(path, ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
