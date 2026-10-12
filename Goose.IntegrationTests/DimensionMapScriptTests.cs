using Goose.Scripting;
using Goose.IntegrationTests.Fixtures;
using Goose.Testing;

namespace Goose.IntegrationTests;

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

        var player = fixture.PlayerOn(dim3, x: 50, y: 50);
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

        var player = fixture.PlayerOn(dim3, x: 50, y: 50);
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

        var player = fixture.PlayerOn(dim3, x: 50, y: 50);
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

        var player = fixture.PlayerOn(dim1, x: 50, y: 50);
        player.Properties["dimension.max"] = 1;
        player.BoundID = 500001;
        player.BoundMap = dim5;

        script.Object.OnPlayerEntered(dim1, player, fixture.World);

        Assert.Equal(100001, player.MapID);   // ORCHESTRATOR CORRECTION: the current map was allowed - no relocation; player stays on dim1 (100001). The plan's "Assert.Equal(1, player.Map.ID)" was a bug - the player was placed on map 100001.
        Assert.Equal(1, player.BoundID);      // but the bind was not
        Assert.Same(town, player.BoundMap);
    }

    // ---- Delegation to the base map's script ----------------------------------------

    [Fact]
    public void Forwards_to_the_base_maps_script()
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Arena", width: 100, height: 100);
        var inner = new RecordingMapScript();
        basic.Script = ScriptStub.For<IMapScript>(inner);
        basic.ScriptParams = "inner-params";

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.MapHandler.GetMap(100001)!;
        var player = fixture.PlayerOn(clone, 5, 5);
        player.Properties["dimension.max"] = 6;

        clone.Script!.Object.OnPlayerEntered(clone, player, fixture.World);

        Assert.Equal(1, inner.EnteredCalls);
        // The dimension now comes from the map id, so ScriptParams carries the base map's.
        Assert.Equal("inner-params", clone.ScriptParams);
    }

    [Fact]
    public void A_refusal_from_the_base_script_still_blocks_entry()
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Arena", width: 100, height: 100);
        basic.Script = ScriptStub.For<IMapScript>(new RecordingMapScript { Refusal = "Arena is closed." });

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.MapHandler.GetMap(300001)!;
        var player = fixture.PlayerOn(clone, 5, 5);
        player.Properties["dimension.max"] = 6;   // the dimension gate would allow this

        Assert.Equal("Arena is closed.", clone.Script!.Object.CanPlayerJoin(clone, player, fixture.World));
    }

    [Fact]
    public void The_dimension_gate_still_wins_over_a_permissive_base_script()
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Arena", width: 100, height: 100);
        basic.Script = ScriptStub.For<IMapScript>(new RecordingMapScript());

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.MapHandler.GetMap(300001)!;
        var player = fixture.PlayerOn(clone, 5, 5);
        player.Properties["dimension.max"] = 1;

        Assert.Contains("maximum dimension", clone.Script!.Object.CanPlayerJoin(clone, player, fixture.World));
    }

    [Fact]
    public void A_GM_entering_a_locked_dimension_map_still_triggers_the_base_script()
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Arena", width: 100, height: 100);
        var inner = new RecordingMapScript();
        basic.Script = ScriptStub.For<IMapScript>(inner);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.MapHandler.GetMap(100001)!;   // dimension 1
        var player = fixture.PlayerOn(clone, 5, 5);
        player.Properties["dimension.max"] = 0;   // dim 1 > max 0: the gate would refuse a normal player
        player.Access = Player.AccessStatus.GameMaster;   // ...but GMs bypass it (AccessLevels.cs)
        Assert.True(player.HasPrivilege(AccessPrivilege.IgnoreMapRequirements));

        clone.Script!.Object.OnPlayerEntered(clone, player, fixture.World);

        // The GM is not relocated, but the base script still sees the entry.
        Assert.Equal(1, inner.EnteredCalls);
        Assert.Equal(100001, player.MapID);
    }

    [Fact]
    public void A_rejected_entry_is_not_forwarded_to_the_base_script()
    {
        using var fixture = new GlobalScriptFixture();
        var basic = fixture.AddBaseMap(1, "Arena", width: 100, height: 100);
        var inner = new RecordingMapScript();
        basic.Script = ScriptStub.For<IMapScript>(inner);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        var clone = fixture.World.MapHandler.GetMap(300001)!;   // dimension 3
        var player = fixture.PlayerOn(clone, 5, 5);
        player.Properties["dimension.max"] = 1;   // dim 3 > max 1: the gate refuses

        clone.Script!.Object.OnPlayerEntered(clone, player, fixture.World);

        // The base script must not see an entry that was warped straight back out.
        Assert.Equal(0, inner.EnteredCalls);
        Assert.Equal(1, player.MapID);   // relocated to the dimension-0 map (MapID, not Map - WarpTo nulls Map)
    }

    // ---- Map key (required item) gate ---------------------------------------------

    private const int Offset = 100000;
    private const int KeyId = 5001;

    private static (GlobalScriptFixture Fixture, Map Base, Map Clone) KeyGated(int dim = 3)
    {
        var fixture = new GlobalScriptFixture();
        var baseMap = fixture.AddBaseMap(1, "Vault", width: 100, height: 100);
        fixture.AddBaseItemTemplate(KeyId, "Vault Key", ItemTemplate.UseTypes.NoUse);
        baseMap.AddRequiredItem(KeyId);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);

        return (fixture, baseMap, fixture.World.MapHandler.GetMap(1 + Offset * dim)!);
    }

    private static GlobalScriptFixture.CapturingPlayer KeyPlayer(GlobalScriptFixture fixture, Map map, ItemTemplate? key)
    {
        var player = fixture.CommandPlayerOn(map, 5, 5);
        player.Bank = new PlayerBank();
        player.Properties["dimension.max"] = 6;
        if (key is not null)
            player.Inventory.AddItem(new Item { Template = key }, 1, fixture.World);
        return player;
    }

    [Fact]
    public void A_dimension_map_key_gate_accepts_the_same_or_higher_dimension_key()
    {
        var (fixture, _, clone) = KeyGated(dim: 3);
        using var _ = fixture;

        var withDim3 = KeyPlayer(fixture, clone, fixture.World.ItemHandler.GetTemplateVariant(KeyId, 3)!);
        Assert.True(clone.PlayerCanJoin(withDim3, fixture.World));

        var withDim4 = KeyPlayer(fixture, clone, fixture.World.ItemHandler.GetTemplateVariant(KeyId, 4)!);
        Assert.True(clone.PlayerCanJoin(withDim4, fixture.World));
    }

    [Fact]
    public void A_dimension_map_key_gate_rejects_base_and_lower_dimension_keys()
    {
        var (fixture, _, clone) = KeyGated(dim: 3);
        using var _ = fixture;

        var withBase = KeyPlayer(fixture, clone, fixture.World.ItemHandler.GetTemplate(KeyId)!);
        Assert.False(clone.PlayerCanJoin(withBase, fixture.World));

        var withDim2 = KeyPlayer(fixture, clone, fixture.World.ItemHandler.GetTemplateVariant(KeyId, 2)!);
        Assert.False(clone.PlayerCanJoin(withDim2, fixture.World));
    }

    [Fact]
    public void A_dimension_map_key_in_an_equipped_slot_or_the_combine_bag_opens_the_map()
    {
        var (fixture, _, clone) = KeyGated(dim: 3);
        using var _ = fixture;
        var key = fixture.World.ItemHandler.GetTemplateVariant(KeyId, 3)!;

        var equipped = KeyPlayer(fixture, clone, null);
        equipped.Inventory.GetEquippedSlots()[1] = new ItemSlot { Item = new Item { Template = key }, Stack = 1 };
        Assert.True(clone.PlayerCanJoin(equipped, fixture.World));

        var combined = KeyPlayer(fixture, clone, null);
        combined.Inventory.GetCombineBagContainer().SetSlot(1, new ItemSlot { Item = new Item { Template = key }, Stack = 1 });
        Assert.True(clone.PlayerCanJoin(combined, fixture.World));
    }

    [Fact]
    public void A_key_refusal_names_the_required_item()
    {
        var (fixture, _, clone) = KeyGated(dim: 3);
        using var _ = fixture;

        var player = KeyPlayer(fixture, clone, fixture.World.ItemHandler.GetTemplateVariant(KeyId, 2)!);
        Assert.False(clone.PlayerCanJoin(player, fixture.World));
        Assert.Contains(player.Sent, m => m.Contains("'Vault Key'"));
    }

    [Fact]
    public void The_base_map_still_requires_the_exact_base_key()
    {
        var (fixture, baseMap, _) = KeyGated(dim: 3);
        using var _ = fixture;

        var withDimKey = KeyPlayer(fixture, baseMap, fixture.World.ItemHandler.GetTemplateVariant(KeyId, 3)!);
        Assert.False(baseMap.PlayerCanJoin(withDimKey, fixture.World));

        var withBaseKey = KeyPlayer(fixture, baseMap, fixture.World.ItemHandler.GetTemplate(KeyId)!);
        Assert.True(baseMap.PlayerCanJoin(withBaseKey, fixture.World));
    }

    [Fact]
    public void A_refusal_from_the_delegated_base_script_wins_before_the_key_and_experience_gates()
    {
        var fixture = new GlobalScriptFixture();
        var baseMap = fixture.AddBaseMap(1, "Vault", width: 100, height: 100);
        baseMap.Script = ScriptStub.For<IMapScript>(new RecordingMapScript { Refusal = "Vault is closed." });
        fixture.AddBaseItemTemplate(KeyId, "Vault Key", ItemTemplate.UseTypes.NoUse);
        baseMap.AddRequiredItem(KeyId);

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        using var _ = fixture;

        var clone = fixture.World.MapHandler.GetMap(1 + Offset * 3)!;
        clone.MinExperience = 1_000_000;   // the keyless player below also fails this

        var player = KeyPlayer(fixture, clone, null);
        Assert.False(clone.PlayerCanJoin(player, fixture.World));
        Assert.Contains(player.Sent, m => m.Contains("Vault is closed."));
        Assert.DoesNotContain(player.Sent, m => m.Contains("Vault Key"));
        Assert.DoesNotContain(player.Sent, m => m.Contains("experience to enter"));
    }

    private sealed class RecordingMapScript : BaseMapScript
    {
        public int EnteredCalls;
        public string? Refusal;

        public override void OnPlayerEntered(Map map, Player player, GameWorld world) => this.EnteredCalls++;
        public override string? CanPlayerJoin(Map map, Player player, GameWorld world) => this.Refusal;
    }
}
