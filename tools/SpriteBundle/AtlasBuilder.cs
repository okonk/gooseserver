using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Goose.Tools.SpriteBundle;

/// <summary>One sprite to pull into a bundle. Key is what the editor looks up by.</summary>
public readonly record struct SpriteRef(string Key, int Sheet, int Graphic);

/// <summary>A sprite that could not be packed, and why. The reason is carried rather than
/// inferred because the causes need different responses: a missing manifest entry usually means a
/// stale graphic id in the game data, whereas a rect that does not fit its own sheet is an
/// asset-pipeline bug. Collapsing them into a bare count hides the second behind the first.
///
/// Sheet is a field rather than something to read out of Reason because skips cluster by sheet,
/// not by message: a bad-rect reason embeds the rect, so nineteen sprites off the end of one sheet
/// produce nineteen distinct strings and grouping by Reason reports the same sheet nineteen
/// times.</summary>
public sealed record SkippedSprite(string Key, int Sheet, string Reason);

public sealed class BuiltAtlas : IDisposable
{
    public required Image<Rgba32> Image { get; init; }
    public required IReadOnlyDictionary<string, SpriteRect> Rects { get; init; }
    public required IReadOnlyList<SkippedSprite> Skipped { get; init; }

    public void Dispose() => Image.Dispose();
}

/// <summary>Crops sprites out of the client's sheet PNGs and packs them into one atlas.
///
/// Uses direct pixel assignment rather than ctx.DrawImage: DrawImage is not pixel-exact, it
/// rewrites fully transparent pixels to Rgba32(0,0,0,0), discarding whatever RGB the source
/// carried under a zero alpha (the client's sheets do contain such pixels, e.g. Rgba32(1,0,0,0)
/// in sheet 20107). Invisible when drawn, but it defeats exact verification. The per-pixel loop
/// is fast enough not to matter: measured at roughly 40 MPx/s including PNG decode, so every
/// sprite in the client manifest at once (336 MPx) copies in about 8s.
///
/// Anything that cannot be packed is reported through Skipped rather than thrown: the client
/// corpus contains hundreds of sprites whose manifest rect overruns its own sheet, and one bad
/// asset must not take down a bundle build.</summary>
public static class AtlasBuilder
{
    /// <summary>One sprite as the packing core sees it, after the caller has said where its
    /// source rect comes from. Unresolved is non-null when the caller could not produce a rect at
    /// all; it is carried through rather than dropped so the key still gets a Skipped entry (and
    /// still participates in duplicate detection).</summary>
    private readonly record struct Candidate(
        string Key, int Sheet, SpriteRect Rect, string? UnresolvedReason);

    /// <summary>Packs sprites whose rects live in the manifest, keyed by sheet and graphic id.</summary>
    public static BuiltAtlas Build(Manifest manifest, IReadOnlyList<SpriteRef> sources, int width) =>
        BuildCore(manifest, sources.Select(s =>
            manifest.TryGetRect(s.Sheet, s.Graphic, out var rect)
                ? new Candidate(s.Key, s.Sheet, rect, null)
                : new Candidate(s.Key, s.Sheet, default,
                    $"sheet {s.Sheet} has no graphic {s.Graphic} in the manifest")),
            sources.Count, width, nameof(sources));

    /// <summary>Packs frames whose rects are already known (they come from a .tres) rather than
    /// looked up in the manifest. Everything after that — bounds validation against the real PNG,
    /// duplicate detection, packing, sheet-grouped decode, pixel copy — is shared with Build, so
    /// both entry points behave identically on bad input.</summary>
    public static BuiltAtlas BuildFromFrames(Manifest manifest,
        IReadOnlyList<(string Key, TresFrame Frame)> frames, int width) =>
        BuildCore(manifest, frames.Select(f => new Candidate(
                f.Key, f.Frame.Sheet, new SpriteRect(f.Frame.X, f.Frame.Y, f.Frame.W, f.Frame.H),
                null)),
            frames.Count, width, nameof(frames));

