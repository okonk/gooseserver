using Goose.IntegrationTests.Fixtures;

namespace Goose.IntegrationTests;

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
        using var fixture = Run(f => { f.AddBaseMap(1, "Town", width: 100, height: 100); f.AddBaseSpellEffect(42, "Firestorm"); });

        Assert.Equal("Supreme Firestorm", fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3).Name);
        Assert.Equal("Godly Firestorm", fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 6).Name);
        Assert.Equal("Firestorm", fixture.World.SpellHandler.GetSpellEffect(42).Name);   // base untouched
        Assert.Null(fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 7));
    }

    [Fact]
    public void Scales_duration_taunt_and_target_size()
    {
        using var fixture = Run(f => { f.AddBaseMap(1, "Town", width: 100, height: 100); f.AddBaseSpellEffect(42, "Firestorm", e =>
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
        using var fixture = Run(f => { f.AddBaseMap(1, "Town", width: 100, height: 100); f.AddBaseSpellEffect(42, "Bless", e =>
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
        using var fixture = Run(f => { f.AddBaseMap(1, "Town", width: 100, height: 100); f.AddBaseSpellEffect(42, "Thing", e =>
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
        using var fixture = Run(f => { f.AddBaseMap(1, "Town", width: 100, height: 100); f.AddBaseSpellEffect(42, "Nova", e =>
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
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
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
        fixture.AddBaseMap(1, "Town", width: 100, height: 100);
        fixture.AddBaseSpellEffect(42, "Firestorm");
        fixture.AddBaseSpell(Offset + 91, "Misconfigured", 42);

        var script = fixture.CompileShipped();
        var error = Assert.Throws<Exception>(() => script.Object.OnLoaded(fixture.World));

        Assert.Contains((Offset + 91).ToString(), error.Message);
        Assert.Null(fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3));   // no effects either
    }

    // ---- The rewire pass ---------------------------------------------------------------

    [Fact]
    public void Cross_references_point_at_the_same_dimensions_effects()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(9, "Retaliate");
            f.AddBaseSpellEffect(42, "Thorns", e =>
            {
                e.OnMeleeHitSpellID = 9;
                e.OnMeleeHitSpell = f.World.SpellHandler.GetSpellEffect(9);
            });
        });

        var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

        Assert.Equal(9 + Offset * 3, dim3.OnMeleeHitSpellID);
        Assert.Same(fixture.World.SpellHandler.GetSpellEffect(9 + Offset * 3), dim3.OnMeleeHitSpell);

        // The base effect keeps its own reference.
        Assert.Same(fixture.World.SpellHandler.GetSpellEffect(9),
                    fixture.World.SpellHandler.GetSpellEffect(42).OnMeleeHitSpell);
    }

    [Fact]
    public void A_cross_reference_with_no_clone_is_dropped_rather_than_left_at_dimension_zero()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Thorns", e =>
            {
                // An id that resolved at load time but is not in the handler now.
                e.OnMeleeHitSpellID = 999;
                e.OnMeleeHitSpell = new SpellEffect { ID = 999 };
            });
        });

        var dim3 = fixture.World.SpellHandler.GetSpellEffect(42 + Offset * 3);

        Assert.Null(dim3.OnMeleeHitSpell);
        Assert.Equal(0, dim3.OnMeleeHitSpellID);
    }

    /// <summary>The ladder: a dimension-3 buff replaces every copy at dimension 3 or below.</summary>
    [Fact]
    public void A_buff_stacks_over_its_own_lower_dimension_copies()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Bless", e => e.EffectType = SpellEffect.EffectTypes.Buff);
        });

        var handler = fixture.World.SpellHandler;
        var dim3 = handler.GetSpellEffect(42 + Offset * 3);

        Assert.Contains(handler.GetSpellEffect(42), dim3.BuffStacksOver);
        Assert.Contains(handler.GetSpellEffect(42 + Offset), dim3.BuffStacksOver);
        Assert.Contains(handler.GetSpellEffect(42 + Offset * 2), dim3.BuffStacksOver);
        Assert.DoesNotContain(handler.GetSpellEffect(42 + Offset * 4), dim3.BuffStacksOver);
    }

    /// <summary>And is refused outright by any higher-dimension copy already applied.</summary>
    [Fact]
    public void A_buff_does_not_stack_over_its_own_higher_dimension_copies()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Bless", e => e.EffectType = SpellEffect.EffectTypes.Buff);
        });

        var handler = fixture.World.SpellHandler;

        var dim3 = handler.GetSpellEffect(42 + Offset * 3);
        Assert.Contains(handler.GetSpellEffect(42 + Offset * 4), dim3.BuffDoesntStackOver);
        Assert.Contains(handler.GetSpellEffect(42 + Offset * 6), dim3.BuffDoesntStackOver);
        Assert.DoesNotContain(handler.GetSpellEffect(42 + Offset * 2), dim3.BuffDoesntStackOver);

        // The dimension-0 effect gets the same treatment, or the base spell would overwrite
        // its own upgrades.
        var basic = handler.GetSpellEffect(42);
        Assert.Contains(handler.GetSpellEffect(42 + Offset), basic.BuffDoesntStackOver);
    }

    /// <summary>The ladder extends to every entry in the base list, not just the effect itself.
    /// Without this a dim-3 Bless meeting a dim-0 Minor Bless matches neither list and applies
    /// as a second buff, stacking both stat blocks.</summary>
    [Fact]
    public void The_ladder_extends_to_entries_from_the_base_stacking_list()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var minor = f.AddBaseSpellEffect(41, "Minor Bless",
                e => e.EffectType = SpellEffect.EffectTypes.Buff);
            f.AddBaseSpellEffect(42, "Bless", e =>
            {
                e.EffectType = SpellEffect.EffectTypes.Buff;
                e.BuffStacksOver.Add(minor);
            });
        });

        var handler = fixture.World.SpellHandler;
        var dim3 = handler.GetSpellEffect(42 + Offset * 3);

        Assert.Contains(handler.GetSpellEffect(41), dim3.BuffStacksOver);              // dim 0 Minor Bless
        Assert.Contains(handler.GetSpellEffect(41 + Offset * 3), dim3.BuffStacksOver); // dim 3 Minor Bless
        Assert.DoesNotContain(handler.GetSpellEffect(41 + Offset * 5), dim3.BuffStacksOver);
    }

    /// <summary>The mirror of the test above, and the case a ladder built only from higher copies
    /// of the effect ITSELF gets wrong. Dimension 5 Minor Bless is above dimension 3, so it is not
    /// in dim-3 Bless's StacksOver; unless it is in DoesntStackOver it is in neither list, and
    /// AddBuff adds a second buff applying both stat blocks at once.</summary>
    [Fact]
    public void A_buff_refuses_to_stack_over_a_higher_dimension_copy_of_a_related_effect()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var minor = f.AddBaseSpellEffect(41, "Minor Bless",
                e => e.EffectType = SpellEffect.EffectTypes.Buff);
            f.AddBaseSpellEffect(42, "Bless", e =>
            {
                e.EffectType = SpellEffect.EffectTypes.Buff;
                e.BuffStacksOver.Add(minor);
            });
        });

        var handler = fixture.World.SpellHandler;
        var dim3 = handler.GetSpellEffect(42 + Offset * 3);

        Assert.Contains(handler.GetSpellEffect(41 + Offset * 5), dim3.BuffDoesntStackOver);
        Assert.Contains(handler.GetSpellEffect(41 + Offset * 4), dim3.BuffDoesntStackOver);
        Assert.Contains(handler.GetSpellEffect(41 + Offset * 6), dim3.BuffDoesntStackOver);

        // Every dimension copy of Minor Bless is in exactly one list - that is the invariant.
        for (int k = 0; k <= 6; k++)
        {
            var minorK = handler.GetSpellEffect(41 + Offset * k);
            Assert.True(dim3.BuffStacksOver.Contains(minorK) ^ dim3.BuffDoesntStackOver.Contains(minorK),
                        $"Minor Bless at dimension {k} is in neither list, or in both.");
        }
    }

    /// <summary>The lists are only worth anything if Player.AddBuff reads them the way the ladder
    /// assumes. This drives the real method: BuffDoesntStackOver is checked first and refuses the
    /// incoming buff outright (Player.cs:2074), which is what makes the ladder's ties resolve
    /// toward the higher dimension.</summary>
    [Fact]
    public void Player_AddBuff_refuses_a_lower_dimension_buff_over_a_higher_related_one()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            var minor = f.AddBaseSpellEffect(41, "Minor Bless",
                e => e.EffectType = SpellEffect.EffectTypes.Buff);
            f.AddBaseSpellEffect(42, "Bless", e =>
            {
                e.EffectType = SpellEffect.EffectTypes.Buff;
                e.BuffStacksOver.Add(minor);
            });
        });

        var handler = fixture.World.SpellHandler;
        var map = fixture.AddBaseMap(9, "Arena", width: 100, height: 100);
        var player = fixture.PlayerOn(map, x: 50, y: 50);
        player.State = Player.States.Ready;   // below Ready, AddBuff skips the stacking checks

        var applied = new Buff { SpellEffect = handler.GetSpellEffect(41 + Offset * 5), Target = player };
        player.Buffs.Add(applied);            // added directly: the refusal path is what is under test

        player.AddBuff(new Buff { SpellEffect = handler.GetSpellEffect(42 + Offset * 3), Target = player },
                       fixture.World, refreshbar: false, updateCharacter: false);

        Assert.Single(player.Buffs);
        Assert.Same(handler.GetSpellEffect(41 + Offset * 5), player.Buffs[0].SpellEffect);
    }

    // ---- The spell clone pass -----------------------------------------------------------

    [Fact]
    public void Clones_each_spell_once_per_dimension_pointing_at_the_same_dimensions_effect()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Firestorm");
            f.AddBaseSpell(91, "Firestorm", 42, s => s.Description = "Burns");
        });

        var handler = fixture.World.SpellHandler;
        var dim3 = handler.GetSpell(91 + Offset * 3);

        Assert.NotNull(dim3);
        Assert.Equal("Supreme Firestorm", dim3.Name);
        Assert.Equal("Abyss (3) Burns", dim3.Description);
        Assert.Equal(42 + Offset * 3, dim3.SpellEffectID);
        Assert.Same(handler.GetSpellEffect(42 + Offset * 3), dim3.SpellEffect);
    }

    [Fact]
    public void Scales_aether_and_static_costs()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Firestorm");
            f.AddBaseSpell(91, "Firestorm", 42, s =>
            {
                s.Aether = 10000; s.HPStaticCost = 50; s.MPStaticCost = 100;
                s.SPStaticCost = 7; s.MPPercentCost = 0.25m;
            });
        });

        var dim3 = fixture.World.SpellHandler.GetSpell(91 + Offset * 3);

        Assert.Equal((long)(10000 * Math.Pow(0.9, 3)), dim3.Aether);          // SpellHandler.java:279
        Assert.Equal((int)(50 * Math.Pow(3, 3)), dim3.HPStaticCost);          // :280
        Assert.Equal((int)(100 * Math.Pow(3, 3)), dim3.MPStaticCost);         // :281
        Assert.Equal(7, dim3.SPStaticCost);                                   // abyss leaves SP alone
        Assert.Equal(0.25m, dim3.MPPercentCost);                              // percent costs unscaled
    }

    [Fact]
    public void Leaves_the_base_spell_untouched()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Firestorm");
            f.AddBaseSpell(91, "Firestorm", 42, s => { s.Aether = 10000; s.Description = "Burns"; });
        });

        var basic = fixture.World.SpellHandler.GetSpell(91);

        Assert.Equal("Firestorm", basic.Name);
        Assert.Equal("Burns", basic.Description);
        Assert.Equal(10000, basic.Aether);
        Assert.Equal(42, basic.SpellEffectID);
    }

    /// <summary>The single-target extra 1.15 and the InvariantCulture separator, which
    /// ParseFormula depends on (SpellEffect.cs:1311).</summary>
    [Fact]
    public void Wraps_damage_formulas_with_the_dimension_multiplier()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Bolt", e =>
            {
                e.TargetType = SpellEffect.TargetTypes.Target;
                e.HPFormula = "-5 * %clevel";
            });
            f.AddBaseSpellEffect(43, "Nova", e =>
            {
                e.TargetType = SpellEffect.TargetTypes.Area;
                e.HPFormula = "-5 * %clevel";
            });
        });

        var handler = fixture.World.SpellHandler;
        var single = (1.15 * Math.Pow(1.25, 2)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var area = Math.Pow(1.25, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal("(-5 * %clevel) * " + single, handler.GetSpellEffect(42 + Offset * 2).HPFormula);
        Assert.Equal("(-5 * %clevel) * " + area, handler.GetSpellEffect(43 + Offset * 2).HPFormula);
        Assert.Contains(".", handler.GetSpellEffect(42 + Offset * 2).HPFormula);
    }

    // ---- NPC buff replacement ------------------------------------------------------------

    /// <summary>NPC.AddBuff's replacement branch stores the new effect without swapping
    /// stats (NPC.cs:1483) - unlike Player.AddBuff, which removes the old and adds the new.
    /// The dimension ladder makes this path reachable: a dim-3 buff replaces a dim-0 copy
    /// on an NPC, and without the swap the old stats stay applied while expiry subtracts
    /// the new effect's never-added stats.</summary>
    [Fact]
    public void Npc_AddBuff_replacement_swaps_the_stats()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Bless", e =>
            {
                e.EffectType = SpellEffect.EffectTypes.Buff;
                e.Stats.HP = 100;
            });
        });

        var handler = fixture.World.SpellHandler;
        var town = fixture.World.MapHandler.GetMap(1);
        var npc = new NPC { Map = town, MapX = 10, MapY = 10 };
        npc.MaxStats = new AttributeSet();
        npc.Buffs = new List<Buff>();   // parameterless NPC ctor leaves both null

        var dim0 = handler.GetSpellEffect(42);
        var dim3 = handler.GetSpellEffect(42 + Offset * 3);

        // Seed the applied dim-0 buff the way AddBuff's new-buff path applies stats.
        npc.Buffs.Add(new Buff { SpellEffect = dim0, Target = npc });
        npc.MaxStats += dim0.Stats;
        Assert.Equal(100, npc.MaxStats.HP);

        npc.AddBuff(new Buff { SpellEffect = dim3, Target = npc }, fixture.World);

        Assert.Single(npc.Buffs);
        Assert.Same(dim3, npc.Buffs[0].SpellEffect);
        Assert.Equal(1600, npc.MaxStats.HP);   // 100 * (3+1)^2, not the old 100
    }

    /// <summary>And the swap must be balanced: expiry subtracts exactly what replacement
    /// added, so MaxStats returns to base rather than going negative.</summary>
    [Fact]
    public void Npc_AddBuff_replacement_is_balanced_on_removal()
    {
        using var fixture = Run(f =>
        {
            f.AddBaseMap(1, "Town", width: 100, height: 100);
            f.AddBaseSpellEffect(42, "Bless", e =>
            {
                e.EffectType = SpellEffect.EffectTypes.Buff;
                e.Stats.HP = 100;
            });
        });

        var handler = fixture.World.SpellHandler;
        var town = fixture.World.MapHandler.GetMap(1);
        var npc = new NPC { Map = town, MapX = 10, MapY = 10 };
        npc.MaxStats = new AttributeSet();
        npc.Buffs = new List<Buff>();   // parameterless NPC ctor leaves both null

        var dim0 = handler.GetSpellEffect(42);
        var dim3 = handler.GetSpellEffect(42 + Offset * 3);

        npc.Buffs.Add(new Buff { SpellEffect = dim0, Target = npc });
        npc.MaxStats += dim0.Stats;
        npc.AddBuff(new Buff { SpellEffect = dim3, Target = npc }, fixture.World);

        npc.RemoveBuff(npc.Buffs[0], fixture.World);

        Assert.Empty(npc.Buffs);
        Assert.Equal(0, npc.MaxStats.HP);   // back to base, not 100 - 1600 = -1500
    }
}
