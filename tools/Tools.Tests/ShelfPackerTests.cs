using Goose.Tools.SpriteBundle;

namespace Tools.Tests;

public class ShelfPackerTests
{
    private static IReadOnlyList<(int W, int H)> Sizes(params (int, int)[] s) => s;

    [Fact]
    public void Places_tallest_first_and_starts_new_row_when_full()
    {
        var packed = ShelfPacker.Pack(Sizes((60, 10), (60, 20), (60, 30)), width: 120);

        // Sorted tallest-first: 30, 20, 10. First two share row 0, third wraps.
        // Placements are emitted in packing order, so the indices run tallest-first (2, 1, 0),
        // not input order.
        Assert.Equal(new[] { 2, 1, 0 }, packed.Placements.Select(p => p.Index).ToArray());
        Assert.Equal((0, 0), (packed.Placements[0].X, packed.Placements[0].Y));
        Assert.Equal((60, 0), (packed.Placements[1].X, packed.Placements[1].Y));
        Assert.Equal((0, 30), (packed.Placements[2].X, packed.Placements[2].Y));
        Assert.Equal(120, packed.Width);
        Assert.Equal(40, packed.Height);
    }

    [Fact]
    public void Placements_report_original_indices()
    {
        // Input order short, tall. Packing sorts tall first, but placements must map back.
        var packed = ShelfPacker.Pack(Sizes((10, 5), (10, 50)), width: 100);

        var tall = packed.Placements.Single(p => p.Index == 1);
        Assert.Equal((0, 0), (tall.X, tall.Y));

        var shortOne = packed.Placements.Single(p => p.Index == 0);
        Assert.Equal(10, shortOne.X);
        Assert.Equal(0, shortOne.Y);
    }

    [Fact]
    public void No_two_sprites_overlap()
    {
        var rng = new Random(1234);
        var sizes = Enumerable.Range(0, 500)
            .Select(_ => (rng.Next(8, 100), rng.Next(8, 120)))
            .ToList();

        var packed = ShelfPacker.Pack(sizes, width: 2048);

        var occupied = new HashSet<(int, int)>();
        foreach (var p in packed.Placements)
        {
            var (w, h) = sizes[p.Index];
            for (int y = p.Y; y < p.Y + h; y++)
            for (int x = p.X; x < p.X + w; x++)
                Assert.True(occupied.Add((x, y)), $"overlap at {x},{y}");
        }
    }

    [Fact]
    public void Nothing_exceeds_the_configured_width()
    {
        var rng = new Random(99);
        var sizes = Enumerable.Range(0, 300).Select(_ => (rng.Next(8, 200), rng.Next(8, 80))).ToList();

        var packed = ShelfPacker.Pack(sizes, width: 512);

        foreach (var p in packed.Placements)
            Assert.True(p.X + sizes[p.Index].Item1 <= 512);
    }

    [Fact]
    public void Achieves_high_area_efficiency_on_uniform_input()
    {
        var sizes = Enumerable.Repeat((32, 32), 4096).ToList();

        var packed = ShelfPacker.Pack(sizes, width: 2048);

        var used = sizes.Sum(s => s.Item1 * s.Item2);
        var total = packed.Width * packed.Height;
        Assert.True(used / (double)total > 0.99, $"efficiency {used / (double)total:P1}");
    }

    [Fact]
    public void Empty_input_produces_empty_atlas()
    {
        var packed = ShelfPacker.Pack(Array.Empty<(int, int)>(), width: 2048);

        Assert.Empty(packed.Placements);
        Assert.Equal(0, packed.Height);
    }

    /// <summary>A sprite wider than the atlas can never be placed; the guard must name the
    /// offending sprite rather than emitting an out-of-bounds placement.</summary>
    [Fact]
    public void Sprite_wider_than_the_atlas_is_rejected()
    {
        var e = Assert.Throws<ArgumentException>(
            () => ShelfPacker.Pack(Sizes((10, 10), (600, 10)), width: 512));

        Assert.Contains("sprite 1", e.Message);
        Assert.Contains("600", e.Message);
        Assert.Contains("512", e.Message);
    }
}
