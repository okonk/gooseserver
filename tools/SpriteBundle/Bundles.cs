namespace Goose.Tools.SpriteBundle;

/// <summary>Collects the sprites that go into each bundle, and — more importantly — mints their
/// keys. The three key schemes are the contract with the editor front end, which looks sprites up
/// by them: icons are "&lt;sheet&gt;:&lt;graphic&gt;", parts "&lt;category&gt;:&lt;id&gt;:&lt;clip&gt;",
/// effects "&lt;id&gt;:&lt;frameIndex&gt;".
///
/// Counts are reported at run time rather than asserted — the exact totals depend on the client's
/// current art.</summary>
public static class Bundles
{
    public static List<SpriteRef> Icons(Manifest manifest, BundleConfig config)
    {
        var refs = new List<SpriteRef>();

        foreach (var sheet in config.IconSheets)
        {
            // sheets.json is hand-editable and can name a sheet the manifest has never had; that
            // is config staleness, not a reason to fail a build.
            if (!manifest.Sheets.TryGetValue(sheet, out var graphics)) continue;

            foreach (var graphic in graphics.Keys)
                refs.Add(new SpriteRef($"{sheet}:{graphic}", sheet, graphic));
        }

        return refs;
    }

    /// <summary>Character parts come from .tres clips rather than the manifest, so they are
    /// returned as explicit frames with their own keys.
    ///
    /// The effects category is skipped even when partCategories lists it (sheets.json does):
    /// effect animations carry none of the idle clips, so walking them yields nothing while
    /// parsing every one of the ~560 files that Effects is about to parse again.</summary>
    public static List<(string Key, TresFrame Frame)> Parts(string assetRoot, BundleConfig config)
    {
        var frames = new List<(string Key, TresFrame Frame)>();

        foreach (var category in config.PartCategories)
        {
            if (category == config.EffectsCategory) continue;

            var dir = Path.Combine(assetRoot, category);
            if (!Directory.Exists(dir)) continue;

            foreach (var partDir in Ordered(dir))
            {
                var tres = Path.Combine(partDir, "animations.tres");
                if (!File.Exists(tres)) continue;

                var id = Path.GetFileName(partDir);
                var parsed = TresParser.Parse(tres);

                foreach (var clip in config.PartClips)
                    if (parsed.TryGetFirstFrame(clip, out var frame))
                        frames.Add(($"{category}:{id}:{clip}", frame));
            }
        }

        return frames;
    }

    /// <summary>Every frame of every effect animation. Each Effects/&lt;id&gt;/animations.tres
    /// holds a single clip named after the id, with no directional variants — verified across all
    /// 564 files in the client corpus.
    ///
    /// That single clip is load-bearing rather than incidental: the "&lt;id&gt;:&lt;frameIndex&gt;"
    /// key has no room for a clip name, so a second clip would collide frame-for-frame with the
    /// first and, since Clips is a dictionary with no guaranteed iteration order, which one
    /// survived would not even be deterministic. Fail loudly instead.</summary>
    public static List<(string Key, TresFrame Frame)> Effects(string assetRoot, BundleConfig config)
    {
        var frames = new List<(string Key, TresFrame Frame)>();
        var dir = Path.Combine(assetRoot, config.EffectsCategory);
        if (!Directory.Exists(dir)) return frames;

        foreach (var effectDir in Ordered(dir))
        {
            var tres = Path.Combine(effectDir, "animations.tres");
            if (!File.Exists(tres)) continue;

            var id = Path.GetFileName(effectDir);
            var parsed = TresParser.Parse(tres);

            if (parsed.Clips.Count != 1)
                throw new InvalidDataException(
                    $"{tres}: effect '{id}' has {parsed.Clips.Count} animations, but the "
                    + "'<id>:<frameIndex>' bundle key assumes exactly one");

            var clipFrames = parsed.Clips.First().Value;
            for (var i = 0; i < clipFrames.Count; i++)
                frames.Add(($"{id}:{i}", clipFrames[i]));
        }

        return frames;
    }

    /// <summary>Subdirectories in a fixed order. Directory.EnumerateDirectories returns
    /// filesystem order, which on the client's asset tree is hash order and so differs from
    /// machine to machine. That order is not cosmetic: ShelfPacker breaks height ties by position
    /// in the list these methods return, so it decides the atlas layout and therefore every byte
    /// of the rendered PNG. The fragments are committed, so an unsorted walk would give every
    /// teammate's rebuild a whole-file diff.
    ///
    /// Sorting whole paths is the same as sorting the ids: they share a directory prefix.</summary>
    private static IEnumerable<string> Ordered(string dir) =>
        Directory.EnumerateDirectories(dir).Order(StringComparer.Ordinal);
}
