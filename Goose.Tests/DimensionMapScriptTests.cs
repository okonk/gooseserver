using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionMapScriptTests
{
    [Fact]
    public void Refuses_players_below_the_required_dimension()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var map = fixture.AddBaseMap(300001, "Town (3)");
        map.ScriptParams = "3";

        var player = new Player(0);
        player.Properties["dimension.max"] = 1;

        var refusal = script.Object.CanPlayerJoin(map, player, fixture.World);

        Assert.Equal("The void has rejected you. You have a maximum dimension of 1.", refusal);
    }

    [Fact]
    public void Allows_players_at_or_above_the_required_dimension()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var map = fixture.AddBaseMap(300001, "Town (3)");
        map.ScriptParams = "3";

        var player = new Player(0);
        player.Properties["dimension.max"] = 3;

        Assert.Null(script.Object.CanPlayerJoin(map, player, fixture.World));
    }

    [Fact]
    public void Players_with_no_progress_default_to_dimension_zero()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var map = fixture.AddBaseMap(100001, "Town (1)");
        map.ScriptParams = "1";

        Assert.NotNull(script.Object.CanPlayerJoin(map, new Player(0), fixture.World));
    }

    // ---- The login and bind clamps ------------------------------------------------

    [Fact]
    public void Entering_a_locked_dimension_map_relocates_the_player_to_dimension_zero()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim3 = fixture.AddBaseMap(300001, "Town (3)", width: 100, height: 100);
        dim3.ScriptParams = "3";

        var player = PlayerOn(dim3, x: 50, y: 50);
        player.Properties["dimension.max"] = 0;

        script.Object.OnPlayerEntered(dim3, player, fixture.World);

        Assert.Equal(1, player.MapID);   // ORCHESTRATOR CORRECTION: assert MapID, not Map.ID (see note below)
    }

    /// <summary>The design calls for clamping bound_id as well as map_id. Without this a
    /// player whose progress is reduced keeps a bind inside a locked dimension and returns
    /// there every time they die (Player.cs:1775).</summary>
    [Fact]
    public void A_bind_inside_a_locked_dimension_is_clamped_to_dimension_zero()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var town = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim3 = fixture.AddBaseMap(300001, "Town (3)", width: 100, height: 100);
        dim3.ScriptParams = "3";

        var player = PlayerOn(dim3, x: 50, y: 50);
        player.Properties["dimension.max"] = 0;
        player.BoundID = 300001;
        player.BoundMap = dim3;
        player.BoundX = 40;
        player.BoundY = 40;

        script.Object.OnPlayerEntered(dim3, player, fixture.World);

        Assert.Equal(1, player.BoundID);
        Assert.Same(town, player.BoundMap);
        Assert.Equal(40, player.BoundX);      // coordinates survive; only the map changes
        Assert.Equal(40, player.BoundY);
    }

    [Fact]
    public void A_bind_the_player_still_has_access_to_is_left_alone()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim3 = fixture.AddBaseMap(300001, "Town (3)", width: 100, height: 100);
        dim3.ScriptParams = "3";

        var player = PlayerOn(dim3, x: 50, y: 50);
        player.Properties["dimension.max"] = 3;
        player.BoundID = 300001;
        player.BoundMap = dim3;

        script.Object.OnPlayerEntered(dim3, player, fixture.World);

        Assert.Equal(300001, player.BoundID);
        Assert.Same(dim3, player.BoundMap);
    }

    /// <summary>Binds are clamped even when the map being entered is fine - a player can
    /// walk into dimension 0 carrying a dimension-5 bind.</summary>
    [Fact]
    public void A_locked_bind_is_clamped_even_when_the_current_map_is_allowed()
    {
        using var fixture = new GlobalScriptFixture();
        var script = fixture.CompileShippedMapScript();
        var town = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        var dim1 = fixture.AddBaseMap(100001, "Town (1)", width: 100, height: 100);
        dim1.ScriptParams = "1";
        var dim5 = fixture.AddBaseMap(500001, "Town (5)", width: 100, height: 100);
        dim5.ScriptParams = "5";

        var player = PlayerOn(dim1, x: 50, y: 50);
        player.Properties["dimension.max"] = 1;
        player.BoundID = 500001;
        player.BoundMap = dim5;

        script.Object.OnPlayerEntered(dim1, player, fixture.World);

        Assert.Equal(100001, player.MapID);   // ORCHESTRATOR CORRECTION: the current map was allowed - no relocation; player stays on dim1 (100001). The plan's "Assert.Equal(1, player.Map.ID)" was a bug - the player was placed on map 100001.
        Assert.Equal(1, player.BoundID);      // but the bind was not
        Assert.Same(town, player.BoundMap);
    }

    /// <summary>Builds a Player already placed on a map - the minimum for WarpTo to work in
    /// a test world. WarpTo needs player.Map, MapID, MapX, MapY set, and the source map's
    /// characters array sized (AddBaseMap does that).</summary>
    private static Player PlayerOn(Map map, int x, int y)
    {
        return new Player(0)
        {
            Map = map,
            MapID = map.ID,
            MapX = x,
            MapY = y,
        };
    }
}
