using Goose.Scripting;
using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionTeleportScriptTests
{
    private const int Offset = 100000;

    /// <summary>A world with base maps 1 (town) and 2 (cave), a Gate effect teleporting to
    /// map 2, and the whole generation pass run over it. Every cast test starts here, so what
    /// it exercises is the shipped rewrite rather than a hand-built effect.</summary>
    private static GlobalScriptFixture RunWithGate(int teleportMapId = 2)
    {
        var fixture = new GlobalScriptFixture();

        var town = fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        town.CanCast = true;                       // Map.cs:53 defaults to false
        var cave = fixture.AddBaseMap(2, "Cave", width: 100, height: 100);
        cave.CanCast = true;

        fixture.AddBaseSpellEffect(42, "Gate", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Teleport;
            e.TeleportMapID = teleportMapId;
            e.TeleportMapX = 7;
            e.TeleportMapY = 8;
            e.Effected = SpellEffect.SpellEffected.Self;
            e.MaximumLevelEffected = 99;
        });

        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Every_teleport_effect_is_rewritten_to_a_script_effect()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);   // CreateUnlockChain spawns the warden on map 1
        fixture.AddBaseSpellEffect(42, "Gate", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Teleport;
            e.TeleportMapID = 1; e.TeleportMapX = 5; e.TeleportMapY = 6;
        });

        using (fixture)
        {
            fixture.CompileShipped().Object.OnLoaded(fixture.World);

            foreach (var id in new[] { 42, 42 + Offset, 42 + Offset * 6 })
            {
                var effect = fixture.World.SpellHandler.GetSpellEffect(id);
                Assert.Equal(SpellEffect.EffectTypes.Script, effect.EffectType);
                Assert.NotNull(effect.Script);
                Assert.Equal(Offset.ToString(), effect.ScriptParams);

                // The destination data survives - the script reads it.
                Assert.Equal(1, effect.TeleportMapID);
                Assert.Equal(5, effect.TeleportMapX);
            }
        }
    }

    /// <summary>Non-teleport effects must not be touched.</summary>
    [Fact]
    public void Other_effect_types_keep_their_type()
    {
        var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);   // CreateUnlockChain spawns the warden on map 1
        fixture.AddBaseSpellEffect(43, "Bless", e => e.EffectType = SpellEffect.EffectTypes.Buff);

        using (fixture)
        {
            fixture.CompileShipped().Object.OnLoaded(fixture.World);

            Assert.Equal(SpellEffect.EffectTypes.Buff,
                         fixture.World.SpellHandler.GetSpellEffect(43 + Offset * 3).EffectType);
            Assert.Null(fixture.World.SpellHandler.GetSpellEffect(43 + Offset * 3).Script);
        }
    }

    /// <summary>The behaviour change the part exists for, driven through CastSpell on the
    /// effect the generation pass produced.</summary>
    [Fact]
    public void Casting_from_a_dimension_map_lands_in_that_dimension()
    {
        using var fixture = RunWithGate();

        var effect = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);
        var dim3Town = fixture.World.MapHandler.GetMap(1 + Offset * 3);
        var player = fixture.PlayerOn(dim3Town, x: 50, y: 50);
        player.Properties["dimension.max"] = 3;      // DimensionMap.csx gates the destination

        Assert.True(effect.CastSpell(player, player, fixture.World));
        Assert.Equal(2 + Offset * 3, player.MapID);  // MapID, not Map.ID - see above
        Assert.Equal(7, player.MapX);
    }

    /// <summary>Dimension 0 is rewritten too, and must still behave exactly as
    /// CastTeleportSpell did.</summary>
    [Fact]
    public void Casting_from_dimension_zero_lands_in_dimension_zero()
    {
        using var fixture = RunWithGate();

        var effect = fixture.World.SpellHandler.GetSpellEffect(42);
        var player = fixture.PlayerOn(fixture.World.MapHandler.GetMap(1), x: 50, y: 50);

        Assert.True(effect.CastSpell(player, player, fixture.World));
        Assert.Equal(2, player.MapID);
    }

    /// <summary>A destination with no clone in the caster's dimension falls back to the base
    /// map - an exit from the dimension rather than a broken spell. Same rule RewireWarps
    /// applies to warp tiles, and a deliberate deviation from abyss, which would send the
    /// player to their bind instead. Recorded in the design doc's decisions table.</summary>
    [Fact]
    public void A_destination_with_no_clone_falls_back_to_the_base_map()
    {
        using var fixture = RunWithGate();

        // Delete the dimension-3 copy of the destination, leaving the base map only.
        fixture.World.MapHandler.Maps.Remove(2 + Offset * 3);

        var effect = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);
        var player = fixture.PlayerOn(fixture.World.MapHandler.GetMap(1 + Offset * 3), x: 50, y: 50);
        player.Properties["dimension.max"] = 3;

        Assert.True(effect.CastSpell(player, player, fixture.World));
        Assert.Equal(2, player.MapID);
    }

    /// <summary>TeleportMapID 0 means "send them to their bind" - how gate spells work
    /// (SpellEffect.cs:717). The rewrite reimplements that branch, so it needs its own test.</summary>
    [Fact]
    public void A_teleport_with_no_destination_map_warps_to_the_bound_location()
    {
        using var fixture = RunWithGate(teleportMapId: 0);

        var town = fixture.World.MapHandler.GetMap(1);
        var effect = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);
        var player = fixture.PlayerOn(fixture.World.MapHandler.GetMap(1 + Offset * 3), x: 50, y: 50);
        player.BoundMap = town;
        player.BoundID = town.ID;
        player.BoundX = 11;
        player.BoundY = 12;

        Assert.True(effect.CastSpell(player, player, fixture.World));
        Assert.Equal(1, player.MapID);
        Assert.Equal(11, player.MapX);
        Assert.Equal(12, player.MapY);
    }

    /// <summary>PlayerCanJoin is the other branch reimplemented from CastTeleportSpell
    /// (SpellEffect.cs:727), and it is load-bearing: without it a dimension-0 teleport whose
    /// destination resolves into a locked dimension would walk straight past the entry gate.
    /// The refusal here comes from the real DimensionMap.csx script on the cloned map.</summary>
    [Fact]
    public void A_destination_the_player_cannot_enter_refuses_the_cast()
    {
        using var fixture = RunWithGate();

        var effect = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);
        var dim3Town = fixture.World.MapHandler.GetMap(1 + Offset * 3);
        var player = fixture.PlayerOn(dim3Town, x: 50, y: 50);
        player.Properties["dimension.max"] = 0;      // no access to dimension 3

        Assert.False(effect.CastSpell(player, player, fixture.World));
        Assert.Equal(1 + Offset * 3, player.MapID);  // did not move
    }

    /// <summary>CastSpell gated EffectTypes.Teleport on "target is Player"
    /// (SpellEffect.cs:939); the EffectTypes.Script arm does not (:975). The rewrite therefore
    /// removes a server-side guard, and the script has to put it back. This test is the only
    /// thing holding that.</summary>
    [Fact]
    public void An_npc_target_is_refused()
    {
        using var fixture = RunWithGate();

        var effect = fixture.World.SpellHandler.GetSpellEffect(42);
        var town = fixture.World.MapHandler.GetMap(1);
        var player = fixture.PlayerOn(town, x: 50, y: 50);
        var npc = new NPC { Map = town, MapX = 51, MapY = 50, CanBeKilled = true };

        Assert.False(effect.CastSpell(player, npc, fixture.World));
    }

    /// <summary>The rewrite drops teleport effects out of the built-in switch's
    /// case EffectTypes.Teleport (SpellEffect.cs:446) into default:, which would silently lose
    /// the destination line from the spell info window. Task 2's hook puts it back - asserted
    /// here on the shipped effect, through SpellEffect.GetItemDescription, so it covers the
    /// hook, the script's override, and the rewrite together.</summary>
    [Fact]
    public void The_shipped_rewritten_effect_still_describes_its_destination()
    {
        using var fixture = RunWithGate();

        var lines = fixture.World.SpellHandler.GetSpellEffect(42)
                           .GetItemDescription(fixture.World).ToArray();

        Assert.Equal(new[] { "Teleport to Cave (7, 8) in your current dimension" }, lines);
    }

    /// <summary>With the feature off, the spell pass must generate nothing AND leave teleport
    /// effects alone - the rewrite is a mutation of shipped data, so "disabled" has to mean
    /// the base effect is untouched, not merely that no clones appeared. Compiled variant, the
    /// same technique as DimensionsScriptTests.Disabled_by_configuration_changes_nothing.</summary>
    [Fact]
    public void Disabled_by_configuration_generates_no_spells_and_leaves_teleports_alone()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseSpellEffect(42, "Gate", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Teleport;
            e.TeleportMapID = 2;
        });
        fixture.AddBaseSpell(91, "Gate", 42);

        var source = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "DimensionScripts", "Dimensions.csx"));
        var disabled = source.Replace("public const bool Enabled = true;",
                                      "public const bool Enabled = false;");
        Assert.NotEqual(source, disabled);   // the flag line moved - fix this test, not the script

        fixture.CompileSource(disabled, "DimensionsDisabled.csx").Object.OnLoaded(fixture.World);

        Assert.Equal(1, fixture.World.SpellHandler.EffectCount);
        Assert.Equal(1, fixture.World.SpellHandler.Count);
        Assert.Null(fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3));
        Assert.Null(fixture.World.SpellHandler.GetSpell(91 + Offset * 3));

        var untouched = fixture.World.SpellHandler.GetSpellEffect(42);
        Assert.Equal(SpellEffect.EffectTypes.Teleport, untouched.EffectType);
        Assert.Null(untouched.Script);
        Assert.Empty(untouched.BuffDoesntStackOver);   // no ladder on the base effect either
    }
}
