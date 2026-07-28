using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Goose.Tools.SpriteBundle;

/// <summary>One sprite to pull into a bundle. Key is what the editor looks up by.</summary>
public readonly record struct SpriteRef(string Key, int Sheet, int Graphic);

public sealed class BuiltAtlas : IDisposable
{
    public required Image<Rgba32> Image { get; init; }
    public required Dictionary<string, SpriteRect> Rects { get; init; }
    public required List<string> Skipped { get; init; }

    public void Dispose() => Image.Dispose();
}

/// <summary>Crops sprites out of the client's sheet PNGs and packs them into one atlas.
///
/// Uses direct pixel assignment rather than ctx.DrawImage: DrawImage is not pixel-exact, it
/// rewrites fully transparent pixels to Rgba32(0,0,0,0), discarding whatever RGB the source
/// carried under a zero alpha (the client's sheets do contain such pixels, e.g. Rgba32(1,0,0,0)
/// in sheet 20107). Invisible when drawn, but it defeats exact verification. The per-pixel loop
/// is fast enough not to matter: measured at roughly 40 MPx/s including PNG decode, so every
/// sprite in the client manifest at once (336 MPx) copies in about 8s.</summary>
public static class AtlasBuilder
{
    public static BuiltAtlas Build(Manifest manifest, IReadOnlyList<SpriteRef> sources, int width)
    {
        var resolved = new List<(SpriteRef Ref, SpriteRect Rect)>(sources.Count);
        var skipped = new List<string>();

        foreach (var s in sources)
        {
            if (manifest.TryGetRect(s.Sheet, s.Graphic, out var rect) &&
                File.Exists(manifest.SheetPath(s.Sheet)))
                resolved.Add((s, rect));
            else
                skipped.Add(s.Key);
        }

        var packed = ShelfPacker.Pack(
            resolved.Select(r => (r.Rect.W, r.Rect.H)).ToList(), width);

        // An empty source list packs to height 0, and ImageSharp rejects a zero dimension.
        var atlas = new Image<Rgba32>(packed.Width, Math.Max(packed.Height, 1));
        var rects = new Dictionary<string, SpriteRect>(resolved.Count);

        // Group by sheet so each PNG is decoded once — sheets are shared by many sprites.
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

        return new BuiltAtlas { Image = atlas, Rects = rects, Skipped = skipped };
    }
}
