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

    [Fact]
    public void A_zone_without_a_buff_link_is_fine()
    {
        Assert.Null(SpellHandler.ZoneBuffProblem(Zone(), null));
    }

    [Fact]
    public void An_unknown_zone_buff_is_rejected()
    {
        Assert.NotNull(SpellHandler.ZoneBuffProblem(Zone(e => e.ZoneBuffEffectID = 99), null));
    }

    [Theory]
    [InlineData(SpellEffect.EffectTypes.Buff, true)]
    [InlineData(SpellEffect.EffectTypes.TickBuff, true)]
    [InlineData(SpellEffect.EffectTypes.Snare, true)]
    [InlineData(SpellEffect.EffectTypes.Stun, true)]
    [InlineData(SpellEffect.EffectTypes.Root, true)]
    [InlineData(SpellEffect.EffectTypes.Formula, false)]
    [InlineData(SpellEffect.EffectTypes.Tick, false)]
    [InlineData(SpellEffect.EffectTypes.GroundZone, false)]
    public void Only_buff_like_effects_can_be_zone_buffs(SpellEffect.EffectTypes type, bool allowed)
    {
        var buff = new SpellEffect { ID = 99, EffectType = type };

        var problem = SpellHandler.ZoneBuffProblem(Zone(e => e.ZoneBuffEffectID = 99), buff);

        Assert.Equal(allowed, problem is null);
    }
}
