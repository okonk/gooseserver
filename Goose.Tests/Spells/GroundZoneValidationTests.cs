namespace Goose.Tests.Spells;

public class GroundZoneValidationTests
{
    private static SpellEffect Zone(Action<SpellEffect>? configure = null)
    {
        var effect = new SpellEffect
        {
            ID = 1, Name = "Zone",
            EffectType = SpellEffect.EffectTypes.GroundZone,
            TargetType = SpellEffect.TargetTypes.Area,
            TargetSize = 1,
            Duration = 8,
        };
        configure?.Invoke(effect);
        return effect;
    }

    [Fact]
    public void A_well_formed_zone_has_no_problem()
    {
        Assert.Null(SpellHandler.GroundZoneProblem(Zone()));
    }

    [Fact]
    public void Non_zone_effects_are_never_flagged()
    {
        Assert.Null(SpellHandler.GroundZoneProblem(new SpellEffect { TargetType = SpellEffect.TargetTypes.Random }));
    }

    [Fact]
    public void Random_shape_is_rejected()
    {
        Assert.NotNull(SpellHandler.GroundZoneProblem(Zone(e => e.TargetType = SpellEffect.TargetTypes.Random)));
    }

    [Fact]
    public void Zero_duration_is_rejected()
    {
        Assert.NotNull(SpellHandler.GroundZoneProblem(Zone(e => e.Duration = 0)));
    }

    [Fact]
    public void Group_target_zone_spell_is_rejected()
    {
        var spell = new Spell { ID = 1, Name = "Zone", Target = Spell.SpellTargets.Group, SpellEffect = Zone() };

        Assert.NotNull(SpellHandler.SpellProblem(spell));
    }

    [Theory]
    [InlineData(Spell.SpellTargets.Self)]
    [InlineData(Spell.SpellTargets.Target)]
    public void Self_and_target_zone_spells_are_fine(Spell.SpellTargets target)
    {
        var spell = new Spell { ID = 1, Name = "Zone", Target = target, SpellEffect = Zone() };

        Assert.Null(SpellHandler.SpellProblem(spell));
    }
}
