using Goose.Tools.SpriteBundle;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tools.Tests;

public class AtlasBuilderTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            Directory.Delete(dir, recursive: true);
    }

    // ---- Tests against the real client assets -------------------------------------------

    /// <summary>Both sprites are checked, not just the first: 810003 packs at atlas origin
    /// (0,0), so a destination-vs-source offset mix-up would still pass on it alone. 810004
    /// lands at a non-zero dst.X and does discriminate.</summary>
    [SkippableFact]
    public void Copies_pixels_exactly_including_transparent_rgb()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var manifest = Manifest.Load(ManifestTests.AssetRoot!);

        // Sheet 20107 contains fully transparent pixels whose RGB is (1,0,0). ImageSharp's
        // DrawImage zeroes those; direct pixel copy must preserve them.
        var sources = new[]
        {
            new SpriteRef("20107:810003", 20107, 810003),
            new SpriteRef("20107:810004", 20107, 810004),
        };

        using var built = AtlasBuilder.Build(manifest, sources, width: 2048);

        using var sheet = Image.Load<Rgba32>(manifest.SheetPath(20107));

        foreach (var (key, graphic) in new[] { ("20107:810003", 810003), ("20107:810004", 810004) })
        {
            manifest.TryGetRect(20107, graphic, out var src);
            var dst = built.Rects[key];

            for (int y = 0; y < src.H; y++)
            for (int x = 0; x < src.W; x++)
                Assert.Equal(sheet[src.X + x, src.Y + y],
                             built.Image[dst.X + x, dst.Y + y]);
        }

        // Guards the discrimination above: if packing order ever changes so that both sprites
        // land at dst.X == 0, the loop stops distinguishing dst from src and this fails loudly.
        Assert.NotEqual(0, built.Rects["20107:810004"].X);
    }

    [SkippableFact]
    public void Rect_index_preserves_sprite_dimensions_from_the_manifest()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var manifest = Manifest.Load(ManifestTests.AssetRoot!);

        var sources = new[] { new SpriteRef("k", 20107, 810003) };
        using var built = AtlasBuilder.Build(manifest, sources, width: 2048);

        manifest.TryGetRect(20107, 810003, out var src);
        Assert.Equal(src.W, built.Rects["k"].W);
        Assert.Equal(src.H, built.Rects["k"].H);
    }

    [SkippableFact]
    public void Skips_graphics_absent_from_the_manifest()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var manifest = Manifest.Load(ManifestTests.AssetRoot!);

        var sources = new[]
        {
            new SpriteRef("real", 20107, 810003),
            new SpriteRef("bogus", 999999, 1),
        };

        using var built = AtlasBuilder.Build(manifest, sources, width: 2048);

        Assert.True(built.Rects.ContainsKey("real"));
        Assert.False(built.Rects.ContainsKey("bogus"));
        Assert.Equal("bogus", Assert.Single(built.Skipped).Key);
    }

    // ---- Hermetic tests -----------------------------------------------------------------
    //
    // The asset-gated tests above cover nothing on a machine without the client checkout, so
    // the behaviour that does not need real art is exercised against hand-built fixtures:
    // temp dir + generated manifest.json + tiny solid-colour PNGs, cleaned up in Dispose.

    private static readonly Rgba32 Red = new(255, 0, 0, 255);
    private static readonly Rgba32 Green = new(0, 255, 0, 255);
    private static readonly Rgba32 Blue = new(0, 0, 255, 255);

    /// <summary>Writes a manifest plus one solid-colour PNG per sheet. A null size writes the
    /// manifest entry but no PNG, which is how the missing-sheet branch is reached.</summary>
    private Manifest WriteAssets(
        params (int Sheet, int? W, int? H, Rgba32 Colour, (int Graphic, SpriteRect Rect)[] Graphics)[] sheets)
    {
        var dir = NewAssetDir();

        var entries = sheets.Select(s =>
        {
            var rects = s.Graphics.Select(g =>
                $"\"{g.Graphic}\":[{g.Rect.X},{g.Rect.Y},{g.Rect.W},{g.Rect.H}]");
            return $"\"{s.Sheet}\":{{{string.Join(",", rects)}}}";
        });

        File.WriteAllText(Path.Combine(dir, "manifest.json"),
            $"{{\"tileSize\":32,\"sheets\":{{{string.Join(",", entries)}}}}}");

        foreach (var s in sheets)
        {
            if (s.W is not { } w || s.H is not { } h) continue;
            using var img = new Image<Rgba32>(w, h, s.Colour);
            img.Save(Path.Combine(dir, "sheets", $"{s.Sheet}.png"));
        }

        return Manifest.Load(dir);
    }

    private string NewAssetDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "atlas-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "sheets"));
        _tempDirs.Add(dir);
        return dir;
    }

    private static (int, SpriteRect)[] G(int graphic, int x, int y, int w, int h) =>
        [(graphic, new SpriteRect(x, y, w, h))];

    /// <summary>Exercises the Math.Max(height, 1) branch: nothing to pack means a zero-height
    /// pack result, and ImageSharp refuses to construct a zero-dimension image.</summary>
    [Fact]
    public void Empty_source_list_produces_a_usable_empty_atlas()
    {
        var manifest = WriteAssets((1, 8, 8, Red, G(10, 0, 0, 4, 4)));

        using var built = AtlasBuilder.Build(manifest, [], width: 64);

        Assert.Equal(1, built.Image.Height);
        Assert.Empty(built.Rects);
        Assert.Empty(built.Skipped);
    }

    /// <summary>Every asset-gated test uses a single sheet, so the GroupBy never runs with more
    /// than one group there. Distinct colours per sheet prove each sprite came from its own.</summary>
    [Fact]
    public void Copies_from_each_sheet_when_sources_span_several()
    {
        var manifest = WriteAssets(
            (1, 8, 8, Red, G(10, 0, 0, 4, 4)),
            (2, 8, 8, Green, G(20, 4, 4, 4, 4)),
            (3, 8, 8, Blue, G(30, 0, 4, 4, 4)));

        using var built = AtlasBuilder.Build(manifest,
            [new SpriteRef("a", 1, 10), new SpriteRef("b", 2, 20), new SpriteRef("c", 3, 30)],
            width: 64);

        Assert.Empty(built.Skipped);
        Assert.Equal(Red, PixelAt(built, "a"));
        Assert.Equal(Green, PixelAt(built, "b"));
        Assert.Equal(Blue, PixelAt(built, "c"));
    }

    private static Rgba32 PixelAt(BuiltAtlas built, string key)
    {
        var r = built.Rects[key];
        return built.Image[r.X, r.Y];
    }

    [Fact]
    public void Skips_and_explains_a_sheet_with_no_png()
    {
        var manifest = WriteAssets((1, null, null, default, G(10, 0, 0, 4, 4)));

        using var built = AtlasBuilder.Build(manifest, [new SpriteRef("a", 1, 10)], width: 64);

        var s = Assert.Single(built.Skipped);
        Assert.Equal("a", s.Key);
        Assert.Contains("no PNG", s.Reason);
    }

    [Fact]
    public void Skips_and_explains_a_graphic_missing_from_the_manifest()
    {
        var manifest = WriteAssets((1, 8, 8, Red, G(10, 0, 0, 4, 4)));

        using var built = AtlasBuilder.Build(manifest, [new SpriteRef("a", 1, 99)], width: 64);

        var s = Assert.Single(built.Skipped);
        Assert.Equal("a", s.Key);
        Assert.Contains("no graphic 99", s.Reason);
    }

    /// <summary>The live case: the client corpus has hundreds of rects that overrun their own
    /// sheet. Before the guard this threw a bare ImageSharp range exception.</summary>
    [Fact]
    public void Skips_and_explains_a_rect_that_overruns_its_sheet()
    {
        var manifest = WriteAssets((1, 8, 8, Red, G(10, 4, 4, 8, 8)));

        using var built = AtlasBuilder.Build(manifest, [new SpriteRef("a", 1, 10)], width: 64);

        var s = Assert.Single(built.Skipped);
        Assert.Equal("a", s.Key);
        Assert.Contains("does not fit sheet 1 (8x8)", s.Reason);
    }

    /// <summary>Every skip carries its sheet as a field. The reason strings for a bad rect embed
    /// the rect itself, so they are distinct per sprite; without the sheet the only way to group
    /// nineteen skips onto the one broken sheet that caused them is to parse the prose.</summary>
    [Fact]
    public void Every_skip_reason_carries_its_sheet_number()
    {
        var manifest = WriteAssets(
            (1, 8, 8, Red, [(10, new SpriteRect(4, 4, 8, 8))]),   // rect overruns the sheet
            (2, null, null, default, G(20, 0, 0, 4, 4)),          // no PNG
            (3, 64, 8, Red, G(30, 0, 0, 64, 8)));                 // wider than the atlas

        using var built = AtlasBuilder.Build(manifest,
            [
                new SpriteRef("overrun", 1, 10), new SpriteRef("nopng", 2, 20),
                new SpriteRef("wide", 3, 30), new SpriteRef("missing", 1, 99),
            ],
            width: 32);

        Assert.Equal(
            new[] { ("missing", 1), ("nopng", 2), ("overrun", 1), ("wide", 3) },
            built.Skipped.Select(s => (s.Key, s.Sheet)).Order().ToArray());
    }

    /// <summary>Latent in the current corpus, guarded anyway: a negative origin would throw the
    /// same opaque range error as an overrun.</summary>
    [Fact]
    public void Skips_and_explains_a_rect_with_a_negative_origin()
    {
        var manifest = WriteAssets((1, 8, 8, Red, G(10, -1, 0, 4, 4)));

        using var built = AtlasBuilder.Build(manifest, [new SpriteRef("a", 1, 10)], width: 64);

        Assert.Contains("does not fit sheet 1", Assert.Single(built.Skipped).Reason);
    }

    /// <summary>A non-positive extent is worse than an overrun: it would not throw at all, just
    /// silently record a nonsense rect and feed a bogus row height to the packer.</summary>
    [Fact]
    public void Skips_and_explains_a_rect_with_a_non_positive_extent()
    {
        var manifest = WriteAssets((1, 8, 8, Red, G(10, 0, 0, 0, 4)));

        using var built = AtlasBuilder.Build(manifest, [new SpriteRef("a", 1, 10)], width: 64);

        Assert.Contains("does not fit sheet 1", Assert.Single(built.Skipped).Reason);
    }

    /// <summary>ShelfPacker throws for an over-wide sprite, naming an index into an internal
    /// list the caller cannot map back to anything. Reported as a skip instead.</summary>
    [Fact]
    public void Skips_and_explains_a_sprite_wider_than_the_atlas()
    {
        var manifest = WriteAssets((1, 64, 8, Red, G(10, 0, 0, 64, 8)));

        using var built = AtlasBuilder.Build(manifest, [new SpriteRef("a", 1, 10)], width: 32);

        Assert.Contains("wider than the 32px atlas", Assert.Single(built.Skipped).Reason);
    }

    /// <summary>Duplicates double-pack the sprite and leave Rects pointing at one copy; if the
    /// two instances differed in resolvability the key would land in Rects and Skipped both.</summary>
    [Fact]
    public void Duplicate_source_key_throws_naming_the_key()
    {
        var manifest = WriteAssets((1, 8, 8, Red,
            [(10, new SpriteRect(0, 0, 4, 4)), (11, new SpriteRect(4, 0, 4, 4))]));

        var e = Assert.Throws<ArgumentException>(() => AtlasBuilder.Build(
            manifest, [new SpriteRef("dup", 1, 10), new SpriteRef("dup", 1, 11)], width: 64));

        Assert.Contains("'dup'", e.Message);
    }

    /// <summary>Destination offsets must come from the packer, not the source rect. Two sprites
    /// cropped from different halves of one sheet must land side by side carrying their own
    /// colours — copying to the source offset instead would put both in the wrong place.</summary>
    [Fact]
    public void Destination_offsets_come_from_the_packer_not_the_source_rect()
    {
        var dir = NewAssetDir();

        // One 8x8 sheet, left half red, right half green.
        using (var img = new Image<Rgba32>(8, 8))
        {
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                img[x, y] = x < 4 ? Red : Green;
            img.Save(Path.Combine(dir, "sheets", "1.png"));
        }

        File.WriteAllText(Path.Combine(dir, "manifest.json"),
            """{"tileSize":32,"sheets":{"1":{"10":[0,0,4,8],"11":[4,0,4,8]}}}""");

        var manifest = Manifest.Load(dir);
        using var built = AtlasBuilder.Build(manifest,
            [new SpriteRef("left", 1, 10), new SpriteRef("right", 1, 11)], width: 8);

        var left = built.Rects["left"];
        var right = built.Rects["right"];

        // Packed side by side into an 8px-wide atlas: one at x=0, the other at x=4.
        Assert.Equal(0, Math.Min(left.X, right.X));
        Assert.Equal(4, Math.Max(left.X, right.X));

        Assert.Equal(Red, built.Image[left.X, left.Y]);
        Assert.Equal(Green, built.Image[right.X, right.Y]);
    }

    // ---- BuildFromFrames ------------------------------------------------------------------
    //
    // Parts and effects carry their rects on the .tres frame instead of looking them up in the
    // manifest. Everything after that lookup is shared with Build, so these tests cover the
    // difference plus one case per hardening guard to pin that the sharing is real.

    [Fact]
    public void Frames_are_packed_from_their_own_rects_not_the_manifest()
    {
        var dir = NewAssetDir();

        using (var img = new Image<Rgba32>(8, 8))
        {
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                img[x, y] = x < 4 ? Red : Green;
            img.Save(Path.Combine(dir, "sheets", "1.png"));
        }

        // The manifest has no graphics at all: BuildFromFrames must not consult it for rects.
        File.WriteAllText(Path.Combine(dir, "manifest.json"),
            """{"tileSize":32,"sheets":{"1":{}}}""");

        var manifest = Manifest.Load(dir);
        using var built = AtlasBuilder.BuildFromFrames(manifest,
            [("left", new TresFrame(1, 0, 0, 4, 8)), ("right", new TresFrame(1, 4, 0, 4, 8))],
            width: 8);

        Assert.Empty(built.Skipped);
        Assert.Equal(Red, built.Image[built.Rects["left"].X, built.Rects["left"].Y]);
        Assert.Equal(Green, built.Image[built.Rects["right"].X, built.Rects["right"].Y]);
    }

    [Fact]
    public void Frames_pack_across_several_sheets()
    {
        var manifest = WriteAssets(
            (1, 8, 8, Red, []), (2, 8, 8, Green, []));

        using var built = AtlasBuilder.BuildFromFrames(manifest,
            [("a", new TresFrame(1, 0, 0, 4, 4)), ("b", new TresFrame(2, 4, 4, 4, 4))],
            width: 64);

        Assert.Empty(built.Skipped);
        Assert.Equal(Red, PixelAt(built, "a"));
        Assert.Equal(Green, PixelAt(built, "b"));
    }

    [Fact]
    public void Frames_produce_a_usable_empty_atlas()
    {
        var manifest = WriteAssets((1, 8, 8, Red, []));

        using var built = AtlasBuilder.BuildFromFrames(manifest, [], width: 64);

        Assert.Equal(1, built.Image.Height);
        Assert.Empty(built.Rects);
    }

    /// <summary>The plan's original BuildFromFrames validated only File.Exists, so an out-of-
    /// bounds .tres rect would have thrown an opaque ImageSharp range error mid-copy.</summary>
    [Fact]
    public void Frames_skip_and_explain_a_rect_that_overruns_its_sheet()
    {
        var manifest = WriteAssets((1, 8, 8, Red, []));

        using var built = AtlasBuilder.BuildFromFrames(manifest,
            [("a", new TresFrame(1, 4, 4, 8, 8))], width: 64);

        Assert.Empty(built.Rects);
        Assert.Contains("does not fit sheet 1 (8x8)", Assert.Single(built.Skipped).Reason);
    }

    [Fact]
    public void Frames_skip_and_explain_a_sheet_with_no_png()
    {
        var manifest = WriteAssets((1, null, null, default, []));

        using var built = AtlasBuilder.BuildFromFrames(manifest,
            [("a", new TresFrame(1, 0, 0, 4, 4))], width: 64);

        Assert.Contains("no PNG", Assert.Single(built.Skipped).Reason);
    }

    [Fact]
    public void Frames_skip_and_explain_a_sprite_wider_than_the_atlas()
    {
        var manifest = WriteAssets((1, 64, 8, Red, []));

        using var built = AtlasBuilder.BuildFromFrames(manifest,
            [("a", new TresFrame(1, 0, 0, 64, 8))], width: 32);

        Assert.Contains("wider than the 32px atlas", Assert.Single(built.Skipped).Reason);
    }

    [Fact]
    public void Frames_with_a_duplicate_key_throw_naming_the_key()
    {
        var manifest = WriteAssets((1, 8, 8, Red, []));

        var e = Assert.Throws<ArgumentException>(() => AtlasBuilder.BuildFromFrames(manifest,
            [("dup", new TresFrame(1, 0, 0, 4, 4)), ("dup", new TresFrame(1, 4, 0, 4, 4))],
            width: 64));

        Assert.Contains("'dup'", e.Message);
    }
}
