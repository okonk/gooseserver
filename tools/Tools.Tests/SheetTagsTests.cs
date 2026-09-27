using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class SheetTagsTests
{
    private static string Write(string? animation)
    {
        var dir = Directory.CreateTempSubdirectory("sheettags").FullName;
        if (animation is not null)
            File.WriteAllText(Path.Combine(dir, "animation-manifest.json"), animation);
        return dir;
    }

    [Fact]
    public void Reads_the_item_tile_sheets()
    {
        var dir = Write("""
            {"version":1,"sheets":{
              "20":{"categories":[{"name":"ItemTiles"},{"name":"Tiles"}]},
              "10":{"categories":[{"name":"Tiles"}]},
              "30":{"categories":[{"name":"Body","id":4},{"name":"ItemTiles"}]},
              "40":{}
            },"animations":[]}
            """);

        Assert.Equal([20, 30], SheetTags.Load(dir).ItemSheets);
    }

    [Fact]
    public void Within_keeps_only_the_given_sheets()
    {
        Assert.Equal([2], new SheetTags([1, 2, 3]).Within([2, 5]).ItemSheets);
    }

    [Fact]
    public void Missing_animation_manifest_names_the_file()
    {
        var e = Assert.Throws<InvalidDataException>(() => SheetTags.Load(Write(null)));

        Assert.Contains("animation-manifest.json", e.Message);
    }

    [Fact]
    public void Non_numeric_sheet_key_names_the_file()
    {
        var e = Assert.Throws<InvalidDataException>(() => SheetTags.Load(Write("""{"sheets":{"x":{}}}""")));

        Assert.Contains("animation-manifest.json", e.Message);
    }
}
