using Goose.Testing;

namespace Goose.Tests.Spells;

public class FormulaApplyTests
{
    private static (TestWorldFixture fixture, Map map, TestWorldFixture.CapturingPlayer caster, NPC npc, SpellEffect effect) Setup()
    {
        var fixture = new TestWorldFixture(s => s.DamageModifier = 1);
        var map = fixture.AddBaseMap(1, "Field", 40, 40);
        map.CanCast = true;
        var caster = fixture.CommandPlayerOn(map, 10, 12, "Caster");
        map.Players.Add(caster);
        map.SetCharacter(caster, 10, 12);
        var npc = fixture.SpawnNpc(map, 10, 10, fixture.AddNpcTemplate(500, "Dummy", t => t.AggroRange = 0));
        var effect = fixture.AddBaseSpellEffect(1, "Burn", e =>
        {
            e.EffectType = SpellEffect.EffectTypes.Formula;
            e.Effected = SpellEffect.SpellEffected.NPC;
            e.HPFormula = "-10";
            e.MPFormula = "0";
            e.Animation = 5;
            e.DoCastAnimation = true;
        });
        return (fixture, map, caster, npc, effect);
    }

    private static string Received(TestWorldFixture.CapturingPlayer p) => string.Concat(p.Sent);

    [Fact]
    public void CastFormulaSpell_still_sends_the_effect_and_cast_animations()
    {
        var (fixture, _, caster, npc, effect) = Setup();
        using var _f = fixture;

        Assert.True(effect.CastFormulaSpell(caster, npc, fixture.World));

        Assert.Contains("SPP" + npc.LoginID + ",5,", Received(caster));
        Assert.Contains("CST" + caster.LoginID, Received(caster));
    }

    [Fact]
    public void ApplyFormula_without_animation_damages_but_sends_no_animations()
    {
        var (fixture, _, caster, npc, effect) = Setup();
        using var _f = fixture;
        long before = npc.CurrentHP;

        effect.ApplyFormula(caster, npc, fixture.World, animate: false, out long hp, out _);

        Assert.Equal(-10, hp);
        Assert.Equal(before - 10, npc.CurrentHP);
        Assert.DoesNotContain("SPP", Received(caster));
        Assert.DoesNotContain("CST", Received(caster));
    }

    [Fact]
    public void CanCastSpell_checks_the_given_map_instead_of_the_casters()
    {
        var (fixture, map, caster, npc, effect) = Setup();
        using var _f = fixture;
        var town = fixture.AddBaseMap(2, "Town", 20, 20);
        town.CanCast = false;
        caster.Map = town;

        Assert.False(effect.CanCastSpell(caster, npc));
        Assert.True(effect.CanCastSpell(caster, npc, map));
    }
}
