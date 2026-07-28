using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class BundlesTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            Directory.Delete(dir, recursive: true);
    }

    // ---- Icons --------------------------------------------------------------------------

    [Fact]
    public void Icons_keys_every_graphic_of_every_configured_sheet()
    {
        var manifest = WriteManifest("""
            {"tileSize":32,"sheets":{
              "5":{"100":[0,0,1,1],"101":[1,0,1,1]},
              "6":{"200":[0,0,1,1]}}}
            """);

        var refs = Bundles.Icons(manifest, Config(iconSheets: [5, 6]));

        Assert.Equal(
            new[] { "5:100", "5:101", "6:200" },
            refs.Select(r => r.Key).Order(StringComparer.Ordinal).ToArray());

        // The key is cosmetic; the sheet/graphic pair is what AtlasBuilder resolves through.
        var first = refs.Single(r => r.Key == "5:100");
        Assert.Equal((5, 100), (first.Sheet, first.Graphic));
    }

    /// <summary>sheets.json is hand-editable, so it can name a sheet the manifest does not have.
    /// That is a config staleness problem, not a build failure.</summary>
    [Fact]
    public void Icons_ignores_a_configured_sheet_absent_from_the_manifest()
    {
        var manifest = WriteManifest("""{"tileSize":32,"sheets":{"5":{"100":[0,0,1,1]}}}""");

        var refs = Bundles.Icons(manifest, Config(iconSheets: [5, 999]));

        Assert.Equal("5:100", Assert.Single(refs).Key);
    }

    // ---- Parts --------------------------------------------------------------------------

    [Fact]
    public void Parts_keys_the_first_frame_of_each_configured_clip()
    {
        var root = NewAssetDir();
        WriteTres(root, "Bodies", "1",
            ("idle-down", [(7, 0, 0, 4, 4), (7, 4, 0, 4, 4)]),
            ("walk-down", [(7, 8, 0, 4, 4)]));

        var parts = Bundles.Parts(root, Config(partCategories: ["Bodies"], partClips: ["idle-down"]));

        var (key, frame) = Assert.Single(parts);
        Assert.Equal("Bodies:1:idle-down", key);
        // The clip's first frame, not its second and not the unconfigured clip's.
        Assert.Equal(new TresFrame(7, 0, 0, 4, 4), frame);
    }

    [Fact]
    public void Parts_skips_a_clip_the_part_does_not_have()
    {
        var root = NewAssetDir();
        WriteTres(root, "Bodies", "1", ("idle-down", [(7, 0, 0, 4, 4)]));

        var parts = Bundles.Parts(root,
            Config(partCategories: ["Bodies"], partClips: ["idle-down", "mounted-idle-down"]));

        Assert.Equal("Bodies:1:idle-down", Assert.Single(parts).Key);
    }

    [Fact]
    public void Parts_ignores_a_category_directory_that_does_not_exist()
    {
        var root = NewAssetDir();
        WriteTres(root, "Bodies", "1", ("idle-down", [(7, 0, 0, 4, 4)]));

        var parts = Bundles.Parts(root,
            Config(partCategories: ["Bodies", "Nope"], partClips: ["idle-down"]));

        Assert.Single(parts);
    }

    [Fact]
    public void Parts_ignores_a_part_directory_with_no_animations()
    {
        var root = NewAssetDir();
        WriteTres(root, "Bodies", "1", ("idle-down", [(7, 0, 0, 4, 4)]));
        Directory.CreateDirectory(Path.Combine(root, "Bodies", "2"));

        var parts = Bundles.Parts(root, Config(partCategories: ["Bodies"], partClips: ["idle-down"]));

        Assert.Single(parts);
    }

    /// <summary>sheets.json lists "Effects" in partCategories as well as effectsCategory. Walked
    /// as a part category it yields nothing (effects have no idle clips) at the cost of parsing
    /// every one of the ~560 effect files a second time, so it is skipped outright.</summary>
    [Fact]
    public void Parts_skips_the_effects_category()
    {
        var root = NewAssetDir();
        WriteTres(root, "Effects", "1080", ("idle-down", [(7, 0, 0, 4, 4)]));
        WriteTres(root, "Bodies", "1", ("idle-down", [(7, 0, 0, 4, 4)]));

        var parts = Bundles.Parts(root,
            Config(partCategories: ["Bodies", "Effects"], partClips: ["idle-down"]));

        // The Effects part deliberately carries a matching clip, so a build that walked the
        // category would produce two entries here.
        Assert.Equal("Bodies:1:idle-down", Assert.Single(parts).Key);
    }

    // ---- Determinism ----------------------------------------------------------------------
    //
    // The emitted fragments are committed, and ShelfPacker breaks height ties by list index, so
    // the order these methods return sprites in decides the atlas layout and therefore every byte
    // of the PNG. Directory.EnumerateDirectories returns filesystem order — on the real asset
    // tree that is hash order, which differs by machine. Both walks must sort.
    //
    // The fixtures name directories so that ordinal order disagrees with both creation order and
    // numeric order, so the assertion fails unless the sort is actually applied.

    [Fact]
    public void Parts_returns_part_directories_in_ordinal_order()
    {
        var root = NewAssetDir();
        foreach (var id in new[] { "2", "10", "1" })
            WriteTres(root, "Bodies", id, ("idle-down", [(7, 0, 0, 4, 4)]));

        var parts = Bundles.Parts(root, Config(partCategories: ["Bodies"], partClips: ["idle-down"]));

        Assert.Equal(
            new[] { "Bodies:1:idle-down", "Bodies:10:idle-down", "Bodies:2:idle-down" },
            parts.Select(p => p.Key).ToArray());
    }

    [Fact]
    public void Effects_returns_effect_directories_in_ordinal_order()
    {
        var root = NewAssetDir();
        foreach (var id in new[] { "2", "10", "1" })
            WriteTres(root, "Effects", id, (id, [(7, 0, 0, 4, 4)]));

        var effects = Bundles.Effects(root, Config());

        Assert.Equal(new[] { "1:0", "10:0", "2:0" }, effects.Select(e => e.Key).ToArray());
    }

    /// <summary>The real corpus is the case that matters: a temp dir with three entries may well
    /// enumerate in creation order anyway, so it cannot prove the sort is load-bearing.</summary>
    [SkippableFact]
    public void Ordering_is_stable_against_the_client_assets()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var root = ManifestTests.AssetRoot!;
        var config = BundleConfig.Load(ConfigPath);

        // Parts keys are "<category>:<id>:<clip>": ids must ascend within each category.
        foreach (var category in Bundles.Parts(root, config)
                     .Select(p => p.Key.Split(':')).GroupBy(k => k[0]))
        {
            var ids = category.Select(k => k[1]).Distinct().ToArray();
            Assert.Equal(ids.Order(StringComparer.Ordinal).ToArray(), ids);
        }

        // Effects keys are "<id>:<frameIndex>".
        var effectIds = Bundles.Effects(root, config)
            .Select(e => e.Key.Split(':')[0]).Distinct().ToArray();

        Assert.Equal(effectIds.Order(StringComparer.Ordinal).ToArray(), effectIds);
    }

    /// <summary>The property the fragments actually need: two builds in one process must produce
    /// byte-identical output, rects and PNG payload alike.</summary>
    [SkippableFact]
    public void Bundles_render_reproducibly_against_the_client_assets()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var root = ManifestTests.AssetRoot!;
        var config = BundleConfig.Load(ConfigPath);
        var manifest = Manifest.Load(root);

        Assert.Equal(RenderAll(manifest, root, config), RenderAll(manifest, root, config));
    }

    private static string RenderAll(Manifest manifest, string root, BundleConfig config)
    {
        using var icons = AtlasBuilder.Build(
            manifest, Bundles.Icons(manifest, config), config.AtlasWidth);
        using var parts = AtlasBuilder.BuildFromFrames(
            manifest, Bundles.Parts(root, config), config.AtlasWidth);
        using var effects = AtlasBuilder.BuildFromFrames(
            manifest, Bundles.Effects(root, config), config.AtlasWidth);

        return BundleWriter.Render("icons", icons.Image, icons.Rects)
             + BundleWriter.Render("parts", parts.Image, parts.Rects)
             + BundleWriter.Render("effects", effects.Image, effects.Rects);
    }

    // ---- Effects ------------------------------------------------------------------------

    [Fact]
    public void Effects_keys_every_frame_by_index()
    {
        var root = NewAssetDir();
        WriteTres(root, "Effects", "1080",
            ("1080", [(7, 0, 0, 4, 4), (7, 4, 0, 4, 4), (7, 8, 0, 4, 4)]));

        var effects = Bundles.Effects(root, Config());

        Assert.Equal(new[] { "1080:0", "1080:1", "1080:2" }, effects.Select(e => e.Key).ToArray());
        Assert.Equal(new TresFrame(7, 4, 0, 4, 4), effects[1].Frame);
    }

    /// <summary>Keys are &lt;id&gt;:&lt;frameIndex&gt;, which only identifies a frame uniquely
    /// while an effect holds one clip. All 564 files in the client corpus do; a second clip would
    /// collide key-for-key and silently last-write-win, so it fails loudly instead.</summary>
    [Fact]
    public void Effects_rejects_a_file_with_more_than_one_clip()
    {
        var root = NewAssetDir();
        WriteTres(root, "Effects", "1080",
            ("1080", [(7, 0, 0, 4, 4)]),
            ("1080-alt", [(7, 4, 0, 4, 4)]));

        var e = Assert.Throws<InvalidDataException>(() => Bundles.Effects(root, Config()));

        Assert.Contains("1080", e.Message);
        Assert.Contains("2", e.Message);
    }

    [Fact]
    public void Effects_returns_nothing_when_the_category_is_absent()
    {
        Assert.Empty(Bundles.Effects(NewAssetDir(), Config()));
    }

    // ---- Real client assets --------------------------------------------------------------

    /// <summary>The hermetic tests above fix the key shapes; this one pins that the real corpus
    /// actually produces each bundle, since every count depends on the client's current art.</summary>
    [SkippableFact]
    public void Every_bundle_is_non_empty_against_the_client_assets()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");
        var root = ManifestTests.AssetRoot!;
        var config = BundleConfig.Load(ConfigPath);
        var manifest = Manifest.Load(root);

        var icons = Bundles.Icons(manifest, config);
        var parts = Bundles.Parts(root, config);
        var effects = Bundles.Effects(root, config);

        Assert.NotEmpty(icons);
        Assert.NotEmpty(parts);
        Assert.NotEmpty(effects);

        // Keys must be unique per bundle: AtlasBuilder throws on a duplicate.
        foreach (var keys in new[] { icons.Select(i => i.Key), parts.Select(p => p.Key),
                                     effects.Select(e => e.Key) })
            Assert.Empty(keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key));

        // Parts must not have walked the effects category.
        Assert.DoesNotContain(parts, p => p.Key.StartsWith(config.EffectsCategory + ":"));
    }

    /// <summary>SpriteBundle.csproj copies sheets.json into the test output; matches
    /// BundleConfigTests.</summary>
    private static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "sheets.json");

    // ---- Fixtures -------------------------------------------------------------------------

    private static BundleConfig Config(
        IReadOnlyList<int>? iconSheets = null,
        IReadOnlyList<string>? partCategories = null,
        IReadOnlyList<string>? partClips = null) => new()
    {
        IconSheets = iconSheets ?? [],
        PartCategories = partCategories ?? [],
        PartClips = partClips ?? [],
    };

    private string NewAssetDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "bundles-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    private Manifest WriteManifest(string json)
    {
        var dir = NewAssetDir();
        File.WriteAllText(Path.Combine(dir, "manifest.json"), json);
        return Manifest.Load(dir);
    }

    /// <summary>Writes &lt;root&gt;/&lt;category&gt;/&lt;id&gt;/animations.tres in the client's
    /// generated format, so the fixtures go through the real TresParser.</summary>
    private static void WriteTres(string root, string category, string id,
        params (string Clip, (int Sheet, int X, int Y, int W, int H)[] Frames)[] clips)
    {
        var dir = Path.Combine(root, category, id);
        Directory.CreateDirectory(dir);

        var sheets = clips.SelectMany(c => c.Frames).Select(f => f.Sheet).Distinct().ToList();
        var text = new System.Text.StringBuilder();
        text.Append("[gd_resource type=\"SpriteFrames\" load_steps=2 format=3]\n\n");

        foreach (var sheet in sheets)
            text.Append($"[ext_resource type=\"Texture2D\" path=\"res://Assets/Sprites/sheets/"
                        + $"{sheet}.png\" id=\"t{sheet}\"]\n");

        var ids = new List<string>();
        var n = 0;
        foreach (var (_, frames) in clips)
            foreach (var f in frames)
            {
                var sub = $"s{n++}";
                ids.Add(sub);
                text.Append($"\n[sub_resource type=\"AtlasTexture\" id=\"{sub}\"]\n"
                            + $"atlas = ExtResource(\"t{f.Sheet}\")\n"
                            + $"region = Rect2({f.X}, {f.Y}, {f.W}, {f.H})\n");
            }

        // Written with an explicit loop rather than a Select closing over a running index: that
        // would silently depend on the sequence being enumerated exactly once, in order.
        text.Append("\n[resource]\nanimations = [");
        var i = 0;
        var written = 0;
        foreach (var (clip, clipFrames) in clips)
        {
            if (written++ > 0) text.Append(',');

            var refs = new List<string>(clipFrames.Length);
            foreach (var _ in clipFrames)
                refs.Add($"{{\"duration\": 1.0,\"texture\": SubResource(\"{ids[i++]}\")}}");

            text.Append($"{{\"frames\": [{string.Join(", ", refs)}],\"loop\": true,"
                        + $"\"name\": &\"{clip}\",\"speed\": 8.0}}");
        }

        text.Append("]\n");

        File.WriteAllText(Path.Combine(dir, "animations.tres"), text.ToString());
    }
}
