using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class ManifestTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            Directory.Delete(dir, recursive: true);
    }

    /// <summary>Client asset root, or null when there is no client checkout to test against.
    /// Defaults to a sibling checkout; override with GOOSE_CLIENT_ASSETS when the client lives
    /// elsewhere. An explicitly set GOOSE_CLIENT_ASSETS that points nowhere throws rather than
    /// returning null: asking for these tests and typo'ing the path must not silently skip.</summary>
    internal static readonly string? AssetRoot = ResolveAssetRoot();

    private static string? ResolveAssetRoot()
    {
        var configured = Environment.GetEnvironmentVariable("GOOSE_CLIENT_ASSETS");
        var root = configured ?? "../Goose2ClientGodot/Assets/Sprites";
        var full = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", root));

        if (File.Exists(Path.Combine(full, "manifest.json")))
            return full;

        if (!string.IsNullOrEmpty(configured))
            throw new DirectoryNotFoundException(
                $"GOOSE_CLIENT_ASSETS is set but no manifest.json was found under '{full}'");

        return null;
    }

    [SkippableFact]
    public void Loads_sheets_and_rects()
    {
        Skip.If(AssetRoot is null, "client assets not available");

        var m = Manifest.Load(AssetRoot);

        Assert.Equal(32, m.TileSize);
        Assert.Equal(7450, m.Sheets.Count);
    }

    [SkippableFact]
    public void Rect_matches_the_raw_manifest()
    {
        Skip.If(AssetRoot is null, "client assets not available");

        var m = Manifest.Load(AssetRoot);

        // Verified against manifest.json: sheet 20107, graphic 810003.
        Assert.True(m.TryGetRect(20107, 810003, out var r));
        Assert.Equal((96, 0, 32, 32), (r.X, r.Y, r.W, r.H));
    }

    [SkippableFact]
    public void Missing_graphic_returns_false()
    {
        Skip.If(AssetRoot is null, "client assets not available");

        var m = Manifest.Load(AssetRoot);

        // graphic_file 0 is the "no graphic" sentinel and is absent from the manifest.
        Assert.False(m.TryGetRect(0, 0, out _));
    }

    /// <summary>The sheets/&lt;id&gt;.png layout is an undocumented contract with the client's
    /// on-disk asset tree, so check a real file rather than comparing strings.</summary>
    [SkippableFact]
    public void SheetPath_points_at_a_real_sheet()
    {
        Skip.If(AssetRoot is null, "client assets not available");

        var m = Manifest.Load(AssetRoot);

        Assert.True(File.Exists(m.SheetPath(20107)), m.SheetPath(20107));
    }

    /// <summary>Writes a throwaway manifest; the directory is removed in Dispose.</summary>
    private string WriteManifest(string json)
    {
        var dir = Path.Combine(Path.GetTempPath(), "manifest-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"), json);
        return dir;
    }

    [Fact]
    public void Missing_sheets_object_names_the_file()
    {
        var dir = WriteManifest("""{"tileSize":32}""");

        var e = Assert.Throws<InvalidDataException>(() => Manifest.Load(dir));

        Assert.Contains("manifest.json", e.Message);
        Assert.Contains("'sheets'", e.Message);
    }

    [Fact]
    public void Short_rect_names_the_sheet_and_graphic()
    {
        var dir = WriteManifest("""{"tileSize":32,"sheets":{"7":{"42":[1,2]}}}""");

        var e = Assert.Throws<InvalidDataException>(() => Manifest.Load(dir));

        Assert.Contains("manifest.json", e.Message);
        Assert.Contains("sheet 7 graphic 42", e.Message);
    }

    [Fact]
    public void Non_numeric_key_names_the_key()
    {
        var dir = WriteManifest("""{"tileSize":32,"sheets":{"bogus":{"42":[1,2,3,4]}}}""");

        var e = Assert.Throws<InvalidDataException>(() => Manifest.Load(dir));

        Assert.Contains("manifest.json", e.Message);
        Assert.Contains("'bogus'", e.Message);
    }

    [Fact]
    public void Null_inner_sheet_names_the_sheet()
    {
        var dir = WriteManifest("""{"tileSize":32,"sheets":{"7":null}}""");

        var e = Assert.Throws<InvalidDataException>(() => Manifest.Load(dir));

        Assert.Contains("manifest.json", e.Message);
        Assert.Contains("sheet 7", e.Message);
    }

    /// <summary>A silently-zero tile size would propagate into downstream geometry, so a
    /// missing or mis-cased tileSize must fail at the boundary instead.</summary>
    [Fact]
    public void Missing_tile_size_names_the_file()
    {
        var dir = WriteManifest("""{"TileSize":32,"sheets":{"7":{"42":[1,2,3,4]}}}""");

        var e = Assert.Throws<InvalidDataException>(() => Manifest.Load(dir));

        Assert.Contains("manifest.json", e.Message);
        Assert.Contains("tileSize", e.Message);
    }
}
