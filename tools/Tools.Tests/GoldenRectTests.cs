using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

/// <summary>Builds the icon atlas once for the whole class. Each build crops ~4.8k sprites out of
/// the client's sheets, so a per-test rebuild would cost seconds apiece for no added coverage.
/// The fixture is constructed even when there is no client checkout (xunit has no way to skip a
/// fixture), so it holds nulls in that case and each test skips on AssetRoot as usual.</summary>
public sealed class IconBundleFixture : IDisposable
{
    public Manifest? Manifest { get; }
    public BundleConfig? Config { get; }
    public BuiltAtlas? Icons { get; }

    public IconBundleFixture()
    {
        if (ManifestTests.AssetRoot is null) return;

        Manifest = Manifest.Load(ManifestTests.AssetRoot);
        Config = BundleConfig.Load(Path.Combine(AppContext.BaseDirectory, "sheets.json"));
        Icons = AtlasBuilder.Build(Manifest, Bundles.Icons(Manifest, Config), Config.AtlasWidth);
    }

    public void Dispose() => Icons?.Dispose();
}

/// <summary>The correctness gate for the bundles: rects in the emitted index must still describe
/// the sprite the client's manifest describes.</summary>
public class GoldenRectTests(IconBundleFixture fixture) : IClassFixture<IconBundleFixture>
{
    [SkippableFact]
    public void Icon_bundle_rect_dimensions_match_the_manifest()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        foreach (var (key, packed) in fixture.Icons!.Rects)
        {
            var parts = key.Split(':');
            Assert.True(fixture.Manifest!.TryGetRect(
                int.Parse(parts[0]), int.Parse(parts[1]), out var src));
            Assert.Equal(src.W, packed.W);
            Assert.Equal(src.H, packed.H);
        }
    }

    [SkippableFact]
    public void Known_icons_are_present()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        // 810003 is "fire" per aspereta-info/spellbookids.txt (110003 + 700000 offset).
        Assert.Contains("20107:810003", fixture.Icons!.Rects.Keys);
    }

    [SkippableFact]
    public void Part_bundle_covers_every_category()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var frames = Bundles.Parts(ManifestTests.AssetRoot!, fixture.Config!);

        // Effects is listed in partCategories but is deliberately built by Bundles.Effects.
        foreach (var category in fixture.Config!.PartCategories
                     .Where(c => c != fixture.Config.EffectsCategory))
            Assert.Contains(frames, f => f.Key.StartsWith(category + ":", StringComparison.Ordinal));
    }

    [SkippableFact]
    public void Every_part_key_is_unique()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var frames = Bundles.Parts(ManifestTests.AssetRoot!, fixture.Config!);

        Assert.Equal(frames.Count, frames.Select(f => f.Key).Distinct().Count());
    }
}
