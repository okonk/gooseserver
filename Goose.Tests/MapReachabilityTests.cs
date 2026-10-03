namespace Goose.Tests;

public class MapReachabilityTests
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
    public void CanReachTile_GapInWall_ReturnsTrue()
    {
        var m = NewMap();
        BuildWallWithDoorway(m, wallX: 10, doorY: 10, new BlockedTile());

        Assert.True(m.CanReachTile(5, 5, 11, 10, radius: 1));
    }

    [Fact]
    public void CanReachTile_SealedPocket_ReturnsFalse()
    {
        var m = NewMap();
        BuildRing(m, 17, 17, new BlockedTile());

        Assert.False(m.CanReachTile(5, 5, 17, 17, radius: 1));
    }

    [Fact]
    public void CanReachTile_PortalOnlyRoute_ReturnsFalse()
    {
        var m = NewMap();
        BuildRing(m, 17, 17, new WarpTile());

        Assert.False(m.CanReachTile(5, 5, 17, 17, radius: 1));
    }

    [Fact]
    public void CanReachTile_AggroBoxBoundary_ReturnsFalseBeyondBox()
    {
        var m = new Map { ID = 1, Name = "Test", Width = 60, Height = 40 };
        m.characters = new ICharacter[(m.Width + 1) * (m.Height + 1)];
        m.tiles = new ITile[(m.Width + 1) * (m.Height + 1)];

        Assert.True(m.CanReachTile(30, 20, 53, 20, radius: 0));
        Assert.False(m.CanReachTile(30, 20, 54, 20, radius: 0));
        Assert.True(m.CanReachTile(30, 20, 30, 35, radius: 0));
        Assert.False(m.CanReachTile(30, 20, 30, 36, radius: 0));
    }

    [Fact]
    public void CanReachTile_CharactersArePassThrough_ReturnsTrue()
    {
        var m = NewMap();
        BuildWallWithDoorway(m, wallX: 10, doorY: 10, new BlockedTile());
        m.SetCharacter(new Player(0), 10, 10);

        Assert.True(m.CanReachTile(5, 5, 11, 10, radius: 1));
    }
}
