using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class TresParserTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
            Directory.Delete(dir, recursive: true);
    }

    private static string PartPath(string category, int id) =>
        Path.Combine(ManifestTests.AssetRoot!, category, id.ToString(), "animations.tres");

    [SkippableFact]
    public void Parses_every_clip_in_the_file()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var parts = TresParser.Parse(PartPath("Bodies", 1));

        // Verified: Bodies/1 declares 68 clips over 164 AtlasTexture sub-resources.
        Assert.Equal(68, parts.Clips.Count);
    }

    [SkippableFact]
    public void Resolves_first_frame_to_its_sheet_and_rect()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var parts = TresParser.Parse(PartPath("Bodies", 1));

        // Verified by reading Bodies/1/animations.tres directly.
        Assert.True(parts.TryGetFirstFrame("idle-no-equip-down", out var idle));
        Assert.Equal((115, 0, 48, 24, 48), (idle.Sheet, idle.X, idle.Y, idle.W, idle.H));

        Assert.True(parts.TryGetFirstFrame("mounted-idle-down", out var mounted));
        Assert.Equal((125, 0, 80, 52, 80), (mounted.Sheet, mounted.X, mounted.Y, mounted.W, mounted.H));
    }

    [SkippableFact]
    public void Distinct_clips_resolve_to_distinct_frames()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var parts = TresParser.Parse(PartPath("Bodies", 1));

        // Regression guard: a lazy regex that spans clip boundaries makes every clip
        // resolve to the file's first frame. These three must differ.
        parts.TryGetFirstFrame("idle-no-equip-down", out var a);
        parts.TryGetFirstFrame("idle-equip-down", out var b);
        parts.TryGetFirstFrame("mounted-idle-down", out var c);

        Assert.NotEqual(a, b);
        Assert.NotEqual(b, c);
        Assert.NotEqual(a, c);
        Assert.Equal(116, b.Sheet);   // idle-equip-down lives on a different sheet
    }

    [SkippableFact]
    public void Absent_clip_reports_false()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var parts = TresParser.Parse(PartPath("Bodies", 1));

        Assert.False(parts.TryGetFirstFrame("no-such-clip", out _));
    }

    [SkippableFact]
    public void Effect_animations_have_one_clip_named_after_the_id()
    {
        Skip.If(ManifestTests.AssetRoot is null, "client assets not available");

        var dir = Directory.GetDirectories(Path.Combine(ManifestTests.AssetRoot!, "Effects"))
                           .OrderBy(d => d).First();
        var id = Path.GetFileName(dir);

        var parts = TresParser.Parse(Path.Combine(dir, "animations.tres"));

        Assert.Single(parts.Clips);
        Assert.Equal(id, parts.Clips.Keys.Single());
        Assert.True(parts.Clips[id].Count >= 1);
    }

    // ---- hermetic fixtures ----------------------------------------------------------------

    /// <summary>Writes a throwaway .tres; the directory is removed in Dispose.</summary>
    private string WriteTres(string text)
    {
        var dir = Path.Combine(Path.GetTempPath(), "tres-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        var path = Path.Combine(dir, "animations.tres");
        File.WriteAllText(path, text);
        return path;
    }

    /// <summary>Atlas_0's region uses the bare-integer form and Atlas_1's the float form; Godot
    /// writes both, and the assertions on Atlas_1 are what cover the truncating parse.</summary>
    private static string Fixture(string animations) => $"""
        [gd_resource type="SpriteFrames" format=3]

        [ext_resource type="Texture2D" path="res://Assets/Sprites/sheets/115.png" id="Tex_0"]
        [ext_resource type="Texture2D" path="res://Assets/Sprites/sheets/116.png" id="Tex_1"]

        [sub_resource type="AtlasTexture" id="Atlas_0"]
        atlas = ExtResource("Tex_0")
        region = Rect2(0, 48, 24, 48)

        [sub_resource type="AtlasTexture" id="Atlas_1"]
        atlas = ExtResource("Tex_1")
        region = Rect2(24.0, 96.0, 24.0, 48.0)

        [sub_resource type="AtlasTexture" id="Atlas_orphan"]
        atlas = ExtResource("Tex_missing")
        region = Rect2(9, 9, 9, 9)

        [resource]
        animations = [{animations}]

        """;

    private static string Clip(string name, params string[] atlasIds) =>
        "{\"frames\": ["
        + string.Join(", ", atlasIds.Select(id =>
            $"{{\"duration\": 1.0, \"texture\": SubResource(\"{id}\")}}"))
        + $"],\"loop\": true,\"name\": &\"{name}\",\"speed\": 8.0}}";

    /// <summary>The whole animations array is one line and frame entries contain '}', so a
    /// per-clip lazy regex spans every earlier clip and resolves them all to the first frame.
    /// This is the load-bearing guard and deliberately does not need a client checkout.</summary>
    [Fact]
    public void Clips_sharing_one_line_keep_their_own_frames()
    {
        var path = WriteTres(Fixture(
            string.Join(", ", Clip("first", "Atlas_0"), Clip("second", "Atlas_1"))));

        var parts = TresParser.Parse(path);

        Assert.True(parts.TryGetFirstFrame("first", out var first));
        Assert.True(parts.TryGetFirstFrame("second", out var second));
        Assert.Equal(new TresFrame(115, 0, 48, 24, 48), first);
        Assert.Equal(new TresFrame(116, 24, 96, 24, 48), second);
    }

    [Fact]
    public void Frames_keep_their_declared_order()
    {
        var path = WriteTres(Fixture(Clip("walk", "Atlas_1", "Atlas_0")));

        var parts = TresParser.Parse(path);

        Assert.Equal(
            [new TresFrame(116, 24, 96, 24, 48), new TresFrame(115, 0, 48, 24, 48)],
            parts.Clips["walk"]);
    }

    /// <summary>A frame pointing at a sub-resource that is not declared cannot be located, but
    /// dropping it silently would shift every later frame index, so it must fail loudly.</summary>
    [Fact]
    public void Unknown_sub_resource_reference_names_the_file_and_the_id()
    {
        var path = WriteTres(Fixture(Clip("walk", "Atlas_0", "Atlas_nope")));

        var e = Assert.Throws<InvalidDataException>(() => TresParser.Parse(path));

        Assert.Contains("animations.tres", e.Message);
        Assert.Contains("Atlas_nope", e.Message);
        Assert.Contains("walk", e.Message);
    }

    /// <summary>An AtlasTexture whose ExtResource is undeclared has no sheet number. It is only
    /// a problem if a clip actually uses it, so parsing succeeds while nothing references it.</summary>
    [Fact]
    public void Unreferenced_atlas_with_unknown_ext_resource_is_ignored()
    {
        var path = WriteTres(Fixture(Clip("walk", "Atlas_0")));

        var parts = TresParser.Parse(path);

        Assert.True(parts.TryGetFirstFrame("walk", out var frame));
        Assert.Equal(new TresFrame(115, 0, 48, 24, 48), frame);
    }

    [Fact]
    public void Referenced_atlas_with_unknown_ext_resource_names_the_file_and_the_id()
    {
        var path = WriteTres(Fixture(Clip("walk", "Atlas_orphan")));

        var e = Assert.Throws<InvalidDataException>(() => TresParser.Parse(path));

        Assert.Contains("animations.tres", e.Message);
        Assert.Contains("Atlas_orphan", e.Message);
    }

    /// <summary>The lazy frames group is bounded only by the newline, so a clip the regex cannot
    /// match is not merely skipped: the match starting at its '{"frames": [' runs through it and
    /// terminates on the NEXT clip's tail, handing that clip the broken clip's frames. The result
    /// is a confident wrong sprite, so the clip count must be checked against the file.</summary>
    [Fact]
    public void A_malformed_clip_cannot_silently_donate_its_frames_to_the_next()
    {
        var broken = Clip("first", "Atlas_0").Replace("\"speed\": 8.0", "\"speed\": null");
        var path = WriteTres(Fixture(string.Join(", ", broken, Clip("second", "Atlas_1"))));

        var e = Assert.Throws<InvalidDataException>(() => TresParser.Parse(path));

        Assert.Contains("animations.tres", e.Message);
    }

    /// <summary>Two clips sharing a name would overwrite each other in the dictionary, losing one
    /// silently. The same count check catches it.</summary>
    [Fact]
    public void A_duplicate_clip_name_names_the_file()
    {
        var path = WriteTres(Fixture(
            string.Join(", ", Clip("walk", "Atlas_0"), Clip("walk", "Atlas_1"))));

        var e = Assert.Throws<InvalidDataException>(() => TresParser.Parse(path));

        Assert.Contains("animations.tres", e.Message);
    }

    [Fact]
    public void A_file_with_no_clips_names_the_file()
    {
        var path = WriteTres("[gd_resource type=\"SpriteFrames\" format=3]\n");

        var e = Assert.Throws<InvalidDataException>(() => TresParser.Parse(path));

        Assert.Contains("animations.tres", e.Message);
    }

    [Fact]
    public void A_resource_that_is_not_SpriteFrames_names_the_file()
    {
        var path = WriteTres(Fixture(Clip("walk", "Atlas_0"))
            .Replace("type=\"SpriteFrames\"", "type=\"Shader\""));

        var e = Assert.Throws<InvalidDataException>(() => TresParser.Parse(path));

        Assert.Contains("animations.tres", e.Message);
        Assert.Contains("SpriteFrames", e.Message);
    }

    [Fact]
    public void A_missing_file_throws_naming_the_path()
    {
        var missing = Path.Combine(Path.GetTempPath(), "no-such-dir-" + Guid.NewGuid().ToString("N"),
                                   "animations.tres");

        var e = Record.Exception(() => TresParser.Parse(missing));

        Assert.NotNull(e);
        Assert.Contains("animations.tres", e.Message);
    }
}
