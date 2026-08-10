using Goose.Tests.Collections;
using Goose.Tests.Fixtures;

namespace Goose.Tests;

[Collection(GameWorldSettingsCollection.Name)]
public class DimensionSpellScriptTests
{
    private const int Offset = 100000;

    private static GlobalScriptFixture Run(Action<GlobalScriptFixture> arrange)
    {
        var fixture = new GlobalScriptFixture();
        arrange(fixture);
        fixture.CompileShipped().Object.OnLoaded(fixture.World);
        return fixture;
    }

    [Fact]
    public void Clones_each_effect_once_per_dimension_with_the_name_prefix()
    {
        using var fixture = Run(f => { f.AddBaseMap(1, "Town"); f.AddBaseSpellEffect(42, "Firestorm"); });

        Assert.Equal("Supreme Firestorm", fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3).Name);
        Assert.Equal("Godly Firestorm", fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 6).Name);
        Assert.Equal("Firestorm", fixture.World.SpellHandler.GetSpellEffect(42).Name);   // base untouched
        Assert.Null(fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 7));
    }

    [Fact]
    public void Scales_duration_taunt_and_target_size()
    {
        using var fixture = Run(f => { f.AddBaseMap(1, "Town"); f.AddBaseSpellEffect(42, "Firestorm", e =>
        {
            e.Duration = 60000;
            e.TauntAggro = 500;
            e.TargetSize = 2;
            e.TargetType = SpellEffect.TargetTypes.Area;
        }); });

        var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

        Assert.Equal((long)(60000 * Math.Pow(1.15, 3)), dim3.Duration);                    // SpellHandler.java:295
        Assert.Equal((long)(500 * Math.Pow(3, 3) + 100000 * Math.Pow(20, 3)), dim3.TauntAggro);  // :298
        Assert.Equal(2 + 3, dim3.TargetSize);                                              // :302
    }

    /// <summary>AttributeSet.java:347. Unlike abyss we clone the set first, so MoveSpeed and
    /// SP survive rather than being silently zeroed - recorded as a deliberate deviation.</summary>
    [Fact]
    public void Scales_buff_stats_and_preserves_unscaled_fields()
    {
        using var fixture = Run(f => { f.AddBaseMap(1, "Town"); f.AddBaseSpellEffect(42, "Bless", e =>
        {
            e.Stats.HP = 100;  e.Stats.MP = 50;
            e.Stats.HPStaticRegen = 3;
            e.Stats.AC = 10;   e.Stats.Strength = 4;
            e.Stats.SpellDamage = 0.2m;
            e.Stats.MoveSpeed = 5;  e.Stats.SP = 7;
        }); });

        var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

        Assert.Equal(100 * 4 * 4, dim3.Stats.HP);                 // x (dim+1)^2
        Assert.Equal(50 * 4 * 4, dim3.Stats.MP);
        Assert.Equal((int)(3 * Math.Pow(4, 3)), dim3.Stats.HPStaticRegen);
        Assert.Equal((int)(10 * 2.5m), dim3.Stats.AC);            // x (1 + 0.5*dim)
        Assert.Equal(4 * 3, dim3.Stats.Strength);                 // x dim
        Assert.Equal(0.2m * 2.5m, dim3.Stats.SpellDamage);

        // Not in abyss's scaled list, and not zeroed here.
        Assert.Equal(5, dim3.Stats.MoveSpeed);
        Assert.Equal(7, dim3.Stats.SP);

        Assert.Equal(100, fixture.World.SpellHandler.GetSpellEffect(42).Stats.HP);   // base untouched
    }

    /// <summary>SpellHandler.java:290-294. Dimension buffs only land on level-50 targets,
    /// which is every dimension mob; damage effects stay castable on anything.</summary>
    [Theory]
    [InlineData((int)SpellEffect.EffectTypes.Buff, 50)]
    [InlineData((int)SpellEffect.EffectTypes.Permanent, 50)]
    [InlineData((int)SpellEffect.EffectTypes.Formula, 1)]
    public void Sets_minimum_level_effected_by_effect_type(int effectType, int expected)
    {
        using var fixture = Run(f => { f.AddBaseMap(1, "Town"); f.AddBaseSpellEffect(42, "Thing", e =>
        {
            e.EffectType = (SpellEffect.EffectTypes)effectType;
            e.MinimumLevelEffected = 20;
        }); });

        Assert.Equal(expected, fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3).MinimumLevelEffected);
    }

    /// <summary>SpellHandler.java:310-328. Small shapes grow into bigger ones, and the
    /// LineFront branch depends on the BASE size, not the scaled one. All four branches are
    /// covered: two that become Area, and both LineFront outcomes either side of size 1.</summary>
    [Theory]
    [InlineData((int)SpellEffect.TargetTypes.Cross,     2, (int)SpellEffect.TargetTypes.Area,          3)]
    [InlineData((int)SpellEffect.TargetTypes.Plus,      2, (int)SpellEffect.TargetTypes.Area,          3)]
    [InlineData((int)SpellEffect.TargetTypes.LineFront, 1, (int)SpellEffect.TargetTypes.Plus,          3)]
    [InlineData((int)SpellEffect.TargetTypes.LineFront, 2, (int)SpellEffect.TargetTypes.TriangleFront, 3)]
    [InlineData((int)SpellEffect.TargetTypes.LineFront, 3, (int)SpellEffect.TargetTypes.TriangleFront, 4)]
    public void Morphs_small_target_shapes_into_bigger_ones(
        int baseType, int baseSize, int expectedType, int expectedSize)
    {
        using var fixture = Run(f => { f.AddBaseMap(1, "Town"); f.AddBaseSpellEffect(42, "Nova", e =>
        {
            e.TargetType = (SpellEffect.TargetTypes)baseType;
            e.TargetSize = baseSize;
        }); });

        var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

        Assert.Equal((SpellEffect.TargetTypes)expectedType, dim3.TargetType);
        Assert.Equal(expectedSize, dim3.TargetSize);

        // The base effect keeps its own shape - the morph is on the clone only.
        Assert.Equal((SpellEffect.TargetTypes)baseType, fixture.World.SpellHandler.GetSpellEffect(42).TargetType);
        Assert.Equal(baseSize, fixture.World.SpellHandler.GetSpellEffect(42).TargetSize);
    }

    // ---- The preflight ----------------------------------------------------------------

    /// <summary>Base ids must be below the offset, because everything downstream defines
    /// "base" that way - RewireSpellEffects filters on ID &lt; Offset (Task 5). A base id above
    /// the offset would be cloned here and then silently skipped there.</summary>
    [Fact]
    public void A_base_effect_id_at_or_above_the_offset_is_rejected_before_anything_is_generated()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town");
        fixture.AddBaseSpellEffect(42, "Firestorm");
        fixture.AddBaseSpellEffect(Offset + 5, "Misconfigured");

        var script = fixture.CompileShipped();
        var error = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));

        Assert.Contains((Offset + 5).ToString(), error.Message);

        // Nothing was mutated: the preflight runs before the first AddSpellEffect, so the
        // handler is not left half-populated for whoever has to diagnose this.
        Assert.Null(fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3));
    }

    /// <summary>Spells get the same treatment as effects, and the spell check must also run
    /// before the effect cloning starts - not just before CloneSpells - or a bad spell id is
    /// only discovered once several thousand effects have already been registered.</summary>
    [Fact]
    public void A_base_spell_id_at_or_above_the_offset_is_rejected_before_anything_is_generated()
    {
        using var fixture = new GlobalScriptFixture();
        fixture.AddBaseMap(1, "Town");
        fixture.AddBaseSpellEffect(42, "Firestorm");
        fixture.AddBaseSpell(Offset + 91, "Misconfigured", 42);

        var script = fixture.CompileShipped();
        var error = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));

        Assert.Contains((Offset + 91).ToString(), error.Message);
        Assert.Null(fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3));   // no effects either
    }
}
