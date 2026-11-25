using System.Text.Json;
using Goose.Testing;

namespace Goose.Tests.Spells;

public class ShapeTilesTests
{
    public sealed record ShapeCase(string Name, string Shape, int Size, string Facing, bool Legacy, int[][] Tiles)
    {
        public SpellEffect.TargetTypes TargetType => Enum.Parse<SpellEffect.TargetTypes>(Shape);
        public Direction Direction => Enum.Parse<Direction>(Facing);
        public List<(int X, int Y)> Expected => Tiles.Select(t => (t[0], t[1])).ToList();
    }

    public static IReadOnlyList<ShapeCase> Cases { get; } = JsonSerializer.Deserialize<List<ShapeCase>>(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Spells", "shape-tiles.json")),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    private static List<(int X, int Y)> LegacyCastTiles(SpellEffect.TargetTypes shape, int size, Direction facing,
        double randomJoinChance = 0)
    {
        using var fixture = new TestWorldFixture();
        var map = fixture.AddBaseMap(1, "Field", 40, 40);
        var effect = fixture.AddBaseSpellEffect(1, "Shape", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Formula;
            e.Effected = SpellEffect.SpellEffected.NPC;
            e.Display = SpellEffect.SpellDisplays.Tile;
            e.Animation = 77;
            e.AnimationFile = 3;
            e.TargetType = shape;
            e.TargetSize = size;
            e.WorksInPVP = true;
            e.RandomJoinChance = randomJoinChance;
        });
        var caster = fixture.CommandPlayerOn(map, 30, 30, "Caster");
        caster.Facing = facing;
        var target = fixture.CommandPlayerOn(map, 10, 10, "Target");

        effect.Cast(caster, target, fixture.World);

        return string.Concat(target.Sent).Split('\x1')
            .Where(p => p.StartsWith("SPA"))
            .Select(p => p.Substring(3).Split(','))
            .Select(f => (int.Parse(f[0]), int.Parse(f[1])))
            .ToList();
    }

    [Fact]
    public void Legacy_cast_hits_the_recorded_tiles_in_order()
    {
        foreach (var c in Cases.Where(c => c.Legacy))
            Assert.True(c.Expected.SequenceEqual(LegacyCastTiles(c.TargetType, c.Size, c.Direction)),
                $"{c.Name}: got {string.Join(" ", LegacyCastTiles(c.TargetType, c.Size, c.Direction))}");
    }

    [Fact]
    public void Legacy_random_with_no_join_chance_is_only_the_anchor()
    {
        Assert.Equal([(10, 10)], LegacyCastTiles(SpellEffect.TargetTypes.Random, 2, Direction.Down, 0));
    }

    [Fact]
    public void Legacy_random_with_full_join_chance_fills_the_square()
    {
        var tiles = LegacyCastTiles(SpellEffect.TargetTypes.Random, 2, Direction.Down, 100);

        Assert.Equal(25, tiles.Count);
        Assert.Equal((10, 10), tiles[0]);
        Assert.All(tiles, t => Assert.InRange(t.X, 8, 12));
        Assert.All(tiles, t => Assert.InRange(t.Y, 8, 12));
        Assert.Equal(25, tiles.Distinct().Count());
    }
}
