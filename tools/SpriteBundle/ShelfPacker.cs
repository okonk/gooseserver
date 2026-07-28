namespace Goose.Tools.SpriteBundle;

public readonly record struct Placement(int Index, int X, int Y);

public sealed record PackResult(int Width, int Height, IReadOnlyList<Placement> Placements);

/// <summary>Shelf (row) packing: sort tallest-first, fill a fixed-width row left to right,
/// start a new row when a sprite will not fit. Row height is its tallest sprite, so sorting
/// tallest-first leaves only the ragged right edge of each row unused — close enough to optimal
/// that a full bin packer is not worth the dependency.</summary>
public static class ShelfPacker
{
    public static PackResult Pack(IReadOnlyList<(int W, int H)> sizes, int width)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width), width, "atlas width must be positive");

        var order = Enumerable.Range(0, sizes.Count)
            .OrderByDescending(i => sizes[i].H)
            .ThenBy(i => i)          // stable, so output is deterministic
            .ToList();

        var placements = new List<Placement>(sizes.Count);
        int x = 0, y = 0, rowHeight = 0;

        foreach (var i in order)
        {
            var (w, h) = sizes[i];
            if (w > width)
                throw new ArgumentException(
                    $"sprite {i} is {w}px wide, wider than the {width}px atlas");

            if (x + w > width)
            {
                x = 0;
                y += rowHeight;
                rowHeight = 0;
            }

            placements.Add(new Placement(i, x, y));
            x += w;
            if (h > rowHeight) rowHeight = h;
        }

        return new PackResult(width, y + rowHeight, placements);
    }
}
