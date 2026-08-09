using Goose;

namespace Goose.Tests;

public class MapCloneTests
{
    private static Map MakeBase()
    {
        var map = new Map
        {
            ID = 1, Name = "Town", FileName = "Map1.map", Width = 10, Height = 10,
            MinLevel = 5, MaxLevel = 20, MinExperience = 100, MaxExperience = 200,
            CanPVP = false, CanChat = true, CanBind = true, Muted = true,
            ScriptParams = "base-params",
            tiles = new ITile[11 * 11],
            characters = new ICharacter[11 * 11],
        };
        map.SetTile(3, 3, new BlockedTile());
        return map;
    }

    [Fact]
    public void Copies_every_map_setting_including_Muted()
    {
        var clone = MakeBase().CloneAs(100001, "Town (1)");

        Assert.Equal(100001, clone.ID);
        Assert.Equal("Town (1)", clone.Name);
        Assert.Equal("Map1.map", clone.FileName);
        Assert.Equal(10, clone.Width);
        Assert.Equal(10, clone.Height);
        Assert.Equal(5, clone.MinLevel);
        Assert.Equal(20, clone.MaxLevel);
        Assert.Equal(100, clone.MinExperience);
        Assert.Equal(200, clone.MaxExperience);
        Assert.True(clone.CanChat);
        Assert.True(clone.CanBind);
        Assert.True(clone.Muted);
        Assert.Equal("base-params", clone.ScriptParams);
    }

    /// <summary>The reason this API exists: requiredItems is private, so a clone
    /// assembled from public fields would bypass item-gated entry entirely.</summary>
    [Fact]
    public void Copies_required_items_without_sharing_the_list()
    {
        var basic = MakeBase();
        basic.AddRequiredItem(1234);

        var clone = basic.CloneAs(100001, "Town (1)");
        clone.AddRequiredItem(5678);

        Assert.Equal(new[] { 1234 }, basic.RequiredItems);
        Assert.Equal(new[] { 1234, 5678 }, clone.RequiredItems);
    }

    [Fact]
    public void Gives_the_clone_its_own_occupancy_state_but_shares_tiles()
    {
        var basic = MakeBase();
        var clone = basic.CloneAs(100001, "Town (1)");

        Assert.NotSame(basic.characters, clone.characters);
        Assert.Equal(basic.characters.Length, clone.characters.Length);
        Assert.NotSame(basic.Players, clone.Players);
        Assert.NotSame(basic.NPCs, clone.NPCs);
        Assert.NotSame(basic.Items, clone.Items);
        Assert.Empty(clone.Players);
        Assert.Empty(clone.NPCs);

        // tiles is a new array holding the same tile objects - BlockedTile is a stateless
        // marker (BlockedTile.cs:8) and WarpTiles get replaced by the caller.
        Assert.NotSame(basic.tiles, clone.tiles);
        Assert.Same(basic.GetTile(3, 3), clone.GetTile(3, 3));
    }
}
