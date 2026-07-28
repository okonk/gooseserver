using Goose.Tools.SpriteBundle;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Tools.Tests;

public class AtlasBuilderTests
{
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
        manifest.TryGetRect(20107, 810003, out var src);
        var dst = built.Rects["20107:810003"];

        for (int y = 0; y < src.H; y++)
        for (int x = 0; x < src.W; x++)
            Assert.Equal(sheet[src.X + x, src.Y + y],
                         built.Image[dst.X + x, dst.Y + y]);
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
        Assert.Single(built.Skipped);
        Assert.Equal("bogus", built.Skipped[0]);
    }
}
