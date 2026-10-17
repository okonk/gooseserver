using Goose;

namespace Goose.Tests;

public class MapWarpTilesTests
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

    [Fact]
    public void GetWarpTiles_ReturnsOnlyWarpPositions()
    {
        var m = NewMap();
        m.SetTile(2, 3, new WarpTile());
        m.SetTile(10, 1, new WarpTile());
        m.SetTile(4, 4, new BlockedTile());

        var tiles = m.GetWarpTiles();

        Assert.Equal(new[] { (10, 1), (2, 3) }, tiles.Select(t => (t.X, t.Y)));
    }

    [Fact]
    public void GetWarpTiles_IncludesFarEdgeTiles()
    {
        var m = NewMap();
        m.SetTile(Width, Height, new WarpTile());

        var tiles = m.GetWarpTiles();

        Assert.Equal(new[] { (Width, Height) }, tiles.Select(t => (t.X, t.Y)));
    }

    [Fact]
    public void GetWarpTiles_EntryHoldsPlacedInstance()
    {
        var m = NewMap();
        var placed = new WarpTile();
        m.SetTile(7, 7, placed);

        var entry = m.GetWarpTiles().Single();

        Assert.Same(placed, entry.Tile);
    }

    [Fact]
    public void GetWarpTiles_SeesWarpAddedAfterCachedRead()
    {
        var m = NewMap();
        Assert.Empty(m.GetWarpTiles());

        m.SetTile(5, 5, new WarpTile());

        Assert.Equal(new[] { (5, 5) }, m.GetWarpTiles().Select(t => (t.X, t.Y)));
    }

    [Fact]
    public void GetWarpTiles_DropsWarpOverwrittenAfterCachedRead()
    {
        var m = NewMap();
        m.SetTile(5, 5, new WarpTile());
        Assert.Single(m.GetWarpTiles());

        m.SetTile(5, 5, new BlockedTile());

        Assert.Empty(m.GetWarpTiles());
    }

    private static ItemTile MakeItemTile(int x, int y, GameWorld world)
    {
        var template = new ItemTemplate
        {
            ID = 42, Name = "Sword", Description = "A sword",
            BaseStats = new AttributeSet { HP = 5 },
        };
        world.ItemHandler.AddTemplate(template);
        var item = new Item();
        item.LoadFromTemplate(template);
        return new ItemTile
        {
            X = x, Y = y,
            ItemSlot = new ItemSlot { Item = item, Stack = 1 },
        };
    }

    [Fact]
    public void GetWarpTiles_DropsWarpOverwrittenByAddItem()
    {
        var m = NewMap();
        m.SetTile(5, 5, new WarpTile());
        Assert.Single(m.GetWarpTiles());

        var world = new GameWorld(new GooseSettings());
        m.AddItem(MakeItemTile(5, 5, world), world);

        Assert.Empty(m.GetWarpTiles());
    }

    [Fact]
    public void GetWarpTiles_SharedInstanceReportsEachPosition()
    {
        var m = NewMap();
        var warp = new WarpTile();
        m.SetTile(1, 1, warp);
        m.SetTile(3, 1, warp);

        var tiles = m.GetWarpTiles();

        Assert.Equal(new[] { (1, 1), (3, 1) }, tiles.Select(t => (t.X, t.Y)));
        Assert.All(tiles, t => Assert.Same(warp, t.Tile));
    }

    [Fact]
    public void CloneAs_WarpCacheIsIndependent()
    {
        var source = NewMap();
        source.SetTile(2, 2, new WarpTile());
        Assert.Single(source.GetWarpTiles());

        var clone = source.CloneAs(2, "Test (1)");
        clone.SetTile(6, 6, new WarpTile());

        // tiles is a shallow copy, so the clone's fresh scan also sees the shared warp at (2,2).
        Assert.Equal(new[] { (2, 2), (6, 6) }, clone.GetWarpTiles().Select(t => (t.X, t.Y)));
        Assert.Equal(new[] { (2, 2) }, source.GetWarpTiles().Select(t => (t.X, t.Y)));
    }
}
