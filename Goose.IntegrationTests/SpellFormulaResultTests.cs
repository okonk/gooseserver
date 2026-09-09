using Goose.Testing;

namespace Goose.IntegrationTests;

public class SpellFormulaResultTests
{
    [Fact]
    public void CalculateFormulaResults_applies_spell_and_global_scaling_for_npc_target()
    {
        using var fixture = new TestWorldFixture(settings => settings.DamageModifier = 2m);
        var map = fixture.AddBaseMap(1, "Test");
        var caster = fixture.CommandPlayerOn(map, 5, 5);
        caster.MaxStats.SpellDamage = 0.5m;
        caster.MaxStats.SpellCrit = 0m;
        var target = CreateNpc(map);
        var effect = new SpellEffect { SpellDamageEffects = true };

        var result = effect.CalculateFormulaResults("-10", "10", caster, target, fixture.World);

        Assert.Equal(-30, result.HP);
        Assert.Equal(15, result.MP);
    }

    [Fact]
    public void CalculateFormulaResults_bypasses_spell_scaling_for_player_damage()
    {
        using var fixture = new TestWorldFixture(settings => settings.DamageModifier = 2m);
        var map = fixture.AddBaseMap(1, "Test");
        var caster = fixture.CommandPlayerOn(map, 5, 5);
        caster.MaxStats.SpellDamage = 0.5m;
        caster.MaxStats.SpellCrit = 0m;
        var target = fixture.CommandPlayerOn(map, 5, 4, "Target");
        var effect = new SpellEffect { SpellDamageEffects = true };

        var result = effect.CalculateFormulaResults("-10", "0", caster, target, fixture.World);

        Assert.Equal(-20, result.HP);
        Assert.Equal(0, result.MP);
    }

    [Fact]
    public void CastFormulaSpell_applies_calculated_hp_and_mp_results()
    {
        using var fixture = new TestWorldFixture(settings => settings.DamageModifier = 2m);
        var map = fixture.AddBaseMap(1, "Test");
        var caster = CreateNpc(map);
        var target = fixture.CommandPlayerOn(map, 5, 4, "Target");
        target.MaxStats.HP = 200;
        target.MaxStats.MP = 100;
        target.CurrentHP = 100;
        target.CurrentMP = 10;
        var effect = new SpellEffect
        {
            Effected = SpellEffect.SpellEffected.Player,
            MaximumLevelEffected = 99,
            HPFormula = "-10",
            MPFormula = "5"
        };

        var result = effect.CastFormulaSpell(caster, target, fixture.World);

        Assert.True(result);
        Assert.Equal(80, target.CurrentHP);
        Assert.Equal(15, target.CurrentMP);
    }

    private static NPC CreateNpc(Map map)
    {
        return new NPC
        {
            Map = map,
            MapID = map.ID,
            MapX = 5,
            MapY = 5,
            Name = "NPC",
            BaseStats = new AttributeSet(),
            MaxStats = new AttributeSet { HP = 100, MP = 100 },
            CanBeKilled = true
        };
    }
}
