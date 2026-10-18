namespace Goose.Tests;

public class MapPathfindingTests
{
    private const int Width = 20;
    private const int Height = 20;

    private static Map NewMap()
    {
        var m = new Map { ID = 1, Name = "Test", Width = Width, Height = Height };
        m.characters = new ICharacter[(m.Width + 1) * (m.Height + 1)];
        m.tiles = new ITile[(m.Width + 1) * (m.Height + 1)];
        return m;
    }

    private static void BuildWallWithDoorway(Map m, int wallX, int doorY, ITile tile)
    {
        for (int y = 1; y <= Height; y++)
        {
            if (y == doorY) continue;
            m.SetTile(wallX, y, tile);
        }
    }

    private static void BuildRing(Map m, int cx, int cy, ITile tile)
    {
        for (int y = cy - 1; y <= cy + 1; y++)
        {
            for (int x = cx - 1; x <= cx + 1; x++)
            {
                if (x == cx && y == cy) continue;
                m.SetTile(x, y, tile);
            }
        }
    }

    [Fact]
    public void Open_map_returns_shortest_path_length()
    {
        var m = NewMap();

        var path = m.FindPath(2, 2, 6, 5);

        Assert.NotNull(path);
        Assert.Equal(7, path.Count);
        Assert.Equal((6, 5), path[^1]);

        int px = 2;
        int py = 2;
        foreach (var (X, Y) in path)
        {
            Assert.Equal(1, Math.Abs(X - px) + Math.Abs(Y - py));
            px = X;
            py = Y;
        }
    }

    [Fact]
    public void Routes_around_a_wall_through_the_doorway()
    {
        var m = NewMap();
        BuildWallWithDoorway(m, wallX: 10, doorY: 15, new BlockedTile());

        var path = m.FindPath(5, 5, 15, 5);

        Assert.NotNull(path);
        Assert.Contains((10, 15), path);
        foreach (var (X, Y) in path)
        {
            Assert.False(m.IsTileStaticBlocked(X, Y));
        }
    }

    [Fact]
    public void Sealed_goal_returns_null()
    {
        var m = NewMap();
        BuildRing(m, 10, 10, new BlockedTile());

        Assert.Null(m.FindPath(5, 5, 10, 10));
    }

    [Fact]
    public void Statically_blocked_goal_returns_null()
    {
        var m = NewMap();
        m.SetTile(10, 10, new BlockedTile());

        Assert.Null(m.FindPath(5, 5, 10, 10));
    }

    [Fact]
    public void Same_tile_returns_empty_list()
    {
        var m = NewMap();

        var path = m.FindPath(5, 5, 5, 5);

        Assert.NotNull(path);
        Assert.Empty(path);
    }

    [Fact]
    public void Predicate_blocks_tiles_but_not_the_goal()
    {
        var m = NewMap();
        BuildWallWithDoorway(m, wallX: 10, doorY: 15, new BlockedTile());

        var path = m.FindPath(5, 5, 10, 15, isBlocked: (x, y) => x == 10);

        Assert.NotNull(path);
        Assert.Equal((10, 15), path[^1]);

        Assert.Null(m.FindPath(5, 5, 10, 15, isBlocked: (x, y) => x == 10 || (x == 9 && y == 15)));
    }

    [Fact]
    public void WarpTile_is_impassable()
    {
        var m = NewMap();
        m.SetTile(10, 5, new WarpTile());

        var path = m.FindPath(5, 5, 15, 5);

        Assert.NotNull(path);
        Assert.DoesNotContain((10, 5), path);
    }

    [Fact]
    public void Expansion_limit_returns_null()
    {
        var m = NewMap();

        Assert.Null(m.FindPath(1, 1, 20, 20, maxExpanded: 5));
    }

    [Fact]
    public void Same_inputs_give_same_path()
    {
        var m = NewMap();
        BuildWallWithDoorway(m, wallX: 10, doorY: 15, new BlockedTile());

        var a = m.FindPath(5, 5, 15, 5);
        var b = m.FindPath(5, 5, 15, 5);

        Assert.NotNull(a);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Out_of_bounds_endpoint_returns_null()
    {
        var m = NewMap();

        Assert.Null(m.FindPath(0, 5, 10, 10));
        Assert.Null(m.FindPath(21, 5, 10, 10));
        Assert.Null(m.FindPath(5, 5, 10, 0));
        Assert.Null(m.FindPath(5, 5, 10, 21));
    }
}