    private static BuiltAtlas BuildCore(Manifest manifest, IEnumerable<Candidate> candidates,
                                       int count, int width, string paramName)
    {
        var resolved = new List<Candidate>(count);
        var skipped = new List<SkippedSprite>();
        var keys = new HashSet<string>(count);

        // Sheets are shared by many sprites (~104k sprites over ~7.4k sheets in the client
        // corpus), so probe each PNG's header once. Null means the file is absent.
        var sheetSizes = new Dictionary<int, (int W, int H)?>();

        foreach (var s in candidates)
        {
            // A duplicate key would pack the sprite twice and leave Rects pointing at only one
            // copy — and if the two instances disagreed on resolvability, put the key in both
            // Rects and Skipped. Cheaper to reject than to define semantics for.
            if (!keys.Add(s.Key))
                throw new ArgumentException($"duplicate sprite key '{s.Key}'", paramName);

            if (s.UnresolvedReason is { } reason)
            {
                skipped.Add(new SkippedSprite(s.Key, s.Sheet, reason));
                continue;
            }

            var rect = s.Rect;

            if (!sheetSizes.TryGetValue(s.Sheet, out var size))
            {
                var probePath = manifest.SheetPath(s.Sheet);
                if (File.Exists(probePath))
                {
                    var info = ReadPng(probePath, () => Image.Identify(probePath));
                    size = (info.Width, info.Height);
                }
                else
                {
                    size = null;
                }

                sheetSizes[s.Sheet] = size;
            }

            if (size is not { } sheetSize)
            {
                skipped.Add(new SkippedSprite(
                    s.Key, s.Sheet,
                    $"sheet {s.Sheet} has no PNG at {manifest.SheetPath(s.Sheet)}"));
                continue;
            }

            // Guards the whole family, not just the overrun the client corpus actually exhibits:
            // a negative origin throws the same opaque ImageSharp range error, and a non-positive
            // extent would skip the copy loop silently while still recording a nonsense rect and
            // feeding a bogus row height to the packer.
            if (rect.X < 0 || rect.Y < 0 || rect.W <= 0 || rect.H <= 0 ||
                rect.X + rect.W > sheetSize.W || rect.Y + rect.H > sheetSize.H)
            {
                skipped.Add(new SkippedSprite(
                    s.Key, s.Sheet,
                    $"rect ({rect.X},{rect.Y},{rect.W},{rect.H}) does not fit sheet {s.Sheet} " +
                    $"({sheetSize.W}x{sheetSize.H})"));
                continue;
            }

            // Checked here rather than left to ShelfPacker, whose exception names an index into
            // this list that the caller has no way to map back to a sprite.
            if (rect.W > width)
            {
                skipped.Add(new SkippedSprite(
                    s.Key, s.Sheet,
                    $"sprite is {rect.W}px wide, wider than the {width}px atlas"));
                continue;
            }

            resolved.Add(s);
        }

        var packed = ShelfPacker.Pack(
            resolved.Select(r => (r.Rect.W, r.Rect.H)).ToList(), width);

        // An empty source list packs to height 0, and ImageSharp rejects a zero dimension.
        var atlas = new Image<Rgba32>(packed.Width, Math.Max(packed.Height, 1));
        var rects = new Dictionary<string, SpriteRect>(resolved.Count);

        try
        {
            // Group by sheet so each PNG is decoded once.
            foreach (var group in packed.Placements.GroupBy(p => resolved[p.Index].Sheet))
            {
                var sheetPath = manifest.SheetPath(group.Key);
                using var sheet = ReadPng(sheetPath, () => Image.Load<Rgba32>(sheetPath));

                foreach (var p in group)
                {
                    var src = resolved[p.Index].Rect;

                    for (int y = 0; y < src.H; y++)
                    for (int x = 0; x < src.W; x++)
                        atlas[p.X + x, p.Y + y] = sheet[src.X + x, src.Y + y];

                    rects[resolved[p.Index].Key] = new SpriteRect(p.X, p.Y, src.W, src.H);
                }
            }
        }
        catch
        {
            // At full-corpus scale the atlas is a pooled buffer on the order of a gigabyte, so
            // leaking it on a decode failure is not acceptable in a library method.
            atlas.Dispose();
            throw;
        }

        return new BuiltAtlas { Image = atlas, Rects = rects, Skipped = skipped };
    }

    /// <summary>Names the sheet in an unreadable-PNG failure, the way Manifest.Load names its
    /// file. ImageFormatException — the base of ImageSharp's unknown-format and invalid-content
    /// errors — is not an IOException, so it escaped the CLI's catch as an unhandled stack trace,
    /// and its message carries no path, so catching it up there could not have said which of
    /// thousands of sheets was bad.
    ///
    /// A truncated PNG is the realistic input: the sheets come from a separate client checkout and
    /// an interrupted fetch truncates rather than deletes, so the graceful skip for a missing file
    /// does not cover it. Both the header probe and the full decode are wrapped — the probe reads
    /// only the IHDR, so a file truncated inside the pixel data passes it and fails later.</summary>
    private static T ReadPng<T>(string path, Func<T> read)
    {
        try
        {
            return read();
        }
        catch (ImageFormatException ex)
        {
            throw new InvalidDataException($"{path} is not a readable PNG: {ex.Message}", ex);
        }
    }
}
