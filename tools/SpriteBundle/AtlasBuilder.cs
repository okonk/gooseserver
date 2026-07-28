using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Goose.Tools.SpriteBundle;

/// <summary>One sprite to pull into a bundle. Key is what the editor looks up by.</summary>
public readonly record struct SpriteRef(string Key, int Sheet, int Graphic);

/// <summary>A sprite that could not be packed, and why. The reason is carried rather than
/// inferred because the causes need different responses: a missing manifest entry usually means a
/// stale graphic id in the game data, whereas a rect that does not fit its own sheet is an
/// asset-pipeline bug. Collapsing them into a bare count hides the second behind the first.</summary>
public sealed record SkippedSprite(string Key, string Reason);

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
    public static BuiltAtlas Build(Manifest manifest, IReadOnlyList<SpriteRef> sources, int width)
    {
        var resolved = new List<(SpriteRef Ref, SpriteRect Rect)>(sources.Count);
        var skipped = new List<SkippedSprite>();
        var keys = new HashSet<string>(sources.Count);

        // Sheets are shared by many sprites (~104k sprites over ~7.4k sheets in the client
        // corpus), so probe each PNG's header once. Null means the file is absent.
        var sheetSizes = new Dictionary<int, (int W, int H)?>();

        foreach (var s in sources)
        {
            // A duplicate key would pack the sprite twice and leave Rects pointing at only one
            // copy — and if the two instances disagreed on resolvability, put the key in both
            // Rects and Skipped. Cheaper to reject than to define semantics for.
            if (!keys.Add(s.Key))
                throw new ArgumentException($"duplicate sprite key '{s.Key}'", nameof(sources));

            if (!manifest.TryGetRect(s.Sheet, s.Graphic, out var rect))
            {
                skipped.Add(new SkippedSprite(
                    s.Key, $"sheet {s.Sheet} has no graphic {s.Graphic} in the manifest"));
                continue;
            }

            if (!sheetSizes.TryGetValue(s.Sheet, out var size))
            {
                var probePath = manifest.SheetPath(s.Sheet);
                if (File.Exists(probePath))
                {
                    var info = Image.Identify(probePath);
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
                    s.Key, $"sheet {s.Sheet} has no PNG at {manifest.SheetPath(s.Sheet)}"));
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
                    s.Key,
                    $"rect ({rect.X},{rect.Y},{rect.W},{rect.H}) does not fit sheet {s.Sheet} " +
                    $"({sheetSize.W}x{sheetSize.H})"));
                continue;
            }

            // Checked here rather than left to ShelfPacker, whose exception names an index into
            // this list that the caller has no way to map back to a sprite.
            if (rect.W > width)
            {
                skipped.Add(new SkippedSprite(
                    s.Key, $"sprite is {rect.W}px wide, wider than the {width}px atlas"));
                continue;
            }

            resolved.Add((s, rect));
        }

        var packed = ShelfPacker.Pack(
            resolved.Select(r => (r.Rect.W, r.Rect.H)).ToList(), width);

        // An empty source list packs to height 0, and ImageSharp rejects a zero dimension.
        var atlas = new Image<Rgba32>(packed.Width, Math.Max(packed.Height, 1));
        var rects = new Dictionary<string, SpriteRect>(resolved.Count);

        try
        {
            // Group by sheet so each PNG is decoded once.
            foreach (var group in packed.Placements.GroupBy(p => resolved[p.Index].Ref.Sheet))
            {
                using var sheet = Image.Load<Rgba32>(manifest.SheetPath(group.Key));

                foreach (var p in group)
                {
                    var (sref, src) = resolved[p.Index];

                    for (int y = 0; y < src.H; y++)
                    for (int x = 0; x < src.W; x++)
                        atlas[p.X + x, p.Y + y] = sheet[src.X + x, src.Y + y];

                    rects[sref.Key] = new SpriteRect(p.X, p.Y, src.W, src.H);
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
}
